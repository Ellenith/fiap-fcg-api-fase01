// ============================================================
// JogoController.cs — Endpoints de gerenciamento de jogos
//
// Endpoints:
// POST   /jogos          → cadastrar jogo (Admin)
// GET    /jogos          → listar todos (público)
// GET    /jogos/{id}     → buscar por id (público)
// PUT    /jogos/{id}     → atualizar preço (Admin)
// DELETE /jogos/{id}     → remover jogo (Admin)
// ============================================================

using FCG.API.Domain.Catalogo;
using FCG.API.Domain.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FCG.API.Infraestrutura.Persistencia;

namespace FCG.API.Controllers;

[ApiController]
[Route("jogos")]
public class JogoController : ControllerBase
{
    private readonly IJogoRepositorio _repositorio;
    private readonly FGCDbContext _context;
    public JogoController(IJogoRepositorio repositorio, FGCDbContext context)
    {
        _repositorio = repositorio;
        _context     = context;
    }

    // ----------------------------------------------------------
    // POST /jogos
    // Cadastra um novo jogo — somente Admin
    //
    // Fluxo:
    // 1. Verifica se já existe jogo com o mesmo título
    // 2. Cria o jogo via factory method
    // 3. Salva no banco
    // ----------------------------------------------------------
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Cadastrar jogo")]
    [EndpointDescription("Cadastra um novo jogo. Apenas usuários com role Admin podem acessar esta funcionalidade.")]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarJogoRequest request)
    {
        // Verifica duplicata de título
        var tituloExiste = await _repositorio.TituloExisteAsync(request.Titulo);
        if (tituloExiste)
            throw new DomainException("Já existe um jogo com este título.");

        // Cria o jogo via factory method — valida regras de domínio
        var jogo = Jogo.Criar(
            request.Titulo,
            request.Descricao,
            request.Preco);

        // Persiste no banco
        await _repositorio.AdicionarAsync(jogo);
        await _repositorio.SalvarAsync();

        // Retorna 201 Created com os dados do jogo criado
        return CreatedAtAction(
            nameof(BuscarPorId),
            new { id = jogo.Id },
            new JogoResponse(jogo, "Jogo cadastrado com sucesso!"));
    }

 // GET /jogos — agora retorna PrecoAtual
    [HttpGet]
    [EndpointSummary("Listar todos os jogos")]
    [EndpointDescription("Lista todos os jogos disponíveis. Apenas usuários autenticados podem acessar esta funcionalidade.")]
    public async Task<IActionResult> ListarTodos()
    {
        var jogos = await _repositorio.ListarTodosComPromocoesAsync();
        return Ok(jogos.Select(j => new JogoResponse(j)));
    }

    // GET /jogos/{id} — agora retorna PrecoAtual
    [HttpGet("{id:guid}")]
    [EndpointSummary("Buscar jogo por Id")]
    [EndpointDescription("Busca um jogo específico pelo seu Id. Apenas usuários autenticados podem acessar esta funcionalidade.")]  
    public async Task<IActionResult> BuscarPorId(Guid id)
    {
        var jogo = await _repositorio.BuscarPorIdComPromocoesAsync(id);
        if (jogo is null)
            return NotFound("Jogo não encontrado.");

        return Ok(new JogoResponse(jogo));
    }

    // POST /jogos/{id}/promocoes — somente Admin
    [HttpPost("{id:guid}/promocoes")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Criar promoção para um jogo")]
    [EndpointDescription("Cria uma nova promoção para um jogo específico. Apenas usuários com role Admin podem acessar esta funcionalidade.")]
    public async Task<IActionResult> CriarPromocao(
        Guid id,
        [FromBody] CriarPromocaoRequest request)
    {
        var jogo = await _repositorio.BuscarPorIdComPromocoesAsync(id);
        if (jogo is null)
            return NotFound("Jogo não encontrado.");

        // Converte as datas de horário local (UTC-3) para UTC
        // antes de salvar no banco — padrão uniforme de armazenamento
        var inicioUtc = request.Inicio.ToUniversalTime();
        var fimUtc    = request.Fim.ToUniversalTime();

        var promocao = Promocao.Criar(
            id,
            request.PercentualDesconto,
            inicioUtc,
            fimUtc);

        await _context.Promocoes.AddAsync(promocao);
        await _repositorio.SalvarAsync();

        return CreatedAtAction(
            nameof(BuscarPorId),
            new { id = jogo.Id },
            new
            {
                Mensagem = "Promoção criada com sucesso!",
                Promocao = new
                {
                    promocao.Id,
                    promocao.JogoId,
                    promocao.PercentualDesconto,
                    // Exibe as datas de volta em horário local na resposta
                    Inicio = promocao.Inicio.ToLocalTime(),
                    Fim    = promocao.Fim.ToLocalTime()
                }
            });
}


    // ----------------------------------------------------------
    // GET /jogos/promocoes
    // Lista todos os jogos que possuem promoção ativa no momento
    // Público — não requer autenticação
    // ----------------------------------------------------------
    [HttpGet("promocoes")]
    [EndpointSummary("Listar promoções ativas")]
    [EndpointDescription("Lista todos os jogos que possuem promoção ativa no momento. Esta funcionalidade é pública e não requer autenticação.")]       
    public async Task<IActionResult> ListarPromocoesAtivas()
    {
        // Carrega todos os jogos com suas promoções
        var jogos = await _repositorio.ListarTodosComPromocoesAsync();

        // Filtra apenas jogos com promoção ativa no momento
        var jogosComPromocaoAtiva = jogos
            .Where(j => j.Promocoes.Any(p => p.EstaAtiva()))
            .ToList();

        if (!jogosComPromocaoAtiva.Any())
            return Ok(new { Mensagem = "Nenhuma promoção ativa no momento.", Promocoes = Array.Empty<object>() });

        // Monta a resposta com os dados do jogo e da promoção ativa
        var resposta = jogosComPromocaoAtiva.Select(j =>
        {
            var promocaoAtiva = j.Promocoes.First(p => p.EstaAtiva());
            return new PromocaoAtivaResponse(j, promocaoAtiva);
        });

        return Ok(resposta);
    }
    // ----------------------------------------------------------
    // PUT /jogos/{id}
    // Atualiza o preço do jogo — somente Admin
    // ----------------------------------------------------------
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Atualizar preço do jogo")]
    [EndpointDescription("Atualiza o preço de um jogo específico. Apenas usuários com role Admin podem acessar esta funcionalidade.")]  
    public async Task<IActionResult> AtualizarPreco(
        Guid id,
        [FromBody] AtualizarPrecoRequest request)
    {
        var jogo = await _repositorio.BuscarPorIdAsync(id);
        if (jogo is null)
            return NotFound("Jogo não encontrado.");

        // Atualiza via método do domínio — valida preço ≥ 0
        jogo.AtualizarPreco(request.Preco);

        _repositorio.Atualizar(jogo);
        await _repositorio.SalvarAsync();

        return Ok(new JogoResponse(jogo, "Preço atualizado com sucesso!"));
    }

    // ----------------------------------------------------------
    // DELETE /jogos/{id}
    // Remove um jogo — somente Admin
    // ----------------------------------------------------------
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Remover jogo")]
    [EndpointDescription("Remove um jogo específico do catálogo. Apenas usuários com role Admin podem acessar esta funcionalidade.")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var jogo = await _repositorio.BuscarPorIdAsync(id);
        if (jogo is null)
            return NotFound("Jogo não encontrado.");

        _repositorio.Remover(jogo);
        await _repositorio.SalvarAsync();

        return Ok(new { Mensagem = "Jogo removido com sucesso!" });
    }
}

