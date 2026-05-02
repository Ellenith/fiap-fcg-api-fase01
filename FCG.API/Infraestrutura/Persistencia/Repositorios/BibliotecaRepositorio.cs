// ============================================================
// BibliotecaRepositorio.cs — Implementação do repositório da biblioteca
// ============================================================

using FCG.API.Domain.Biblioteca;
using Microsoft.EntityFrameworkCore;

namespace FCG.API.Infraestrutura.Persistencia.Repositorios;

public class BibliotecaRepositorio : IBibliotecaRepositorio
{
    private readonly FGCDbContext _context;

    public BibliotecaRepositorio(FGCDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Biblioteca>> ListarPorUsuarioAsync(Guid usuarioId)
    {
        // Filtra pelo UsuarioId — cada usuário só vê
        // os jogos que ele mesmo adquiriu
        // Include do Jogo para trazer o Título
        return await _context.Biblioteca
            .AsNoTracking()
            .Include(b => b.Jogo)
            .Where(b => b.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<bool> JogoJaAdquiridoAsync(Guid usuarioId, Guid jogoId)
    {
        // Verifica duplicata — regra mais importante
        // do contexto Biblioteca
        return await _context.Biblioteca
            .AnyAsync(b => b.UsuarioId == usuarioId && b.JogoId == jogoId);
    }

    public async Task AdicionarAsync(Biblioteca biblioteca)
    {
        await _context.Biblioteca.AddAsync(biblioteca);
    }

    public async Task<int> SalvarAsync()
    {
        return await _context.SaveChangesAsync();
    }
}