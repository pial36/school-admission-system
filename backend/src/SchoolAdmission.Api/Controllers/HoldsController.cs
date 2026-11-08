using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Infrastructure;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private readonly HoldService _holds;

    public HoldsController(HoldService holds) => _holds = holds;

    /// <summary>5. GET /holds/{holdId} — current state of one application.</summary>
    [HttpGet("{holdId}")]
    public Task<HoldResponse> Get(string holdId, CancellationToken ct) =>
        _holds.GetAsync(Ids.ParseHoldId(holdId), ct);

    /// <summary>6. POST /holds/{holdId}/confirm — pay fee, confirm admission.</summary>
    [HttpPost("{holdId}/confirm")]
    public Task<HoldConfirmedResponse> Confirm(string holdId, CancellationToken ct)
    {
        var studentId = StudentIdHeader.Require(Request);
        return _holds.ConfirmAsync(Ids.ParseHoldId(holdId), studentId, ct);
    }

    /// <summary>7. DELETE /holds/{holdId} — cancel application. 204 on success.</summary>
    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Cancel(string holdId, CancellationToken ct)
    {
        var studentId = StudentIdHeader.Require(Request);
        await _holds.CancelAsync(Ids.ParseHoldId(holdId), studentId, ct);
        return NoContent();
    }
}
