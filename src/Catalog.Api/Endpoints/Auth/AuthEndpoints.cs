namespace Catalog.Api.Endpoints.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Carter;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Carter module for authentication.
/// Simple enough to stay in a single file — no domain logic involved.
/// </summary>
public class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Login)
           .AllowAnonymous()
           .WithName("Login")
           .WithTags("Auth")
           .Produces<ApiResponse<TokenResponse>>(200)
           .Produces(400)
           .Produces(401);
    }

    private static IResult Login(LoginRequest request, IConfiguration config, HttpContext ctx)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new ApiError { Code = "MISSING_CREDENTIALS", Description = "Identifiants manquants." });

        // Demo: in production, validate against DB with hashed passwords
        var isValid = request.Username == "admin" && request.Password == "admin";
        if (!isValid)
            return Results.Unauthorized();

        var role   = request.Username == "admin" ? "Admin" : "User";
        var traceId = ctx.TraceIdentifier;
        var token  = GenerateToken(request.Username, role, config);
        var response = new ApiResponse<TokenResponse>(token, traceId: traceId);
        return Results.Ok(response);
    }

    private static TokenResponse GenerateToken(string username, string role, IConfiguration config)
    {
        var jwtSecret = config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey non configuree.");
        var issuer    = config["Jwt:Issuer"]    ?? "CatalogueApi";
        var audience  = config["Jwt:Audience"]  ?? "CatalogueApiUsers";
        var expiresIn = config.GetValue<int>("Jwt:ExpiresInMinutes", 60);

        var key     = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresIn);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  username),
            new Claim(JwtRegisteredClaimNames.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expires,
            signingCredentials: creds);

        return new TokenResponse
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAt = expires,
            Username  = username,
            Role      = role
        };
    }
}