namespace SchoolAdmission.Api.Domain;

/// <summary>
/// One admission "class". Every shift is its own class with its own seats,
/// e.g. "Class 6 - Morning Shift" and "Class 6 - Day Shift" are two rows.
/// (Named SchoolClass because "class" is a C# keyword.)
/// </summary>
public class SchoolClass
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Fixed seat capacity. held + confirmed must never exceed this.</summary>
    public int TotalSeats { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Hold> Holds { get; set; } = new();
}
