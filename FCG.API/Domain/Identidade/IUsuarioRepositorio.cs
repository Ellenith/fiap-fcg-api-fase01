// ============================================================
// IUsuarioRepositorio.cs — Interface do repositório de usuários
//
// Define o contrato que o repositório deve cumprir.
// Fica no Domain pois o domínio conhece a interface mas
// não conhece a implementação (que usa EF Core).
//
// Isso permite:
// - Trocar a implementação sem alterar o domínio
// - Usar um repositório falso (mock) nos testes unitários
// ============================================================

namespace FCG.API.Domain.Identidade;

public interface IUsuarioRepositorio
{
    // Busca um usuário pelo Id
    Task<Usuario?> BuscarPorIdAsync(Guid id);

    // Busca um usuário pelo e-mail — usado no login
    Task<Usuario?> BuscarPorEmailAsync(string email);

    // Retorna todos os usuários — usado pelo Admin
    Task<IEnumerable<Usuario>> ListarTodosAsync();

    // Verifica se já existe um usuário com o e-mail informado
    // Usado antes de cadastrar para evitar duplicatas
    Task<bool> EmailExisteAsync(string email);

    // Verifica se existe algum usuário no banco
    // Usado para definir o role do primeiro usuário cadastrado
    Task<bool> ExisteAlgumUsuarioAsync();

    // Adiciona um novo usuário
    Task AdicionarAsync(Usuario usuario);
    

    // Atualiza um usuário existente
    void Atualizar(Usuario usuario);

    // Salva todas as alterações pendentes no banco
    Task<int> SalvarAsync();
}