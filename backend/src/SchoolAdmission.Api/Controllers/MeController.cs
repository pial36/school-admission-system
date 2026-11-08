using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Infrastructure;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    private readonly HoldService _holds;

    public MeController(HoldService holds) => _holds = holds;

    /// <summary>8. GET /me/holds — all applications of the student in X-Student-Id.</summary>
    [HttpGet("holds")]
    public Task<List<MyHoldResponse>> MyHolds(CancellationToken ct) =>
        _holds.ListMineAsync(StudentIdHeader.Require(Request), ct);
}
