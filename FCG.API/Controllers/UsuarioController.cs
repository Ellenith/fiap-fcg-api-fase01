// ============================================================
// UsuarioController.cs — Endpoints de gerenciamento de usuários
//
// Endpoints:
// POST   /usuarios          → cadastro (público)
// GET    /usuarios          → listar todos (Admin)
// GET    /usuarios/{id}     → buscar por id (autenticado)
// PUT    /usuarios/{id}     → atualizar perfil (autenticado)
// ============================================================

using FCG.API.Domain.Identidade;
using FCG.API.Domain.SharedKernel;
using FCG.API.Infraestrutura.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.API.Controllers;

[ApiController]
[Route("usuarios")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioRepositorio _repositorio;
    private readonly PasswordHasherServico _passwordHasher;

    public UsuarioController(
        IUsuarioRepositorio repositorio,
        PasswordHasherServico passwordHasher)
    {
        _repositorio    = repositorio;
        _passwordHasher = passwordHasher;
    }

    // ----------------------------------------------------------
    // POST /usuarios
    // Cadastro público — não requer autenticação
    //
    // Fluxo:
    // 1. Valida se e-mail já existe
    // 2. Gera hash da senha
    // 3. Cria o usuário via factory method (valida regras de domínio)
    // 4. Salva no banco
    // ----------------------------------------------------------
    [HttpPost]
    [EndpointSummary("Cadastrar usuário")]
    [EndpointDescription("Cadastra um novo usuário. O primeiro usuário cadastrado recebe a role Admin automaticamente.")]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarUsuarioRequest request)
    {
        // Verifica se o e-mail já está em uso
        var emailExiste = await _repositorio.EmailExisteAsync(request.Email);
        if (emailExiste)
            throw new DomainException("E-mail já cadastrado.");

        // Gera o hash da senha — a validação de força ocorre
        // dentro de Usuario.Create() via ValidarSenha()
        var hash = _passwordHasher.GerarHash(request.Senha);

        // Verifica se é o primeiro usuário do banco
        // Se sim, ele é criado automaticamente como Admin
        var role = !await _repositorio.ExisteAlgumUsuarioAsync() ? "Admin" : "User";

        // Cria o usuário via factory method
        var usuario = Usuario.Create(
            request.Nome,
            request.Email,
            request.Senha,
            hash,
            role);

        // Persiste no banco
        await _repositorio.AdicionarAsync(usuario);
        await _repositorio.SalvarAsync();

        return CreatedAtAction(
            nameof(BuscarPorId),
            new { id = usuario.Id },
            new UsuarioResponse(usuario));
    }

    // ----------------------------------------------------------
    // GET /usuarios
    // Lista todos os usuários — somente Admin
    // ----------------------------------------------------------
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Listar todos os usuários")]
    [EndpointDescription("Lista todos os usuários cadastrados. Apenas usuários com role Admin podem acessar esta funcionalidade.")]
    public async Task<IActionResult> ListarTodos()
    {
        var usuarios = await _repositorio.ListarTodosAsync();
        return Ok(usuarios.Select(u => new UsuarioResponse(u)));
    }

    // ----------------------------------------------------------
    // GET /usuarios/{id}
    // Busca um usuário pelo Id — autenticado
    // Usuário só pode ver o próprio perfil
    // Admin pode ver qualquer usuário
    // ----------------------------------------------------------
    [HttpGet("{id:guid}")]
    [Authorize]
    [EndpointSummary("Buscar usuário por Id")]
    [EndpointDescription("Busca um usuário específico pelo seu Id. O usuário só pode ver o próprio perfil, enquanto o Admin pode visualizar qualquer usuário.")]
    public async Task<IActionResult> BuscarPorId(Guid id)
    {
        // Extrai o Id do usuário logado do token JWT
        var usuarioLogadoId = ObterUsuarioLogadoId();

        // Verifica permissão — Admin pode ver qualquer perfil
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && usuarioLogadoId != id)
            return Forbid();

        var usuario = await _repositorio.BuscarPorIdAsync(id);
        if (usuario is null)
            return NotFound("Usuário não encontrado.");

        return Ok(new UsuarioResponse(usuario));
    }

    // ----------------------------------------------------------
    // PUT /usuarios/{id}
    // Atualiza nome e senha — autenticado
    // Usuário só pode atualizar o próprio perfil
    // Admin pode atualizar qualquer usuário
    // ----------------------------------------------------------
    [HttpPut("{id:guid}")]
    [Authorize]
    [EndpointSummary("Atualizar usuário")]
    [EndpointDescription("Atualiza o nome e senha de um usuário específico. O usuário só pode atualizar o próprio perfil, enquanto o Admin pode atualizar qualquer usuário.")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarUsuarioRequest request)
    {
        // Verifica permissão
        var usuarioLogadoId = ObterUsuarioLogadoId();
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && usuarioLogadoId != id)
            return Forbid();

        var usuario = await _repositorio.BuscarPorIdAsync(id);
        if (usuario is null)
            return NotFound("Usuário não encontrado.");

        // Gera hash da nova senha
        // A validação de força ocorre dentro de UpdateProfile()
        var novoHash = _passwordHasher.GerarHash(request.Senha);

        // Atualiza via método do domínio — respeita as regras de negócio
        usuario.UpdateProfile(request.Nome, request.Senha, novoHash);

        _repositorio.Atualizar(usuario);
        await _repositorio.SalvarAsync();

        return Ok(new UsuarioResponse(usuario));
    }

    // ----------------------------------------------------------
    // Método auxiliar — extrai o Id do usuário logado do JWT
    // O claim "sub" contém o Id gravado no token durante o login
    // ----------------------------------------------------------
    private Guid ObterUsuarioLogadoId()
    {
        // Tenta primeiro pelo ClaimTypes.NameIdentifier
        // e depois pelo claim "sub" padrão JWT
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.Parse(sub!);
    }
}

// ==========================================================
// DTOs — Data Transfer Objects
//
// Separam o contrato da API das entidades do domínio.
// O cliente nunca recebe a entidade diretamente —
// apenas os dados necessários para cada operação.
// ==========================================================

// Request de cadastro
public record CadastrarUsuarioRequest(
    string Nome,
    string Email,
    string Senha);

// Request de atualização
public record AtualizarUsuarioRequest(
    string Nome,
    string Senha);


// Response padrão — nunca expõe o PasswordHash
public record UsuarioResponse(
    Guid     Id,
    string   Nome,
    string   Email,
    string   Role,
    DateTime CriadoEm,
    string   Mensagem)
{
    // Construtor que recebe a entidade e mapeia para o DTO
    // A mensagem varia conforme o role atribuído
    public UsuarioResponse(Usuario u)
        : this(
            u.Id,
            u.Name,
            u.Email,
            u.Role,
            u.CreatedAt.ToLocalTime(),  // UTC → horário local
            u.Role == "Admin"
                ? "Usuário administrador criado com sucesso!"
                : "Usuário criado com sucesso!") { }
}