// ============================================================
// ErrorHandlingMiddleware.cs — Middleware de tratamento de erros
//
// Captura todas as exceções não tratadas da aplicação e
// retorna uma resposta JSON padronizada.
//
// Sem este middleware, erros retornariam HTML ou respostas
// sem formato definido — difícil de tratar no cliente.
//
// Mapeamento de exceções para status HTTP:
// DomainException      → 400 Bad Request
// UnauthorizedAccess   → 401 Unauthorized
// KeyNotFoundException → 404 Not Found
// Qualquer outra       → 500 Internal Server Error
// ============================================================

using FCG.API.Domain.SharedKernel;
using System.Net;
using System.Text.Json;

namespace FCG.API.Middlewares;

public class ErrorHandlingMiddleware
{
    // Referência ao próximo middleware no pipeline
    private readonly RequestDelegate _next;

    // ILogger para registrar os erros no log da aplicação
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    // ----------------------------------------------------------
    // InvokeAsync — chamado para cada request HTTP
    //
    // Envolve o próximo middleware em um try/catch.
    // Se nenhuma exceção ocorrer, o request segue normalmente.
    // Se uma exceção ocorrer, é capturada e tratada aqui.
    // ----------------------------------------------------------
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Passa o request para o próximo middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            // Registra o erro no log com todos os detalhes
            _logger.LogError(ex, "Erro não tratado: {Mensagem}", ex.Message);

            // Trata o erro e retorna resposta padronizada
            await TratarExcecaoAsync(context, ex);
        }
    }

    // ----------------------------------------------------------
    // TratarExcecaoAsync — mapeia exceções para respostas HTTP
    // ----------------------------------------------------------
    private static async Task TratarExcecaoAsync(
        HttpContext context,
        Exception exception)
    {
        // Define o status code e mensagem conforme o tipo de exceção
        var (statusCode, mensagem) = exception switch
        {
            // Regras de negócio violadas → 400 Bad Request
            DomainException domainEx
                => (HttpStatusCode.BadRequest, domainEx.Message),

            // Acesso não autorizado → 401 Unauthorized
            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized, "Acesso não autorizado."),

            // Recurso não encontrado → 404 Not Found
            KeyNotFoundException
                => (HttpStatusCode.NotFound, "Recurso não encontrado."),

            // Qualquer outro erro → 500 Internal Server Error
            // A mensagem genérica evita expor detalhes internos
            _   => (HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno. Tente novamente mais tarde.")
        };

        // Define o tipo de conteúdo da resposta como JSON
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        // Monta o objeto de resposta padronizado
        var resposta = new ErroResponse(
            (int)statusCode,
            mensagem,
            DateTime.UtcNow);

        // Serializa para JSON com nomes em camelCase
        var json = JsonSerializer.Serialize(resposta, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

// ==========================================================
// DTO de resposta de erro
// Formato padronizado retornado em todos os erros da API
// ==========================================================
public record ErroResponse(
    int      StatusCode,
    string   Mensagem,
    DateTime Timestamp);