namespace Catalog.Api.Common;

using BPRI.ExceptionHandling;
using FluentValidation;

public static class ExceptionHandlingSetup
{
    // FluentValidation -> 400 avec les erreurs par champ.
    public static ErrorInfo? MapValidation(Exception exception)
    {
        if (exception is not ValidationException validationException)
        {
            return null;
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var group in validationException.Errors.GroupBy(e => e.PropertyName))
        {
            errors[group.Key] = group.Select(e => e.ErrorMessage).ToArray();
        }

        return new ErrorInfo(
            StatusCodes.Status400BadRequest,
            "Les donnees saisies sont invalides. Veuillez corriger les erreurs.",
            errors);
    }

    public static Task WriteResponse(HttpContext context, ErrorInfo error, string traceId)
    {
        var response = new ApiResponse<object>();
        response.TraceId = traceId;
        response.Message = error.Message;

        if (error.Errors != null)
        {
            response.ValidationErrors = error.Errors.ToDictionary(e => e.Key, e => e.Value);
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
