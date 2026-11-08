using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Boots the real API in memory, pointed at the test database and fake clock.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly TimeProvider _clock;
    private readonly int _holdSeconds;

    public ApiFactory(string connectionString, TimeProvider clock, int holdSeconds)
    {
        _connectionString = connectionString;
        _clock = clock;
        _holdSeconds = holdSeconds;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", _holdSeconds.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(_clock);
        });
    }
}
