// ============================================================
// FGCDbContextFactory.cs
//
// Factory usada pelo EF Core em tempo de design (migrations).
// Foi necessária pq o DbContext não conseguiu ser instanciado
// automaticamente pelo host da aplicação.
// ============================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCG.API.Infraestrutura.Persistencia;

public class FGCDbContextFactory : IDesignTimeDbContextFactory<FGCDbContext>
{
    public FGCDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FGCDbContext>()
            .UseSqlite("Data Source=fcg.db")
            .Options;

        return new FGCDbContext(options);
    }
}