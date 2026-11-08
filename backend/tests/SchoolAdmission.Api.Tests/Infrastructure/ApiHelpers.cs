using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Small helpers so tests read like the user story.</summary>
public static class ApiHelpers
{
    /// <summary>A random, valid 17-digit birth registration number.</summary>
    public static string NewStudentId() =>
        Random.Shared.NextInt64(10_000_000_000_000_000, 99_999_999_999_999_999).ToString();

    public static async Task<string> CreateClassAsync(this HttpClient client, int totalSeats, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/classes",
            new { name = name ?? $"Test class {Guid.NewGuid():N}", totalSeats });
        response.EnsureSuccessStatusCode();
        var json = await response.ReadJsonAsync();
        return json.GetProperty("id").GetString()!;
    }

    public static Task<HttpResponseMessage> ApplyAsync(this HttpClient client, string classId, string studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Post, $"/classes/{classId}/holds", studentId));

    public static Task<HttpResponseMessage> ConfirmAsync(this HttpClient client, string holdId, string studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId));

    public static Task<HttpResponseMessage> CancelAsync(this HttpClient client, string holdId, string studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Delete, $"/holds/{holdId}", studentId));

    public static async Task<JsonElement> GetClassAsync(this HttpClient client, string classId)
    {
        var response = await client.GetAsync($"/classes/{classId}");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync();
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public static HttpRequestMessage WithStudent(HttpMethod method, string url, string studentId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Student-Id", studentId);
        return request;
    }
}
