using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

/// <summary>Business rules around confirming, expiring and cancelling applications.</summary>
[Collection(ApiCollection.Name)]
public class HoldLifecycleTests
{
    private readonly ApiFixture _fx;

    public HoldLifecycleTests(ApiFixture fx) => _fx = fx;

    private async Task<string> ApplyOkAsync(string classId, string student)
    {
        var response = await _fx.Client.ApplyAsync(classId, student);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("holdId").GetString()!;
    }

    [Fact]
    public async Task Confirming_the_same_hold_twice_counts_the_seat_only_once()
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 3);
        var student = ApiHelpers.NewStudentId();
        var holdId = await ApplyOkAsync(classId, student);

        // Same confirm sent 5 times in parallel, then once more afterwards.
        var parallel = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _fx.Client.ConfirmAsync(holdId, student)));
        var again = await _fx.Client.ConfirmAsync(holdId, student);

        Assert.All(parallel, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal("CONFIRMED", (await again.ReadJsonAsync()).GetProperty("status").GetString());

        var cls = await _fx.Client.GetClassAsync(classId);
        Assert.Equal(1, cls.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, cls.GetProperty("heldSeats").GetInt32());
        Assert.Equal(2, cls.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Expired_hold_frees_the_seat_by_itself_and_cannot_be_confirmed()
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 1);
        var student = ApiHelpers.NewStudentId();
        var holdId = await ApplyOkAsync(classId, student);

        // Class is full while the hold is active.
        var blocked = await _fx.Client.ApplyAsync(classId, ApiHelpers.NewStudentId());
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        // Time passes beyond the hold duration — no API call needed to release it.
        _fx.Clock.Advance(TimeSpan.FromSeconds(ApiFixture.HoldDurationSeconds + 1));

        var hold = await (await _fx.Client.GetAsync($"/holds/{holdId}")).ReadJsonAsync();
        Assert.Equal("EXPIRED", hold.GetProperty("status").GetString());

        var cls = await _fx.Client.GetClassAsync(classId);
        Assert.Equal(0, cls.GetProperty("heldSeats").GetInt32());
        Assert.Equal(1, cls.GetProperty("availableSeats").GetInt32());

        var confirm = await _fx.Client.ConfirmAsync(holdId, student);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_EXPIRED", (await confirm.ReadJsonAsync()).GetProperty("code").GetString());

        // The freed seat can now be taken by someone else.
        var next = await _fx.Client.ApplyAsync(classId, ApiHelpers.NewStudentId());
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_can_confirm_or_cancel_a_hold()
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 2);
        var owner = ApiHelpers.NewStudentId();
        var holdId = await ApplyOkAsync(classId, owner);
        var stranger = ApiHelpers.NewStudentId();

        Assert.Equal(HttpStatusCode.Forbidden, (await _fx.Client.ConfirmAsync(holdId, stranger)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _fx.Client.CancelAsync(holdId, stranger)).StatusCode);
    }

    [Fact]
    public async Task Cancelling_frees_the_seat_and_allows_applying_again()
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 1);
        var student = ApiHelpers.NewStudentId();
        var holdId = await ApplyOkAsync(classId, student);

        var cancel = await _fx.Client.CancelAsync(holdId, student);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var hold = await (await _fx.Client.GetAsync($"/holds/{holdId}")).ReadJsonAsync();
        Assert.Equal("CANCELLED", hold.GetProperty("status").GetString());
        Assert.Equal(1, (await _fx.Client.GetClassAsync(classId)).GetProperty("availableSeats").GetInt32());

        // Same student may apply again after cancelling.
        await ApplyOkAsync(classId, student);
    }
}
