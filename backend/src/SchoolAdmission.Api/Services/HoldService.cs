using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Services;

public class HoldService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly AdmissionOptions _options;

    public HoldService(AppDbContext db, TimeProvider clock, IOptions<AdmissionOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    // ------------------------------------------------------------------ apply
    /// <summary>POST /classes/{classId}/holds — hold one seat for this student.</summary>
    public async Task<HoldCreatedResponse> CreateAsync(Guid classId, string studentId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // TODO(step 4): lock the class row here so concurrent requests cannot oversell.
        var schoolClass = await _db.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct)
            ?? throw ApiException.NotFound(ErrorCodes.ClassNotFound, "This class does not exist.");

        // Turn this class's timed-out holds into EXPIRED rows first, so they stop
        // blocking the "one active application per student" unique index.
        await _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);

        var existing = await _db.Holds
            .Where(h => h.ClassId == classId && h.StudentId == studentId
                        && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed))
            .Select(h => new { h.Status })
            .FirstOrDefaultAsync(ct);
        if (existing != null)
        {
            throw existing.Status == HoldStatus.Confirmed
                ? ApiException.Conflict(ErrorCodes.AlreadyConfirmed,
                    "This student already has a confirmed admission in this class.")
                : ApiException.Conflict(ErrorCodes.AlreadyApplied,
                    "This student already has an active application for this class.");
        }

        var occupied = await _db.Holds.CountAsync(h => h.ClassId == classId
            && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        if (occupied >= schoolClass.TotalSeats)
            throw ApiException.Conflict(ErrorCodes.SeatsFull,
                "Sorry, all seats in this class are taken right now. Please check again later.");

        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            // Whole seconds, so the stored value equals what the API shows.
            ExpiresAt = TruncateToSeconds(now + _options.HoldDuration),
        };
        _db.Holds.Add(hold);

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two requests from the same student raced; the unique index stopped the second.
            throw ApiException.Conflict(ErrorCodes.AlreadyApplied,
                "This student already has an active application for this class.");
        }

        return new HoldCreatedResponse(hold.Id.ToString(), classId.ToString(), studentId,
            hold.Status.ToApiString(), hold.ExpiresAt);
    }

    // ------------------------------------------------------------------ read
    /// <summary>GET /holds/{holdId}</summary>
    public async Task<HoldResponse> GetAsync(Guid holdId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var hold = await _db.Holds.AsNoTracking().Include(h => h.Class)
            .FirstOrDefaultAsync(h => h.Id == holdId, ct)
            ?? throw HoldNotFound();

        return new HoldResponse(hold.Id.ToString(), hold.ClassId.ToString(), hold.Class!.Name,
            hold.EffectiveStatus(now).ToApiString(), hold.ExpiresAt);
    }

    /// <summary>GET /me/holds — every application of this student, newest first.</summary>
    public async Task<List<MyHoldResponse>> ListMineAsync(string studentId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var holds = await _db.Holds.AsNoTracking().Include(h => h.Class)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        return holds.Select(h => new MyHoldResponse(h.Id.ToString(), h.ClassId.ToString(), h.Class!.Name,
            h.EffectiveStatus(now).ToApiString(), h.ExpiresAt)).ToList();
    }

    // ------------------------------------------------------------------ confirm
    /// <summary>
    /// POST /holds/{holdId}/confirm — pay the fee. Idempotent: confirming an
    /// already-confirmed hold returns 200 again and changes nothing.
    /// </summary>
    public async Task<HoldConfirmedResponse> ConfirmAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();

        // One atomic UPDATE: succeeds only if the hold is still HELD, unexpired and
        // owned by this student. No read-then-write gap for a race to slip into.
        var updated = await _db.Holds
            .Where(h => h.Id == holdId && h.StudentId == studentId
                        && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Confirmed)
                .SetProperty(h => h.ConfirmedAt, now), ct);

        if (updated == 1)
            return new HoldConfirmedResponse(holdId.ToString(), HoldStatus.Confirmed.ToApiString());

        // Nothing changed — find out why and answer accordingly.
        var hold = await LoadOwnedAsync(holdId, studentId, ct);
        return hold.EffectiveStatus(now) switch
        {
            HoldStatus.Confirmed => new HoldConfirmedResponse(holdId.ToString(), HoldStatus.Confirmed.ToApiString()),
            HoldStatus.Expired => throw ApiException.Conflict(ErrorCodes.HoldExpired,
                "The time to pay the admission fee has run out. The seat has been released."),
            HoldStatus.Cancelled => throw ApiException.Conflict(ErrorCodes.HoldCancelled,
                "This application was cancelled and can no longer be confirmed."),
            _ => throw new InvalidOperationException("Hold is HELD but could not be confirmed."),
        };
    }

    // ------------------------------------------------------------------ cancel
    /// <summary>
    /// DELETE /holds/{holdId} — cancel an application and free the seat.
    /// Cancelling something that is already cancelled or expired is a no-op (204).
    /// A confirmed admission (fee paid) cannot be cancelled here.
    /// </summary>
    public async Task CancelAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();

        var updated = await _db.Holds
            .Where(h => h.Id == holdId && h.StudentId == studentId
                        && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Cancelled)
                .SetProperty(h => h.CancelledAt, now), ct);

        if (updated == 1) return;

        var hold = await LoadOwnedAsync(holdId, studentId, ct);
        if (hold.EffectiveStatus(now) == HoldStatus.Confirmed)
            throw ApiException.Conflict(ErrorCodes.AlreadyConfirmed,
                "This admission is already confirmed (fee paid) and cannot be cancelled online.");
        // Already CANCELLED or EXPIRED: the seat is free anyway — nothing to do.
    }

    // ------------------------------------------------------------------ helpers
    private async Task<Hold> LoadOwnedAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        var hold = await _db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId, ct)
            ?? throw HoldNotFound();
        if (hold.StudentId != studentId)
            throw ApiException.Forbidden(ErrorCodes.NotYourHold,
                "This application belongs to a different student.");
        return hold;
    }

    private static ApiException HoldNotFound() =>
        ApiException.NotFound(ErrorCodes.HoldNotFound, "This application does not exist.");

    private static DateTimeOffset TruncateToSeconds(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
}
