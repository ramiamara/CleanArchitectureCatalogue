namespace BPRI.LogsPlatform.Back.Errors;

/// <summary>Erreur attendue (400, 404...) : le message est renvoyé tel quel à l'appelant.</summary>
public sealed class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