// ==========================================================
// DTOs
// ==========================================================

// Request de cadastro
public record CadastrarJogoRequest(
    string  Titulo,
    string  Descricao,
    decimal Preco);

// Request de atualização de preço
public record AtualizarPrecoRequest(decimal Preco);

// Response padrão do jogo
public record JogoResponse(
    Guid     Id,
    string   Titulo,
    string   Descricao,
    decimal  Preco,
    decimal  PrecoAtual,
    bool     TemPromocaoAtiva,
    DateTime CriadoEm,
    string?  Mensagem = null)
// Construtor que recebe a entidade e mapeia para o DTO
{
    public JogoResponse(Jogo j, string? mensagem = null)
        : this(
            j.Id,
            j.Titulo,
            j.Descricao,
            j.Preco,
            j.GetCurrentPrice(),        // preço com desconto se houver promoção
            j.Promocoes.Any(p => p.EstaAtiva()),
            j.CriadoEm.ToLocalTime(),  // UTC → horário local
            mensagem) { }
}

// Request de criação de promoção
// Datas no formato: "2026-05-01T10:00:00" (horário de Brasília)
public record CriarPromocaoRequest(
    decimal  PercentualDesconto,
    DateTime Inicio,
    DateTime Fim);

// Response de promoção ativa
public record PromocaoAtivaResponse(
    Guid     JogoId,
    string   Titulo,
    decimal  PrecoOriginal,
    decimal  PrecoComDesconto,
    decimal  PercentualDesconto,
    DateTime PromocaoInicio,
    DateTime PromocaoFim)
{
    public PromocaoAtivaResponse(Jogo j, Promocao p)
        : this(
            j.Id,
            j.Titulo,
            j.Preco,
            j.GetCurrentPrice(),
            p.PercentualDesconto,
            p.Inicio.ToLocalTime(),
            p.Fim.ToLocalTime()) { }
}