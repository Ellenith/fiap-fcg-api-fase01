using FCG.API.Domain.Identidade;
using FCG.API.Domain.Catalogo;
using FCG.API.Domain.Biblioteca;
using Microsoft.EntityFrameworkCore;

namespace FCG.API.Infraestrutura.Persistencia;

public class FGCDbContext : DbContext
{
    public FGCDbContext(DbContextOptions<FGCDbContext> options)
        : base(options)
    {
    }

    // Tabela: Usuarios
    public DbSet<Usuario> Usuarios { get; set; }

    // Tabela: Jogos
    public DbSet<Jogo> Jogos { get; set; }

    // Tabela: Biblioteca
    public DbSet<Biblioteca> Biblioteca { get; set; }

    // Tabela: Promocoes
    public DbSet<Promocao> Promocoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Configuração da entidade Usuario ──
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });

        // ── Configuração da entidade Jogo ──
        modelBuilder.Entity<Jogo>(entity =>
        {
            entity.ToTable("Jogos");

            entity.HasKey(j => j.Id);

            entity.Property(j => j.Titulo)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(j => j.Descricao)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(j => j.Preco)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(j => j.CriadoEm)
                .IsRequired();
        });

        // ── Configuração da entidade Biblioteca ──
        modelBuilder.Entity<Biblioteca>(entity =>
        {
            entity.ToTable("Biblioteca");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.UsuarioId)
                .IsRequired();

            entity.Property(b => b.JogoId)
                .IsRequired();

            entity.HasIndex(b => new { b.UsuarioId, b.JogoId })
                .IsUnique();

            entity.Property(b => b.PrecoPago)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(b => b.AdquiridoEm)
                .IsRequired();

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Jogo)
                .WithMany()
                .HasForeignKey(b => b.JogoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // ── Configuração da entidade Promocao ──
        modelBuilder.Entity<Promocao>(entity =>
        {
            entity.ToTable("Promocoes");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.JogoId)
                .IsRequired();

            entity.Property(p => p.PercentualDesconto)
                .IsRequired()
                .HasPrecision(5, 2);

            entity.Property(p => p.Inicio)
                .IsRequired();

            entity.Property(p => p.Fim)
                .IsRequired();

            // Relacionamento: Jogo tem muitas Promocoes
            entity.HasOne<Jogo>()
                .WithMany(j => j.Promocoes)
                .HasForeignKey(p => p.JogoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}