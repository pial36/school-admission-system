namespace SchoolAdmission.Api.Contracts;

// Field names follow the API contract exactly (camelCase in JSON).

/// <summary>POST /classes body. Nullable so we can return our own clear error messages.</summary>
public record CreateClassRequest(string? Name, int? TotalSeats);

/// <summary>POST /classes 201 response.</summary>
public record ClassCreatedResponse(string Id, string Name, int TotalSeats);

/// <summary>GET /classes item and GET /classes/{classId} response.</summary>
public record ClassSummaryResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
