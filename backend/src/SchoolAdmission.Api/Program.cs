using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from configuration, so it can be overridden with the
// environment variable ConnectionStrings__Default (e.g. in docker-compose.yml).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
