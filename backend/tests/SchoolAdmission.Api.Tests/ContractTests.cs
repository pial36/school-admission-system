using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

/// <summary>
/// The brief says automated tests will hit the API directly, so every field name
/// in the contract must be present. These tests fail if a field is renamed or removed.
/// </summary>
[Collection(ApiCollection.Name)]
public class ContractTests
{
    private readonly ApiFixture _fx;

    public ContractTests(ApiFixture fx) => _fx = fx;

    private static void AssertHasFields(JsonElement json, params string[] fields)
    {
        foreach (var field in fields)
            Assert.True(json.TryGetProperty(field, out _), $"Missing field '{field}' in {json}");
    }

    [Fact]
    public async Task All_endpoints_return_the_contract_fields()
    {
        // 1. POST /classes
        var create = await _fx.Client.PostAsJsonAsync("/classes",
            new { name = "Class 6 - Morning Shift", totalSeats = 120 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.ReadJsonAsync();
        AssertHasFields(created, "id", "name", "totalSeats");
        Assert.Equal("Class 6 - Morning Shift", created.GetProperty("name").GetString());
        Assert.Equal(120, created.GetProperty("totalSeats").GetInt32());
        var classId = created.GetProperty("id").GetString()!;

        var classFields = new[] { "id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats" };

        // 2. GET /classes
        var list = await (await _fx.Client.GetAsync("/classes")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Array, list.ValueKind);
        AssertHasFields(list.EnumerateArray().First(c => c.GetProperty("id").GetString() == classId), classFields);

        // 3. GET /classes/{classId}
        AssertHasFields(await _fx.Client.GetClassAsync(classId), classFields);

        // 4. POST /classes/{classId}/holds
        var student = ApiHelpers.NewStudentId();
        var apply = await _fx.Client.ApplyAsync(classId, student);
        Assert.Equal(HttpStatusCode.Created, apply.StatusCode);
        var hold = await apply.ReadJsonAsync();
        AssertHasFields(hold, "holdId", "classId", "studentId", "status", "expiresAt");
        Assert.Equal("HELD", hold.GetProperty("status").GetString());
        Assert.Equal(student, hold.GetProperty("studentId").GetString());
        Assert.EndsWith("Z", hold.GetProperty("expiresAt").GetString());
        var holdId = hold.GetProperty("holdId").GetString()!;

        // 5. GET /holds/{holdId}
        AssertHasFields(await (await _fx.Client.GetAsync($"/holds/{holdId}")).ReadJsonAsync(),
            "holdId", "classId", "status", "expiresAt");

        // 8. GET /me/holds
        var mine = await (await _fx.Client.SendAsync(
            ApiHelpers.WithStudent(HttpMethod.Get, "/me/holds", student))).ReadJsonAsync();
        AssertHasFields(mine.EnumerateArray().Single(), "holdId", "classId", "className", "status", "expiresAt");

        // 6. POST /holds/{holdId}/confirm
        var confirm = await _fx.Client.ConfirmAsync(holdId, student);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        AssertHasFields(await confirm.ReadJsonAsync(), "holdId", "status");

        // 7. DELETE /holds/{holdId} -> 204 (on a fresh hold)
        var other = ApiHelpers.NewStudentId();
        var otherHold = (await (await _fx.Client.ApplyAsync(classId, other)).ReadJsonAsync())
            .GetProperty("holdId").GetString()!;
        Assert.Equal(HttpStatusCode.NoContent, (await _fx.Client.CancelAsync(otherHold, other)).StatusCode);
    }

    [Theory]
    [InlineData("")]                      // missing
    [InlineData("1234")]                  // too short
    [InlineData("123456789012345678")]    // 18 digits
    [InlineData("1234567890123456X")]     // not a number
    public async Task Invalid_student_id_is_rejected_with_400(string studentId)
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 5);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        if (studentId != "") request.Headers.Add("X-Student-Id", studentId);

        var response = await _fx.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("{\"name\":\"\",\"totalSeats\":10}")]
    [InlineData("{\"name\":\"Class 7\",\"totalSeats\":0}")]
    [InlineData("{\"name\":\"Class 7\",\"totalSeats\":\"many\"}")]
    [InlineData("not json")]
    public async Task Invalid_class_input_is_rejected_with_400(string body)
    {
        var response = await _fx.Client.PostAsync("/classes",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_INPUT", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unknown_ids_return_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _fx.Client.GetAsync($"/classes/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _fx.Client.GetAsync("/holds/not-a-real-id")).StatusCode);
    }
}
