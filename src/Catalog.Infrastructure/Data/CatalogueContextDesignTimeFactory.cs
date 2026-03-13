namespace Catalog.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Factory used by dotnet-ef at design time (migrations add, database update, etc.)
/// so that the tool does not need to launch the API startup project.
/// </summary>
public class CatalogueContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogueContext>
{
    public CatalogueContext CreateDbContext(string[] args)
    {
        // Try to read connection string from the API appsettings for convenience,
        // but fall back to the hard-coded local dev default.
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Catalog.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            config.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=CatalogueDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<CatalogueContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new CatalogueContext(optionsBuilder.Options);
    }
}
