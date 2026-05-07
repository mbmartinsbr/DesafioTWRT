using System.Net;
using System.Text.Json;
using GestaoPedidos.Application.Exceptions;

namespace GestaoPedidos.API.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, tipo, mensagem) = exception switch
        {
            NotFoundException e  => (HttpStatusCode.NotFound, "not_found", e.Message),
            ConflictException e  => (HttpStatusCode.Conflict, "conflict", e.Message),
            BusinessException e  => (HttpStatusCode.BadRequest, "business_error", e.Message),
            _                    => (HttpStatusCode.InternalServerError, "internal_error", "Erro interno do servidor.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new { tipo, mensagem, erros = Array.Empty<string>() },
            _jsonOptions);

        await context.Response.WriteAsync(body);
    }
}
