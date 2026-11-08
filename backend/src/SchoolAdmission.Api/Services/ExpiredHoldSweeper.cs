using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Services;

/// <summary>
/// Housekeeping only. Every few seconds, rows that are still HELD but past their
/// expiry time are written as EXPIRED.
///
/// Correctness does NOT depend on this job: seat counts and API responses already
/// treat a timed-out HELD row as expired (see Hold.EffectiveStatus). The sweeper
/// just keeps the stored data tidy and matching what users see.
/// </summary>
public class ExpiredHoldSweeper : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;
    private readonly ILogger<ExpiredHoldSweeper> _logger;

    public ExpiredHoldSweeper(IServiceScopeFactory scopes, TimeProvider clock, ILogger<ExpiredHoldSweeper> logger)
    {
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = _clock.GetUtcNow();

                // Single UPDATE; safe to run from several backend processes at once.
                var count = await db.Holds
                    .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
                    .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), stoppingToken);

                if (count > 0)
                    _logger.LogInformation("Marked {Count} hold(s) as EXPIRED", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never crash the app because of housekeeping; try again next tick.
                _logger.LogWarning(ex, "Expired-hold sweep failed");
            }
        }
    }
}
