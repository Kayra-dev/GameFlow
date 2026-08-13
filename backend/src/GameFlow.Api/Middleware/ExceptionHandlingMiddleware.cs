using System.Text.Json;
using FluentValidation;
using GameFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Middleware;

/// <summary>
/// Tüm işlenmeyen istisnaları yakalayıp ProblemDetails biçiminde tek tip yanıta çevirir.
/// Böylece istemci her hatayı aynı şekilde ele alabilir ve iç detaylar dışarı sızmaz.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteResponseAsync(context, exception);
        }
    }

    private async Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "İstek işlenirken beklenmeyen hata oluştu: {Path}", context.Request.Path);
        }
        else
        {
            logger.LogWarning("İstek reddedildi ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        if (environment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            problem.Detail = exception.ToString();
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static (int StatusCode, string Title, IDictionary<string, string[]>? Errors) Map(Exception exception)
        => exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Girdiğiniz bilgiler geçerli değil.",
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, null),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message, null),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, null),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message, null),
            // 499: Nginx uyumlu "client closed request".
            OperationCanceledException => (499, "İstek iptal edildi.", null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucuda beklenmeyen bir hata oluştu.",
                null)
        };
}
