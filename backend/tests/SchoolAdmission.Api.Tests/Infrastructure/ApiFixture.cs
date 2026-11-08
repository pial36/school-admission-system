using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>
/// Shared test setup: one real PostgreSQL database + one running API.
///
/// Database source:
///   - If env var TEST_DB_CONNECTION is set (e.g. when tests run inside docker compose),
///     a fresh database with a random name is created on that server.
///   - Otherwise a throw-away PostgreSQL container is started with Testcontainers
///     (needs Docker running on the machine).
///
/// We test against real PostgreSQL (not an in-memory fake) because the row lock
/// and partial unique index we rely on only exist in a real database.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private readonly List<ApiFactory> _factories = new();

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Controllable clock: tests call Clock.Advance(...) instead of sleeping.</summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public HttpClient Client { get; private set; } = null!;

    public const int HoldDurationSeconds = 600;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(external))
        {
            ConnectionString = new NpgsqlConnectionStringBuilder(external)
            {
                Database = $"sas_test_{Guid.NewGuid():N}",
            }.ConnectionString; // EF migrations create this database on first start
        }
        else
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        Client = CreateFactory().CreateClient();
    }

    /// <summary>
    /// Starts another, independent copy of the API on the same database.
    /// Used to simulate two backend processes (bonus requirement).
    /// </summary>
    public ApiFactory CreateFactory()
    {
        var factory = new ApiFactory(ConnectionString, Clock, HoldDurationSeconds);
        _factories.Add(factory);
        return factory;
    }

    public async Task DisposeAsync()
    {
        foreach (var f in _factories) await f.DisposeAsync();
        if (_container != null) await _container.DisposeAsync();
    }
}

/// <summary>All test classes share one fixture and run one after another.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
