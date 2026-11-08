namespace SchoolAdmission.Api.Contracts;

/// <summary>
/// Every error has the same shape so the UI can show <see cref="Message"/> directly
/// and branch on the machine-readable <see cref="Code"/> (e.g. SEATS_FULL).
/// </summary>
public record ErrorResponse(string Code, string Message);
