// ============================================================
// Jogo.cs — Agregado raiz do contexto Catálogo
//
// Representa um jogo disponível na plataforma FCG.
// Ao ser cadastrado, já fica disponível na loja imediatamente.
//
// Regras de negócio encapsuladas aqui:
// - Título obrigatório entre 2 e 200 caracteres
// - Descrição obrigatória com no máximo 2000 caracteres
// - Preço não pode ser negativo
//
// Decisão de MVP: sem estado de publicação e sem promoções.
// Essas funcionalidades serão adicionadas em fases futuras.
// ============================================================

using FCG.API.Domain.SharedKernel;

namespace FCG.API.Domain.Catalogo;

public class Jogo
{
    // ----------------------------------------------------------
    // Propriedades
    // Setters privados garantem que o estado do jogo só pode
    // ser alterado pelos métodos da própria classe,
    // protegendo as regras de negócio de alterações externas
    // ----------------------------------------------------------

    public Guid Id { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Descricao { get; private set; } = string.Empty;

    // Preço original do jogo.
    // Só pode ser alterado via AtualizarPreco() que valida
    // se o novo valor é maior ou igual a zero
    public decimal Preco { get; private set; }

    // Data de criação em UTC — fuso horário universal para
    // evitar problemas com servidores em fusos diferentes
    public DateTime CriadoEm { get; private set; }

    // ----------------------------------------------------------
    // Construtor privado
    // Força o uso do método estático Criar() como único
    // ponto de entrada para instanciar um Jogo válido.
    // O EF Core consegue instanciar mesmo com construtor
    // privado pois usa reflexão internamente.
    // ----------------------------------------------------------
    private Jogo() { }

    // ----------------------------------------------------------
    // Método de fábrica (Factory Method)
    //
    // Centraliza a criação do jogo e garante que todas as
    // regras de negócio são validadas antes de criar o objeto.
    // Se qualquer validação falhar, uma DomainException é
    // lançada antes do objeto ser criado — nunca existirá
    // um Jogo em estado inválido na memória.
    //
    // Parâmetros:
    //   titulo    → nome do jogo exibido na loja
    //   descricao → descrição detalhada do jogo
    //   preco     → preço base em reais (≥ 0)
    // ----------------------------------------------------------
    public static Jogo Criar(string titulo, string descricao, decimal preco)
    {
        // Valida todos os campos antes de criar o objeto
        ValidarTitulo(titulo);
        ValidarDescricao(descricao);
        ValidarPreco(preco);

        // Cria e retorna o objeto somente após todas as
        // validações passarem com sucesso
        return new Jogo
        {
            Id        = Guid.NewGuid(),
            Titulo    = titulo.Trim(),      // remove espaços extras
            Descricao = descricao.Trim(),   // remove espaços extras
            Preco     = preco,
            CriadoEm = DateTime.UtcNow     // sempre em UTC
        };
    }

    // ----------------------------------------------------------
    // Atualiza o preço do jogo
    //
    // Valida o novo preço antes de aplicar a alteração.
    // Não afeta compras anteriores — o preço pago é gravado
    // em UserLibrary no momento da compra e nunca é alterado.
    // ----------------------------------------------------------
    public void AtualizarPreco(decimal novoPreco)
    {
        ValidarPreco(novoPreco);
        Preco = novoPreco;
    }

    // ----------------------------------------------------------
    // Validações privadas
    //
    // Cada regra de negócio fica em seu próprio método para:
    // - Facilitar os testes unitários (cada regra testada isolada)
    // - Reutilizar a mesma validação em métodos diferentes
    // - Deixar o código mais legível e fácil de manter
    //
    // Lançam DomainException que será capturada pelo middleware
    // de erros e convertida em HTTP 400 (Bad Request)
    // ----------------------------------------------------------

    private static void ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("Título é obrigatório.");

        if (titulo.Trim().Length < 2)
            throw new DomainException("Título deve ter pelo menos 2 caracteres.");

        if (titulo.Trim().Length > 200)
            throw new DomainException("Título deve ter no máximo 200 caracteres.");
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição é obrigatória.");

        if (descricao.Trim().Length > 2000)
            throw new DomainException("Descrição deve ter no máximo 2000 caracteres.");
    }

    private static void ValidarPreco(decimal preco)
    {
        // Jogos gratuitos (preco = 0) são permitidos
        // Apenas valores negativos são rejeitados
        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");
    }

    // Lista de promoções do jogo
    // Carregada pelo EF Core via Include() nas queries
    public List<Promocao> Promocoes { get; private set; } = new();

    // Calcula o preço atual considerando promoções ativas
    // Elimina a necessidade de job agendado —
    // a promoção expira naturalmente quando Fim < UtcNow
    public decimal GetCurrentPrice()
    {
        var promocaoAtiva = Promocoes.FirstOrDefault(p => p.EstaAtiva());

        if (promocaoAtiva is null)
            return Preco;

        // Exemplo: Preco=100, Desconto=20 → 100 * (1 - 0.20) = 80
        return Preco * (1 - promocaoAtiva.PercentualDesconto / 100);
    }
}