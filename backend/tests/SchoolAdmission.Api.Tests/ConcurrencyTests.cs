using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

/// <summary>
/// The most important rule: held + confirmed must NEVER exceed totalSeats,
/// no matter how many requests arrive at the same moment.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests
{
    private readonly ApiFixture _fx;

    public ConcurrencyTests(ApiFixture fx) => _fx = fx;

    [Fact]
    public async Task Many_students_applying_at_once_never_get_more_holds_than_seats()
    {
        const int seats = 10;
        const int applicants = 50;
        var classId = await _fx.Client.CreateClassAsync(seats);

        // Fire all 50 requests at the same time (a different student each).
        var responses = await Task.WhenAll(Enumerable.Range(0, applicants)
            .Select(_ => _fx.Client.ApplyAsync(classId, ApiHelpers.NewStudentId())));

        Assert.Equal(seats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(applicants - seats, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var cls = await _fx.Client.GetClassAsync(classId);
        Assert.Equal(seats, cls.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, cls.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Same_student_applying_many_times_at_once_gets_only_one_hold()
    {
        var classId = await _fx.Client.CreateClassAsync(totalSeats: 10);
        var student = ApiHelpers.NewStudentId();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => _fx.Client.ApplyAsync(classId, student)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var cls = await _fx.Client.GetClassAsync(classId);
        Assert.Equal(1, cls.GetProperty("heldSeats").GetInt32());
    }

    [Fact]
    public async Task Two_backend_instances_on_one_database_still_never_oversell()
    {
        // Bonus requirement: two separate API instances share the same database.
        var second = _fx.CreateFactory().CreateClient();
        const int seats = 5;
        var classId = await _fx.Client.CreateClassAsync(seats);

        // 40 requests split evenly between the two instances, all at once.
        var responses = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            (i % 2 == 0 ? _fx.Client : second).ApplyAsync(classId, ApiHelpers.NewStudentId())));

        Assert.Equal(seats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));

        var cls = await second.GetClassAsync(classId);
        Assert.Equal(seats, cls.GetProperty("heldSeats").GetInt32());
    }
}
