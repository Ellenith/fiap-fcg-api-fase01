// ============================================================
// UsuarioRepositorio.cs — Implementação do repositório de usuários
//
// Implementa IUsuarioRepositorio usando EF Core + SQLite.
// Toda lógica de acesso ao banco fica aqui, isolada do domínio.
//
// O DbContext é injetado via construtor — o ASP.NET garante
// que a mesma instância é usada durante todo o request HTTP
// ============================================================

using FCG.API.Domain.Identidade;
using FCG.API.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FCG.API.Infraestrutura.Persistencia.Repositorios;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    // DbContext injetado via DI
    private readonly FGCDbContext _context;

    public UsuarioRepositorio(FGCDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> BuscarPorIdAsync(Guid id)
    {
        // FindAsync é otimizado para busca por chave primária
        // Retorna null se não encontrar
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task<Usuario?> BuscarPorEmailAsync(string email)
    {
        // Normaliza para minúsculo antes de comparar
        // pois o e-mail é salvo em minúsculo no cadastro
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<IEnumerable<Usuario>> ListarTodosAsync()
    {
        // AsNoTracking melhora performance em consultas
        // somente leitura — EF Core não precisa rastrear
        // as entidades retornadas para detectar mudanças
        return await _context.Usuarios
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> EmailExisteAsync(string email)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task AdicionarAsync(Usuario usuario)
    {
        // Adiciona o usuário ao contexto — ainda não salva no banco
        // A gravação acontece quando SalvarAsync() é chamado
        await _context.Usuarios.AddAsync(usuario);
    }

    public async Task<bool> ExisteAlgumUsuarioAsync()
    {
    // AnyAsync é mais eficiente que carregar todos os usuários
    // pois para na primeira ocorrência encontrada
    return await _context.Usuarios.AnyAsync();
    }

    public void Atualizar(Usuario usuario)
    {
        // Update marca o objeto como modificado no contexto
        // O EF Core vai gerar o UPDATE no banco ao salvar
        _context.Usuarios.Update(usuario);
    }

    public async Task<int> SalvarAsync()
    {
        // Persiste todas as alterações pendentes no banco
        // Retorna o número de registros afetados
        return await _context.SaveChangesAsync();
    }


}