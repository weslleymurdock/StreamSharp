using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreamSharp.Middlewares;

/// <summary>
/// Handler global de exceções que captura erros não tratados e formata a resposta no padrão RFC 7807 (Problem Details).
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Uma exceção não tratada foi capturada pelo GlobalExceptionHandler: {Message}", exception.Message);

        // Mapeia o código de status HTTP com base no tipo da exceção
        var (statusCode, title) = exception switch
        {
            ArgumentException or ArgumentNullException => (StatusCodes.Status400BadRequest, "Requisição Inválida"),
            FileNotFoundException => (StatusCodes.Status404NotFound, "Recurso Não Encontrado"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Acesso Não Autorizado"),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Falha no Processamento"),
            _ => (StatusCodes.Status500InternalServerError, "Erro Interno do Servidor")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        // Inclui a StackTrace apenas se o ambiente for Desenvolvimento
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Retorna true indicando que a exceção foi tratada com sucesso e não deve continuar a propagação
        return true;
    }
}