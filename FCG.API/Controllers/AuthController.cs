// ============================================================
// AuthController.cs — Endpoint de autenticação
//
// Endpoints:
// POST /auth/login → valida credenciais e retorna JWT
//
// Este é o primeiro endpoint a ser testado no Swagger —
// o token retornado aqui é usado em todos os outros
// endpoints protegidos via botão Authorize no Swagger
// ============================================================

using FCG.API.Domain.Identidade;
using FCG.API.Domain.SharedKernel;
using FCG.API.Infraestrutura.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace FCG.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioRepositorio _repositorio;
    private readonly PasswordHasherServico _passwordHasher;
    private readonly JwtServico _jwtServico;

    public AuthController(
        IUsuarioRepositorio repositorio,
        PasswordHasherServico passwordHasher,
        JwtServico jwtServico)
    {
        _repositorio    = repositorio;
        _passwordHasher = passwordHasher;
        _jwtServico     = jwtServico;
    }

    // ----------------------------------------------------------
    // POST /auth/login
    // Público — não requer autenticação
    //
    // Fluxo:
    // 1. Busca o usuário pelo e-mail
    // 2. Verifica se a senha informada corresponde ao hash salvo
    // 3. Gera e retorna o token JWT
    //
    // Por segurança, retornamos a mesma mensagem de erro
    // tanto para e-mail não encontrado quanto para senha errada.
    // Isso evita que um atacante descubra se um e-mail
    // está ou não cadastrado na plataforma.
    // ----------------------------------------------------------
    [HttpPost("login")]
    [EndpointSummary("Autenticar usuário")]
    [EndpointDescription("Valida e-mail e senha e retorna um token JWT para uso nos endpoints protegidos.")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Busca o usuário pelo e-mail
        var usuario = await _repositorio.BuscarPorEmailAsync(request.Email);

        // Mensagem genérica para não revelar se o e-mail existe
        if (usuario is null)
            throw new DomainException("E-mail ou senha inválidos.");

        // Verifica se a senha informada corresponde ao hash salvo
        var senhaValida = _passwordHasher.VerificarSenha(
            request.Senha,
            usuario.PasswordHash);

        if (!senhaValida)
            throw new DomainException("E-mail ou senha inválidos.");

        // Gera o token JWT com as claims do usuário
        var token = _jwtServico.GerarToken(usuario);

        // Retorna o token e informações básicas do usuário
        return Ok(new LoginResponse(
            token,
            usuario.Id,
            usuario.Name,
            usuario.Email,
            usuario.Role));
    }
}

// ==========================================================
// DTOs
// ==========================================================

// Request de login
public record LoginRequest(
    string Email,
    string Senha);

// Response do login — contém o token e dados do usuário
public record LoginResponse(
    string Token,
    Guid   Id,
    string Nome,
    string Email,
    string Role);