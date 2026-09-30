using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class ExceptionHandlerExtensions
{
    /// <summary>
    /// Enregistre les services du middleware de gestion des exceptions :
    /// <list type="bullet">
    ///   <item>Options <see cref="ExceptionHandlingOptions"/> (section <c>ExceptionHandling</c>)</item>
    ///   <item>Options <see cref="ExceptionMailOptions"/> (section <c>ExceptionMail</c>)</item>
    ///   <item><see cref="IExceptionLogRepository"/> → <c>ExceptionLogRepository</c> (Scoped)</item>
    ///   <item><see cref="IExceptionMailService"/> → <c>ExceptionMailService</c> (Singleton)</item>
    /// </list>
    /// </summary>
    /// <param name="services">Collection de services DI.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddBpriExceptionHandling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Binding des options depuis appsettings.json via BindConfiguration (Sonar : S4792)
        services.AddOptions<ExceptionHandlingOptions>()
            .BindConfiguration(ExceptionHandlingOptions.SectionName)
            .ValidateOnStart();

        services.AddOptions<ExceptionMailOptions>()
            .BindConfiguration(ExceptionMailOptions.SectionName)
            .ValidateOnStart();

        // Repository : Scoped (cycle de vie lié à la requête HTTP)
        services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();

        // HttpClient dédié au chargement du template depuis une URL HTTP/HTTPS
        services.AddHttpClient<MailTemplateLoader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // MailTemplateLoader : Singleton (cache du template après le premier chargement)
        services.AddSingleton<MailTemplateLoader>(sp =>
        {
            var options    = sp.GetRequiredService<IOptions<ExceptionMailOptions>>();
            var logger     = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MailTemplateLoader>>();
            var httpClient = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>()
                              .CreateClient(nameof(MailTemplateLoader));
            return new MailTemplateLoader(options, logger, httpClient);
        });

        // Service mail : Singleton (Polly pipeline partagé)
        services.AddSingleton<IExceptionMailService, ExceptionMailService>();

        return services;
    }

    /// <summary>
    /// Ajoute le <see cref="GlobalExceptionMiddleware"/> au pipeline HTTP.
    /// Doit être appelé en <b>premier</b> dans <c>Program.cs</c>
    /// pour intercepter toutes les exceptions du pipeline.
    /// </summary>
    /// <param name="app">Le builder de l'application.</param>
    /// <returns>Le builder pour le chaînage.</returns>
    public static IApplicationBuilder UseBpriExceptionHandling(
        this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
