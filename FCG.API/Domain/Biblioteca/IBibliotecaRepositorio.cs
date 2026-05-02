// ============================================================
// IBibliotecaRepositorio.cs — Interface do repositório da biblioteca
// ============================================================

namespace FCG.API.Domain.Biblioteca;

public interface IBibliotecaRepositorio
{
    // Retorna todos os jogos adquiridos por um usuário
    Task<IEnumerable<Biblioteca>> ListarPorUsuarioAsync(Guid usuarioId);

    // Verifica se o usuário já adquiriu o jogo
    // É a verificação de duplicata — regra mais importante
    // do contexto Biblioteca
    Task<bool> JogoJaAdquiridoAsync(Guid usuarioId, Guid jogoId);

    // Registra a aquisição de um jogo
    Task AdicionarAsync(Biblioteca biblioteca);

    // Salva todas as alterações pendentes no banco
    Task<int> SalvarAsync();
}