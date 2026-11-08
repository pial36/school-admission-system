using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Services;

public class ClassService
{
    public const int MaxNameLength = 200;
    public const int MaxTotalSeats = 10_000;

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public ClassService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ClassCreatedResponse> CreateAsync(CreateClassRequest? request, CancellationToken ct)
    {
        var name = request?.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            throw ApiException.BadRequest(ErrorCodes.InvalidInput, "Class name is required.");
        if (name.Length > MaxNameLength)
            throw ApiException.BadRequest(ErrorCodes.InvalidInput,
                $"Class name must be at most {MaxNameLength} characters.");

        if (request!.TotalSeats is not { } seats)
            throw ApiException.BadRequest(ErrorCodes.InvalidInput, "Total seats is required.");
        if (seats < 1 || seats > MaxTotalSeats)
            throw ApiException.BadRequest(ErrorCodes.InvalidInput,
                $"Total seats must be a whole number between 1 and {MaxTotalSeats}.");

        var entity = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            TotalSeats = seats,
            CreatedAt = _clock.GetUtcNow(),
        };
        _db.Classes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ClassCreatedResponse(entity.Id.ToString(), entity.Name, entity.TotalSeats);
    }

    public async Task<List<ClassSummaryResponse>> ListAsync(CancellationToken ct)
    {
        var rows = await QuerySummaries().OrderBy(r => r.CreatedAt).ToListAsync(ct);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<ClassSummaryResponse> GetAsync(Guid classId, CancellationToken ct)
    {
        var row = await QuerySummaries().FirstOrDefaultAsync(r => r.Id == classId, ct)
            ?? throw ApiException.NotFound(ErrorCodes.ClassNotFound, "This class does not exist.");
        return ToResponse(row);
    }

    /// <summary>
    /// Seat counts are always calculated from the holds table, never stored as counters,
    /// so they can never drift. A HELD hold past its expiry time is NOT counted —
    /// that is what makes the seat "come back" automatically when time runs out.
    /// </summary>
    private IQueryable<SummaryRow> QuerySummaries()
    {
        var now = _clock.GetUtcNow();
        return _db.Classes.AsNoTracking().Select(c => new SummaryRow
        {
            Id = c.Id,
            Name = c.Name,
            TotalSeats = c.TotalSeats,
            CreatedAt = c.CreatedAt,
            HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
            ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.Confirmed),
        });
    }

    private static ClassSummaryResponse ToResponse(SummaryRow r) => new(
        r.Id.ToString(),
        r.Name,
        r.TotalSeats,
        r.HeldSeats,
        r.ConfirmedSeats,
        Math.Max(0, r.TotalSeats - r.HeldSeats - r.ConfirmedSeats));

    private class SummaryRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int TotalSeats { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public int HeldSeats { get; init; }
        public int ConfirmedSeats { get; init; }
    }
}
