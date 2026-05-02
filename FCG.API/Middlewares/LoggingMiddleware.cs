// ============================================================
// LoggingMiddleware.cs — Middleware de logs estruturados
//
// Registra informações de cada request e response:
// - Método HTTP (GET, POST, PUT, DELETE)
// - Path da requisição
// - Status code da resposta
// - Tempo de execução em milissegundos
//
// Logs estruturados facilitam monitoramento e diagnóstico
// de problemas em produção.
// ============================================================

using System.Diagnostics;

namespace FCG.API.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(
        RequestDelegate next,
        ILogger<LoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Inicia o cronômetro para medir o tempo de execução
        var stopwatch = Stopwatch.StartNew();

        // Registra o início do request
        _logger.LogInformation(
            "Request iniciado: {Metodo} {Path}",
            context.Request.Method,
            context.Request.Path);

        // Passa para o próximo middleware
        await _next(context);

        // Para o cronômetro após o response ser gerado
        stopwatch.Stop();

        // Registra o fim do request com status e tempo
        _logger.LogInformation(
            "Request concluído: {Metodo} {Path} → {StatusCode} em {Tempo}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}