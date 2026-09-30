namespace BPRI.ExceptionHandling;

/// <summary>Réponse HTTP décidée pour une exception.</summary>
public record ErrorInfo(int StatusCode, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>Exception métier dont le message est renvoyé tel quel au client, avec le statut HTTP choisi (404, 403, 409, 422…).</summary>
public class AppException : Exception
{
    public AppException(string message, int statusCode = 400, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
