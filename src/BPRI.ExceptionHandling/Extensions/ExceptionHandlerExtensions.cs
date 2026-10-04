using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BPRI.ExceptionHandling;

public static class ExceptionHandlerExtensions
{
    /// <summary>Charge la configuration de gestion d'erreurs depuis <c>exceptionhandling.json</c> puis <c>exceptionhandling.{Environment}.json</c> (optionnel), à appeler avant <see cref="AddBpriExceptionHandling"/>.</summary>
    public static IConfigurationBuilder AddBpriExceptionHandlingFile(
        this IConfigurationBuilder configuration, IHostEnvironment environment, string fileName = "exceptionhandling")
    {
        configuration.AddJsonFile($"{fileName}.json", optional: false, reloadOnChange: false);
        configuration.AddJsonFile($"{fileName}.{environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
        return configuration;
    }

    /// <summary>Lit la section <c>ExceptionHandling</c> et prépare le logger (fichier / SQL Server / e-mail selon la configuration).</summary>
    public static IServiceCollection AddBpriExceptionHandling(
        this IServiceCollection services, IConfiguration configuration, Action<ExceptionHandlingOptions>? configure = null)
    {
        var options = new ExceptionHandlingOptions();
        configuration.GetSection(ExceptionHandlingOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        options.Database.ConnectionString ??= configuration.GetConnectionString(options.Database.ConnectionStringName);
        Validate(options);

        services.AddHttpContextAccessor();
        services.AddSingleton(options);
        services.AddSingleton(provider =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            string applicationName = options.ApplicationName ?? environment.ApplicationName;
            return new RequestContextEnricher(
                provider.GetRequiredService<IHttpContextAccessor>(), options, applicationName, environment.EnvironmentName);
        });
        services.AddSingleton(provider =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            return new ExceptionLogger(options, environment.ContentRootPath, provider.GetRequiredService<RequestContextEnricher>());
        });

        if (options.RequestTracing.Enabled)
        {
            services.AddSingleton(provider =>
            {
                var environment = provider.GetRequiredService<IHostEnvironment>();
                return new RequestTraceLogger(options, environment.ContentRootPath, provider.GetRequiredService<RequestContextEnricher>());
            });
        }

        if (options.ApplicationLogs.Enabled)
        {
            services.AddSingleton<ILoggerProvider>(provider =>
                new ApplicationLogProvider(provider.GetRequiredService<ExceptionLogger>(), options.ApplicationLogs));
        }

        return services;
    }

    /// <summary>À placer en premier dans le pipeline HTTP.</summary>
    public static IApplicationBuilder UseBpriExceptionHandling(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ExceptionLogger>();
        var options = app.ApplicationServices.GetRequiredService<ExceptionHandlingOptions>();

        if (options.RequestTracing.Enabled)
        {
            var traceLogger = app.ApplicationServices.GetRequiredService<RequestTraceLogger>();
            app.UseMiddleware<RequestTracingMiddleware>(traceLogger, options.RequestTracing);
        }

        return app.UseMiddleware<ExceptionHandlingMiddleware>(logger, options);
    }

    private static void Validate(ExceptionHandlingOptions options)
    {
        if (options.Database.Enabled && string.IsNullOrWhiteSpace(options.Database.ConnectionString))
        {
            throw new InvalidOperationException(
                "ExceptionHandling:Database est activé mais aucune chaîne de connexion n'est définie (ExceptionHandling:Database:ConnectionString ou ConnectionStrings:"
                + options.Database.ConnectionStringName + ").");
        }

        if (options.ApplicationLogs.Enabled && !options.Database.Enabled)
        {
            throw new InvalidOperationException("ExceptionHandling:ApplicationLogs nécessite ExceptionHandling:Database:Enabled = true.");
        }

        RequestTracingOptions tracing = options.RequestTracing;
        if (tracing.Enabled)
        {
            if (!tracing.File.Enabled && !tracing.Database.Enabled)
            {
                throw new InvalidOperationException("ExceptionHandling:RequestTracing est activé : activez File et/ou Database.");
            }
            if (tracing.Database.Enabled && string.IsNullOrWhiteSpace(options.Database.ConnectionString))
            {
                throw new InvalidOperationException(
                    "ExceptionHandling:RequestTracing:Database est activé mais aucune chaîne de connexion n'est définie (ConnectionStrings:"
                    + options.Database.ConnectionStringName + ").");
            }
        }

        var email = options.Email;
        if (email.Enabled && (string.IsNullOrWhiteSpace(email.Host) || string.IsNullOrWhiteSpace(email.From) || email.To.Count == 0))
        {
            throw new InvalidOperationException("ExceptionHandling:Email est activé : Host, From et To sont obligatoires.");
        }
    }
}
