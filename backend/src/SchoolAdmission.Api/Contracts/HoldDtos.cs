namespace SchoolAdmission.Api.Contracts;

// Field names follow the API contract exactly. Extra fields (e.g. className on
// GET /holds/{id}) are allowed by the brief and help the UI.

/// <summary>POST /classes/{classId}/holds 201 response.</summary>
public record HoldCreatedResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTimeOffset ExpiresAt);

/// <summary>GET /holds/{holdId} response.</summary>
public record HoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTimeOffset ExpiresAt);

/// <summary>POST /holds/{holdId}/confirm 200 response.</summary>
public record HoldConfirmedResponse(string HoldId, string Status);

/// <summary>GET /me/holds item.</summary>
public record MyHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTimeOffset ExpiresAt);
