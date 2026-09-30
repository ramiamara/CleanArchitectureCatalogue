public sealed class ExceptionLog : AuditableEntity
{
    public int Id { get; set; }

    public required string Message { get; set; }
    public string? StackTrace { get; set; }
    public string? Path { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }

    /// <summary>Login Windows du collaborateur connecté au moment de l'exception (nullable)</summary>
    public string? Username { get; set; }

    /// <summary>Code projet source de l'exception</summary>
    public required string Cprj { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
