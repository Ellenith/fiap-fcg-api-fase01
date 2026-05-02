// ============================================================
// JogoRepositorio.cs — Implementação do repositório de jogos
// ============================================================

using FCG.API.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace FCG.API.Infraestrutura.Persistencia.Repositorios;

public class JogoRepositorio : IJogoRepositorio
{
    private readonly FGCDbContext _context;

    public JogoRepositorio(FGCDbContext context)
    {
        _context = context;
    }

    public async Task<Jogo?> BuscarPorIdAsync(Guid id)
    {
        return await _context.Jogos.FindAsync(id);
    }

    public async Task<IEnumerable<Jogo>> ListarTodosAsync()
    {
        return await _context.Jogos
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> TituloExisteAsync(string titulo)
    {
        // Comparação case-insensitive para evitar
        // jogos com títulos duplicados por diferença de caixa
        return await _context.Jogos
            .AnyAsync(j => j.Titulo.ToLower() == titulo.ToLower());
    }

    public async Task AdicionarAsync(Jogo jogo)
    {
        await _context.Jogos.AddAsync(jogo);
    }

    public void Atualizar(Jogo jogo)
    {
        _context.Jogos.Update(jogo);
    }

    public async Task<int> SalvarAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Remover(Jogo jogo)
    {
    // Remove marca o objeto para exclusão no contexto
    // A exclusão ocorre quando SalvarAsync() é chamado
    _context.Jogos.Remove(jogo);
    }

    public async Task<Jogo?> BuscarPorIdComPromocoesAsync(Guid id)
    {
        // Include carrega as promoções junto com o jogo
        // necessário para GetCurrentPrice() funcionar
        return await _context.Jogos
            .Include(j => j.Promocoes)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IEnumerable<Jogo>> ListarTodosComPromocoesAsync()
    {
        return await _context.Jogos
            .Include(j => j.Promocoes)
            .AsNoTracking()
            .ToListAsync();
    }
}