using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Infrastructure;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- configuration
// Connection string: override with env var ConnectionStrings__Default.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

// Hold duration: env var HOLD_DURATION_SECONDS (default 600).
builder.Services.Configure<AdmissionOptions>(o =>
{
    var seconds = builder.Configuration.GetValue<int?>("HOLD_DURATION_SECONDS") ?? 600;
    if (seconds <= 0)
        throw new InvalidOperationException("HOLD_DURATION_SECONDS must be a positive number.");
    o.HoldDurationSeconds = seconds;
});

// ---------------------------------------------------------------- services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System); // replaceable "clock" for tests
builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<HoldService>();
builder.Services.AddHostedService<ExpiredHoldSweeper>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter()));

// Malformed JSON / wrong types (e.g. "totalSeats": "abc") -> our standard 400 body.
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new ErrorResponse(
        ErrorCodes.InvalidInput,
        "The request is not valid. Please check the values and try again."));
});

// The React app runs on a different origin (port), so the browser needs CORS.
// No cookies/auth are used, so allowing any origin is safe here.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ---------------------------------------------------------------- pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Unknown URL -> JSON 404 instead of an empty response.
app.MapFallback(() => Results.NotFound(new ErrorResponse("NOT_FOUND", "This endpoint does not exist.")));

await MigrateDatabaseAsync(app);

app.Run();

// Creates/updates tables on startup. Retries because in docker compose the
// database container may still be starting when the backend boots.
static async Task MigrateDatabaseAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // If two backend processes start together, a PostgreSQL advisory lock
            // makes them migrate one after the other instead of colliding.
            await db.Database.OpenConnectionAsync();
            try
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(727001)");
                await db.Database.MigrateAsync();
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(727001)");
                await db.Database.CloseConnectionAsync();
            }
            logger.LogInformation("Database is up to date.");
            return;
        }
        catch (Exception ex) when (attempt < 30)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}/30): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

// Lets the test project start the app with WebApplicationFactory<Program>.
public partial class Program { }
