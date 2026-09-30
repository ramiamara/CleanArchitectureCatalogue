using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace BPRI.ExceptionHandling;

/// <summary>Ajoute à chaque log (exception ou ILogger) le contexte de l'application et de la requête en cours : cprj, utilisateur, claims JWT, TraceId…</summary>
internal sealed class RequestContextEnricher : ILogEventEnricher
{
    // Les JWT sont souvent remappés par ASP.NET : "sub" devient ClaimTypes.NameIdentifier, etc.
    private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "sub", ClaimTypes.NameIdentifier },
        { "email", ClaimTypes.Email },
        { "name", ClaimTypes.Name },
        { "role", ClaimTypes.Role },
        { "given_name", ClaimTypes.GivenName },
        { "family_name", ClaimTypes.Surname },
    };

    private readonly IHttpContextAccessor _accessor;
    private readonly ExceptionHandlingOptions _options;
    private readonly string _applicationName;
    private readonly string _environmentName;

    public RequestContextEnricher(
        IHttpContextAccessor accessor, ExceptionHandlingOptions options, string applicationName, string environmentName)
    {
        _accessor = accessor;
        _options = options;
        _applicationName = applicationName;
        _environmentName = environmentName;
    }

    public static string? TraceId(HttpContext? context)
    {
        if (Activity.Current != null)
        {
            return Activity.Current.TraceId.ToString();
        }
        if (context != null)
        {
            return context.TraceIdentifier;
        }
        return null;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        HttpContext? http = _accessor.HttpContext;

        AddProperty(logEvent, factory, "ApplicationName", _applicationName);
        AddProperty(logEvent, factory, "EnvironmentName", _environmentName);
        AddProperty(logEvent, factory, "MachineName", Environment.MachineName);
        AddProperty(logEvent, factory, "Cprj", ResolveCprj(http));
        AddProperty(logEvent, factory, "TraceId", TraceId(http));

        if (http == null)
        {
            return;
        }

        AddProperty(logEvent, factory, "HttpMethod", http.Request.Method);
        AddProperty(logEvent, factory, "Path", http.Request.Path.Value);
        AddProperty(logEvent, factory, "UserName", http.User.Identity?.Name);
        AddProperty(logEvent, factory, "UserId", Values(http.User, _options.Context.UserIdClaim).FirstOrDefault());
        AddProperty(logEvent, factory, "Claims", LoggedClaims(http.User));
    }

    private static void AddProperty(LogEvent logEvent, ILogEventPropertyFactory factory, string name, object? value)
    {
        if (value != null)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(name, value));
        }
    }

    /// <summary>Claim JWT, puis en-tête HTTP (si configuré), puis valeur de la configuration, puis nom de l'application.</summary>
    private string ResolveCprj(HttpContext? http)
    {
        if (http != null)
        {
            string? fromJwt = Values(http.User, _options.Context.CprjClaim).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fromJwt))
            {
                return fromJwt;
            }

            string? headerName = _options.Context.CprjHeader;
            if (!string.IsNullOrWhiteSpace(headerName))
            {
                string? fromHeader = http.Request.Headers[headerName].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fromHeader))
                {
                    return fromHeader;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.Cprj))
        {
            return _options.Cprj;
        }
        return _applicationName;
    }

    private string? LoggedClaims(ClaimsPrincipal user)
    {
        if (_options.Context.LoggedClaims.Count == 0 || user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var result = new Dictionary<string, string[]>();
        foreach (string type in _options.Context.LoggedClaims)
        {
            string[] values = Values(user, type).ToArray();
            if (values.Length > 0)
            {
                result[type] = values;
            }
        }

        if (result.Count == 0)
        {
            return null;
        }
        return JsonSerializer.Serialize(result);
    }

    private static IEnumerable<string> Values(ClaimsPrincipal user, string type)
    {
        IEnumerable<Claim> claims = user.FindAll(type);
        if (Aliases.TryGetValue(type, out string? alias))
        {
            claims = claims.Concat(user.FindAll(alias));
        }
        return claims.Select(c => c.Value).Distinct();
    }
}
