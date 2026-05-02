// ============================================================
// BibliotecaController.cs — Endpoints da biblioteca de jogos
//
// Endpoints:
// GET    /biblioteca          → listar jogos adquiridos (autenticado)
// POST   /biblioteca          → adquirir jogo (autenticado)
// ============================================================

using FCG.API.Domain.Biblioteca;
using FCG.API.Domain.Catalogo;
using FCG.API.Domain.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.API.Controllers;

[ApiController]
[Route("biblioteca")]
[Authorize] // Todos os endpoints exigem autenticação
public class BibliotecaController : ControllerBase
{
    private readonly IBibliotecaRepositorio _bibliotecaRepositorio;
    private readonly IJogoRepositorio _jogoRepositorio;

    public BibliotecaController(
        IBibliotecaRepositorio bibliotecaRepositorio,
        IJogoRepositorio jogoRepositorio)
    {
        _bibliotecaRepositorio = bibliotecaRepositorio;
        _jogoRepositorio       = jogoRepositorio;
    }

    // ----------------------------------------------------------
    // GET /biblioteca
    // Lista todos os jogos adquiridos pelo usuário logado
    //
    // O usuário só vê os próprios jogos — o filtro é feito
    // pelo UsuarioId extraído do token JWT, nunca pelo
    // Id informado na URL (evita acesso a dados de outros)
    // ----------------------------------------------------------
    [HttpGet]
    [EndpointSummary("Listar jogos adquiridos")]
    [EndpointDescription("Lista todos os jogos adquiridos pelo usuário logado.")]
    public async Task<IActionResult> ListarJogosAdquiridos()
    {
        // Extrai o Id do usuário logado do token JWT
        var usuarioId = ObterUsuarioLogadoId();

        // Busca apenas os jogos do usuário logado
        var aquisicoes = await _bibliotecaRepositorio
            .ListarPorUsuarioAsync(usuarioId);

        return Ok(aquisicoes.Select(a => new BibliotecaResponse(a)));
    }

    // ----------------------------------------------------------
    // POST /biblioteca
    // Registra a aquisição de um jogo pelo usuário logado
    //
    // Fluxo:
    // 1. Extrai o Id do usuário logado do JWT
    // 2. Verifica se o jogo existe
    // 3. Verifica se o usuário já possui o jogo
    // 4. Registra a aquisição com o preço atual do jogo
    // 5. Salva no banco
    // ----------------------------------------------------------
    [HttpPost]
    [EndpointSummary("Adquirir jogo")]
    [EndpointDescription("Registra a aquisição de um jogo pelo usuário logado.")]
    public async Task<IActionResult> AdquirirJogo([FromBody] AdquirirJogoRequest request)
    {
        // Extrai o Id do usuário logado do token JWT
        var usuarioId = ObterUsuarioLogadoId();

        // Verifica se o jogo existe no catálogo
        var jogo = await _jogoRepositorio.BuscarPorIdAsync(request.JogoId);
        if (jogo is null)
            return NotFound("Jogo não encontrado no catálogo.");

        // Verifica se o usuário já possui o jogo
        // Regra mais importante do contexto Biblioteca
        var jogoJaAdquirido = await _bibliotecaRepositorio
            .JogoJaAdquiridoAsync(usuarioId, request.JogoId);

        if (jogoJaAdquirido)
            throw new DomainException("Você já possui este jogo na sua biblioteca.");

        // Registra a aquisição com o preço atual do jogo
        // PrecoPago é uma fotografia do preço neste momento —
        // não será afetado por futuras alterações de preço
        var aquisicao = Biblioteca.Registrar(
            usuarioId,
            request.JogoId,
            jogo.Preco);

        // Persiste no banco
        await _bibliotecaRepositorio.AdicionarAsync(aquisicao);
        await _bibliotecaRepositorio.SalvarAsync();

        return CreatedAtAction(
            nameof(ListarJogosAdquiridos),
            new BibliotecaResponse(
                aquisicao,
                jogo.Titulo,
                "Jogo adquirido com sucesso!"));
    }

    // ----------------------------------------------------------
    // Método auxiliar — extrai o Id do usuário logado do JWT
    // ----------------------------------------------------------
    private Guid ObterUsuarioLogadoId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.Parse(sub!);
    }
}

// ==========================================================
// DTOs
// ==========================================================

// Request de aquisição
public record AdquirirJogoRequest(Guid JogoId);

// Response da biblioteca
public record BibliotecaResponse(
    Guid     Id,
    Guid     UsuarioId,
    Guid     JogoId,
    string   TituloJogo,
    decimal  PrecoPago,
    DateTime AdquiridoEm,
    string?  Mensagem = null)
{
    // Construtor sem título — usado na listagem
    // O título é carregado via Include do repositório
    public BibliotecaResponse(Biblioteca b, string? mensagem = null)
        : this(
            b.Id,
            b.UsuarioId,
            b.JogoId,
            b.Jogo?.Titulo ?? string.Empty,   // título carregado via Include
            b.PrecoPago,
            b.AdquiridoEm.ToLocalTime(),  // UTC → horário local
            mensagem) { }

    // Construtor com título — usado no retorno da aquisição
    // onde o jogo já foi carregado para verificação
    public BibliotecaResponse(Biblioteca b, string tituloJogo, string? mensagem = null)
        : this(
            b.Id,
            b.UsuarioId,
            b.JogoId,
            tituloJogo,
            b.PrecoPago,
            b.AdquiridoEm.ToLocalTime(),  // UTC → horário local
            mensagem) { }
}