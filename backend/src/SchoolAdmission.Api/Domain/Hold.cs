namespace SchoolAdmission.Api.Domain;

/// <summary>
/// One application = one seat hold for one student in one class.
/// Lifecycle: HELD -> CONFIRMED (fee paid) | EXPIRED (time ran out) | CANCELLED (student cancelled).
/// </summary>
public class Hold
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    /// <summary>17-digit birth registration number from the X-Student-Id header.</summary>
    public string StudentId { get; set; } = string.Empty;

    public HoldStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>After this moment a HELD hold no longer occupies a seat.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// The status as of <paramref name="now"/>. A HELD row whose time has passed is
    /// reported as EXPIRED even before the background sweeper updates the row, so
    /// expiry never depends on anyone calling an API.
    /// </summary>
    public HoldStatus EffectiveStatus(DateTimeOffset now) =>
        Status == HoldStatus.Held && ExpiresAt <= now ? HoldStatus.Expired : Status;
}
