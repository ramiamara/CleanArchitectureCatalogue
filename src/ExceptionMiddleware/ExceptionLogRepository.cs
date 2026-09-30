internal sealed class ExceptionLogRepository : IExceptionLogRepository
{
    private readonly BackBoneDbContext _context;

    public ExceptionLogRepository(BackBoneDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        ExceptionLog exceptionLog,
        CancellationToken cancellationToken = default)
    {
        await _context.ExceptionLogs.AddAsync(exceptionLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
