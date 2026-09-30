using Microsoft.EntityFrameworkCore;

namespace BPRI.LogsPlatform.Back.Data;

public sealed class LogEntry
{
    public int Id { get; set; }
    public string? Message { get; set; }
    public string? Level { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? TraceId { get; set; }
    public string? Cprj { get; set; }
    public string? ApplicationName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? MachineName { get; set; }
    public string? SourceContext { get; set; }
    public int? StatusCode { get; set; }
    public string? ExceptionType { get; set; }
    public string? InnerException { get; set; }
    public string? Fingerprint { get; set; }
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? UserName { get; set; }
    public string? UserId { get; set; }
    public string? Claims { get; set; }
}

public sealed class LogsDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public LogsDbContext(DbContextOptions<LogsDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<LogEntry> Logs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        string table = _configuration["LogsTable:TableName"] ?? "ExceptionLogs";
        string schema = _configuration["LogsTable:SchemaName"] ?? "dbo";

        modelBuilder.Entity<LogEntry>().ToTable(table, schema);
        modelBuilder.Entity<LogEntry>().HasKey(x => x.Id);
    }
}
