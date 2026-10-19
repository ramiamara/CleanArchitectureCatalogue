using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace BPRI.ExceptionHandling;

/// <summary>
/// Trace les entrées (query string, corps) et les réponses (statut, corps, durée) des requêtes.
/// Si RequestTracing:Enabled = true, les requêtes correspondant aux filtres (méthodes, chemins) sont toutes tracées.
/// Dans tous les cas, une requête qui se termine en erreur 500 est tracée (sans corps de réponse quand le traçage est désactivé).
/// </summary>
internal sealed class RequestTracingMiddleware
{
    private const long MaxRequestBodyBytes = 1_000_000;

    private static readonly string[] DefaultMethods = ["GET", "POST", "PUT", "DELETE"];
    private static readonly string[] AlwaysMasked =
        ["password", "motdepasse", "pwd", "secret", "token", "accessToken", "refreshToken", "authorization", "apiKey"];

    private readonly RequestDelegate _next;
    private readonly RequestTraceLogger _logger;
    private readonly RequestTracingOptions _options;
    private readonly string[] _methods;
    private readonly PathString[] _includedPaths;
    private readonly PathString[] _excludedPaths;
    private readonly Regex _jsonSecrets;
    private readonly Regex _formSecrets;

    public RequestTracingMiddleware(RequestDelegate next, RequestTraceLogger logger, RequestTracingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _methods = options.Methods.Count > 0 ? options.Methods.ToArray() : DefaultMethods;
        _includedPaths = ToPaths(options.IncludedPaths);
        _excludedPaths = ToPaths(options.ExcludedPaths);

        string names = string.Join("|", AlwaysMasked.Concat(options.MaskedFields).Select(Regex.Escape));
        _jsonSecrets = new Regex("(\"(?:" + names + ")\"\\s*:\\s*)(\"(?:[^\"\\\\]|\\\\.)*\"|[^,}\\]\\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _formSecrets = new Regex("\\b((?:" + names + ")=)[^&]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool traced = ShouldTrace(context);
        var stopwatch = Stopwatch.StartNew();

        // Le corps est lu avant le traitement pour pouvoir être enregistré si la requête finit en erreur 500.
        string? requestBody = await ReadRequestBodyAsync(context.Request);

        if (!traced)
        {
            await _next(context);

            if (context.Response.StatusCode >= 500)
            {
                WriteTrace(context, stopwatch, requestBody, null);
            }
            return;
        }

        Stream original = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            context.Response.Body = original;

            WriteTrace(context, stopwatch, requestBody, ReadResponseBody(context.Response, buffer));

            buffer.Position = 0;
            await buffer.CopyToAsync(original);
        }
    }

    private void WriteTrace(HttpContext context, Stopwatch stopwatch, string? requestBody, string? responseBody)
    {
        string? query = context.Request.QueryString.Value;
        if (query != null)
        {
            query = _formSecrets.Replace(query, "$1***");
        }

        _logger.Write(
            context.Request.Method,
            context.Request.Path.Value ?? "",
            query,
            context.Response.StatusCode,
            (int)stopwatch.ElapsedMilliseconds,
            requestBody,
            responseBody);
    }

    /// <summary>Vrai si la requête doit être tracée en entier : traçage activé, méthode suivie, chemin inclus et non exclu.</summary>
    private bool ShouldTrace(HttpContext context)
    {
        if (!_options.Enabled)
        {
            return false;
        }
        if (!_methods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        PathString path = context.Request.Path;
        if (_includedPaths.Length > 0 && !StartsWithAny(path, _includedPaths))
        {
            return false;
        }
        return !StartsWithAny(path, _excludedPaths);
    }

    private static bool StartsWithAny(PathString path, PathString[] prefixes)
    {
        foreach (PathString prefix in prefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Ajoute le "/" initial manquant ("test" devient "/test") et ignore les valeurs vides.</summary>
    private static PathString[] ToPaths(List<string> values)
    {
        var paths = new List<PathString>();
        foreach (string value in values)
        {
            string trimmed = value.Trim().TrimEnd('/');
            if (trimmed.Length == 0)
            {
                continue;
            }
            if (!trimmed.StartsWith('/'))
            {
                trimmed = "/" + trimmed;
            }
            paths.Add(new PathString(trimmed));
        }
        return paths.ToArray();
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!_options.IncludeRequestBody)
        {
            return null;
        }

        bool hasBody = request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding");
        if (!hasBody)
        {
            return null;
        }
        if (!IsText(request.ContentType))
        {
            return "[contenu non texte : " + request.ContentType + "]";
        }
        if (request.ContentLength > MaxRequestBodyBytes)
        {
            return "[corps trop volumineux : " + request.ContentLength + " octets]";
        }

        request.EnableBuffering();
        string text;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, leaveOpen: true))
        {
            text = await reader.ReadToEndAsync();
        }
        request.Body.Position = 0;
        return Clean(text);
    }

    private string? ReadResponseBody(HttpResponse response, MemoryStream buffer)
    {
        if (!_options.IncludeResponseBody || buffer.Length == 0)
        {
            return null;
        }
        if (response.Headers.ContentEncoding.Count > 0)
        {
            return "[contenu compressé]";
        }
        if (!IsText(response.ContentType))
        {
            return "[contenu non texte : " + response.ContentType + "]";
        }

        return Clean(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
    }

    /// <summary>Masque les valeurs sensibles (mots de passe, jetons…) puis tronque.</summary>
    private string Clean(string text)
    {
        string masked = _jsonSecrets.Replace(text, "$1\"***\"");
        masked = _formSecrets.Replace(masked, "$1***");

        if (masked.Length > _options.MaxBodyLength)
        {
            return masked.Substring(0, _options.MaxBodyLength) + "…[tronqué]";
        }
        return masked;
    }

    private static bool IsText(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }
}
