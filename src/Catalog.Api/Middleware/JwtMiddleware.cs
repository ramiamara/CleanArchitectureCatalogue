namespace Catalog.Api.Middleware;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Enriches HttpContext.Items with user claims extracted from a validated Bearer token.
/// Does NOT block requests — blocking is handled by [Authorize] / UseAuthorization().
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secretKey;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey non configuree.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey));
                var handler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ClockSkew                = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(token, validationParams, out _);
                var userName  = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value
                             ?? principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (userName is not null)
                    context.Items["UserName"] = userName;
            }
            catch
            {
                // Token is invalid but we let UseAuthorization() handle the 401.
                // No short-circuit here.
            }
        }

        await _next(context);
    }
}
