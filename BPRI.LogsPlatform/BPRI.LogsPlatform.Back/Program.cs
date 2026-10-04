using BPRI.ExceptionHandling;
using BPRI.LogsPlatform.Back.Data;
using BPRI.LogsPlatform.Back.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("ExceptionLogs");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:ExceptionLogs non configuree.");
}

builder.Services.AddDbContext<LogsDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddBpriExceptionHandling(builder.Configuration);

string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.WithOrigins(origins).AllowAnyHeader().WithMethods("GET");
    });
});

var app = builder.Build();

app.UseBpriExceptionHandling();
app.UseCors("Web");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapLogsEndpoints();
app.MapRequestsEndpoints();

app.Run();
