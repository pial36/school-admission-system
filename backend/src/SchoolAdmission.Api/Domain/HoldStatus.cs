namespace SchoolAdmission.Api.Domain;

/// <summary>Stored in the database as text: HELD, CONFIRMED, EXPIRED, CANCELLED.</summary>
public enum HoldStatus
{
    Held,
    Confirmed,
    Expired,
    Cancelled,
}

public static class HoldStatusExtensions
{
    public static string ToApiString(this HoldStatus status) => status switch
    {
        HoldStatus.Held => "HELD",
        HoldStatus.Confirmed => "CONFIRMED",
        HoldStatus.Expired => "EXPIRED",
        HoldStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static HoldStatus FromApiString(string value) => value switch
    {
        "HELD" => HoldStatus.Held,
        "CONFIRMED" => HoldStatus.Confirmed,
        "EXPIRED" => HoldStatus.Expired,
        "CANCELLED" => HoldStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
