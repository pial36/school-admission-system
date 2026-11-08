namespace SchoolAdmission.Api.Infrastructure;

/// <summary>
/// IDs are GUIDs. A malformed ID in the URL can never match a row, so we answer
/// with the same JSON 404 as "not found" instead of a bare framework 404.
/// </summary>
public static class Ids
{
    public static Guid ParseClassId(string value) =>
        Guid.TryParse(value, out var id)
            ? id
            : throw ApiException.NotFound(ErrorCodes.ClassNotFound, "This class does not exist.");

    public static Guid ParseHoldId(string value) =>
        Guid.TryParse(value, out var id)
            ? id
            : throw ApiException.NotFound(ErrorCodes.HoldNotFound, "This application does not exist.");
}
