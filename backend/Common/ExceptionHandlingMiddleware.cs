using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Common;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Erreur lors de l'enregistrement en base de données.");
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "L'opération ne peut pas être enregistrée en base de données.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erreur non gérée lors du traitement de la requête.");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Une erreur interne est survenue.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Requête invalide",
                StatusCodes.Status404NotFound => "Ressource introuvable",
                StatusCodes.Status409Conflict => "Conflit",
                _ => "Erreur interne"
            },
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
