using Carter;
using Catalog.Api.Middleware;
using Catalog.Application.Services;
using Catalog.Application.Validators;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Data;
using Catalog.Infrastructure.Repositories;
using BPRI.ExceptionHandling;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddBpriExceptionHandlingFile(builder.Environment);

// -- JWT
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey non configuree.");
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(key),
        ValidateIssuer           = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidateAudience         = true,
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// -- Rate Limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 200,
                Window               = TimeSpan.FromMinutes(1),
                QueueLimit           = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// -- EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<CatalogueContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// -- Cache
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// -- Application Services
builder.Services.AddScoped<ICatalogueService, CatalogueService>();
builder.Services.AddScoped<IProductService, ProductService>();

// -- Repositories (no more UnitOfWork — services inject IRepository<T> directly)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// -- AutoMapper
builder.Services.AddAutoMapper(typeof(Catalog.Application.Mappings.CatalogueProfile).Assembly);


// -- Validation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCatalogueValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();


// -- Exception handling (BPRI.ExceptionHandling : logs fichier / SQL Server / e-mail, voir appsettings)
builder.Services.AddBpriExceptionHandling(builder.Configuration, o =>
{
    o.MapException = exception =>
    {
        if (exception is not ValidationException validation)
        {
            return null;
        }

        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ErrorInfo(
            StatusCodes.Status400BadRequest,
            "Les donnees saisies sont invalides. Veuillez corriger les erreurs.",
            errors);
    };
});

// -- Carter
builder.Services.AddCarter();

// -- CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});


// -- Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalogue API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Token AD/JWT - format: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// -- Migration + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogueContext>();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(db);
}

// -- Pipeline
app.UseBpriExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalogue API v1"));
}

app.UseRateLimiter();
app.UseCors("AngularPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();

app.MapCarter();
app.Run();