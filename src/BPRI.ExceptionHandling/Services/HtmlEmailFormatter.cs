using System.Net;
using Serilog.Events;
using Serilog.Formatting;

namespace BPRI.ExceptionHandling;

/// <summary>Met en forme un log en HTML pour l'e-mail. Modèle par défaut : Templates/ErrorMail.html (ressource intégrée), ou fichier via Email:TemplatePath ({{Nom}} = valeur encodée).</summary>
internal sealed class HtmlEmailFormatter : ITextFormatter
{
    private readonly string _template;

    public HtmlEmailFormatter(string? templatePath)
    {
        _template = string.IsNullOrEmpty(templatePath) ? ReadDefaultTemplate() : File.ReadAllText(templatePath);
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        string userName = GetProperty(logEvent, "UserName");
        string user = userName.Length > 0 ? userName + " (" + GetProperty(logEvent, "UserId") + ")" : "-";

        var values = new Dictionary<string, string>();
        values["Level"] = logEvent.Level.ToString();
        values["Timestamp"] = logEvent.Timestamp.ToString("dd/MM/yyyy HH:mm:ss zzz");
        values["ApplicationName"] = GetProperty(logEvent, "ApplicationName");
        values["Cprj"] = GetProperty(logEvent, "Cprj");
        values["TraceId"] = GetProperty(logEvent, "TraceId");
        values["HttpMethod"] = GetProperty(logEvent, "HttpMethod");
        values["Path"] = GetProperty(logEvent, "Path");
        values["StatusCode"] = GetProperty(logEvent, "StatusCode");
        values["ExceptionType"] = GetProperty(logEvent, "ExceptionType");
        values["User"] = user;
        values["Environment"] = GetProperty(logEvent, "EnvironmentName") + " / " + GetProperty(logEvent, "MachineName");
        values["Message"] = logEvent.RenderMessage();
        values["Exception"] = logEvent.Exception?.ToString() ?? "";

        string html = _template.Replace("{{Color}}", GetColor(logEvent.Level));
        foreach (KeyValuePair<string, string> item in values)
        {
            html = html.Replace("{{" + item.Key + "}}", WebUtility.HtmlEncode(item.Value));
        }

        output.Write(html);
    }

    private static string GetColor(LogEventLevel level)
    {
        return level >= LogEventLevel.Error ? "#d93025" : level == LogEventLevel.Warning ? "#f29900" : "#1a73e8";
    }

    private static string GetProperty(LogEvent logEvent, string name)
    {
        if (logEvent.Properties.TryGetValue(name, out LogEventPropertyValue? value) && value is ScalarValue scalar && scalar.Value != null)
        {
            return scalar.Value.ToString() ?? "";
        }
        return "";
    }

    private static string ReadDefaultTemplate()
    {
        using Stream stream = typeof(HtmlEmailFormatter).Assembly.GetManifestResourceStream("BPRI.ExceptionHandling.Templates.ErrorMail.html")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
