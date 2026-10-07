using BPRI.LogsPlatform.Back.Data;
using BPRI.LogsPlatform.Back.Errors;
using BPRI.LogsPlatform.Back.Services;
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

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddControllers();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.WithOrigins(origins).AllowAnyHeader().WithMethods("GET");
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Web");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
