namespace SchoolAdmission.Api.Infrastructure;

/// <summary>Settings read from configuration / environment variables.</summary>
public class AdmissionOptions
{
    /// <summary>How long a seat stays HELD. Env var HOLD_DURATION_SECONDS, default 600.</summary>
    public int HoldDurationSeconds { get; set; } = 600;

    public TimeSpan HoldDuration => TimeSpan.FromSeconds(HoldDurationSeconds);
}
