namespace Catalog.Api.Endpoints;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Carter;
using Catalog.Api.Common;
using Catalog.Application.DTOs;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Authentication endpoints — issues JWT tokens for Angular clients.
/// </summary>
public class AuthEndpoints : ICarterModule
{
    // In a real project these would come from a database + hashed passwords.
    // Here we hard-code two demo users for simplicity.
    private static readonly Dictionary<string, (string PasswordHash, string Role)> Users = new()
    {
        ["admin"] = ("Admin@123", "Admin"),
        ["user"]  = ("User@123",  "User"),
    };

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", Login)
             .WithName("Login")
             .WithSummary("Authenticate and get a JWT token")
             .AllowAnonymous()
             .Produces<ApiResponse<TokenResponse>>(200)
             .Produces<ApiResponse<object>>(401);
    }

    private static IResult Login(LoginRequest request, IConfiguration config, HttpContext context)
    {
        var traceId = context.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            var err = new ApiResponse<object>(
                new ApiError
                {
                    Code = "INVALID_CREDENTIALS",
                    Title = "Identifiants manquants",
                    Description = "Le nom d'utilisateur et le mot de passe sont obligatoires."
                }, traceId: traceId);
            return Results.BadRequest(err);
        }

        if (!Users.TryGetValue(request.Username.ToLower(), out var user) ||
            user.PasswordHash != request.Password)
        {
            var err = new ApiResponse<object>(
                new ApiError
                {
                    Code = "INVALID_CREDENTIALS",
                    Title = "Identifiants incorrects",
                    Description = "Nom d'utilisateur ou mot de passe invalide."
                }, traceId: traceId);
            return Results.Json(err, statusCode: StatusCodes.Status401Unauthorized);
        }

        var jwtSecret = config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey non configurée.");
        var expiresInMinutes = config.GetValue<int>("Jwt:ExpiresInMinutes", 60);
        var issuer   = config["Jwt:Issuer"]   ?? "CatalogueApi";
        var audience = config["Jwt:Audience"] ?? "CatalogueApiUsers";

        var key     = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  request.Username),
            new Claim(JwtRegisteredClaimNames.Name, request.Username),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new ApiResponse<TokenResponse>(
            new TokenResponse
            {
                Token     = tokenString,
                TokenType = "Bearer",
                ExpiresAt = expires,
                Username  = request.Username,
                Role      = user.Role
            },
            traceId: traceId);

        return Results.Ok(response);
    }
}
