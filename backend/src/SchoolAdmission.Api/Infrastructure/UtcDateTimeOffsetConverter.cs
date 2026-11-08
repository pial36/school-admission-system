using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolAdmission.Api.Infrastructure;

/// <summary>
/// Writes dates exactly like the contract example: "2026-11-01T04:10:00Z"
/// (UTC, whole seconds, trailing Z) instead of .NET's default "+00:00" form.
/// </summary>
public class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
