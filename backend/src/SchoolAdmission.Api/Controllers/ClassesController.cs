using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Infrastructure;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ClassService _classes;
    private readonly HoldService _holds;

    public ClassesController(ClassService classes, HoldService holds)
    {
        _classes = classes;
        _holds = holds;
    }

    /// <summary>1. POST /classes — create a class (school admin).</summary>
    [HttpPost]
    public async Task<ActionResult<ClassCreatedResponse>> Create([FromBody] CreateClassRequest? request, CancellationToken ct)
    {
        var created = await _classes.CreateAsync(request, ct);
        return Created($"/classes/{created.Id}", created);
    }

    /// <summary>2. GET /classes — all classes with seat counts.</summary>
    [HttpGet]
    public Task<List<ClassSummaryResponse>> List(CancellationToken ct) => _classes.ListAsync(ct);

    /// <summary>3. GET /classes/{classId} — one class with seat counts.</summary>
    [HttpGet("{classId}")]
    public Task<ClassSummaryResponse> Get(string classId, CancellationToken ct) =>
        _classes.GetAsync(Ids.ParseClassId(classId), ct);

    /// <summary>4. POST /classes/{classId}/holds — apply (hold a seat). 409 if no seat.</summary>
    [HttpPost("{classId}/holds")]
    public async Task<ActionResult<HoldCreatedResponse>> CreateHold(string classId, CancellationToken ct)
    {
        var studentId = StudentIdHeader.Require(Request);
        var hold = await _holds.CreateAsync(Ids.ParseClassId(classId), studentId, ct);
        return Created($"/holds/{hold.HoldId}", hold);
    }
}
