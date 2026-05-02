// ============================================================
// IJogoRepositorio.cs — Interface do repositório de jogos
// ============================================================

namespace FCG.API.Domain.Catalogo;

public interface IJogoRepositorio
{
    // Busca um jogo pelo Id
    Task<Jogo?> BuscarPorIdAsync(Guid id);

    // Retorna todos os jogos — usado pelo Admin
    Task<IEnumerable<Jogo>> ListarTodosAsync();

    // Verifica se já existe um jogo com o título informado
    Task<bool> TituloExisteAsync(string titulo);

    // Adiciona um novo jogo
    Task AdicionarAsync(Jogo jogo);

    // Atualiza um jogo existente
    void Atualizar(Jogo jogo);

    // Salva todas as alterações pendentes no banco
    Task<int> SalvarAsync();

    // Remove um jogo do banco
    void Remover(Jogo jogo);

    // Busca jogo com promoções carregadas — necessário para GetCurrentPrice()
    Task<Jogo?> BuscarPorIdComPromocoesAsync(Guid id);

    // Lista todos os jogos com promoções carregadas
    Task<IEnumerable<Jogo>> ListarTodosComPromocoesAsync();
}