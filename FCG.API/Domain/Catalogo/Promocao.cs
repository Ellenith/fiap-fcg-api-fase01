// ============================================================
// Promocao.cs — Entidade do contexto Catálogo
//
// Representa um desconto temporário aplicado a um jogo.
// Versão MVP — simples e funcional.
//
// Regras de negócio:
// - Desconto entre 1% e 100%
// - Data de fim posterior à data de início
// ============================================================

using FCG.API.Domain.SharedKernel;

namespace FCG.API.Domain.Catalogo;

public class Promocao
{
    public Guid Id { get; private set; }

    // Referência ao jogo dono desta promoção
    public Guid JogoId { get; private set; }

    // Percentual de desconto: 1 a 100
    public decimal PercentualDesconto { get; private set; }

    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }

    private Promocao() { }

    public static Promocao Criar(
        Guid jogoId,
        decimal percentualDesconto,
        DateTime inicio,
        DateTime fim)
    {
        if (percentualDesconto <= 0 || percentualDesconto > 100)
            throw new DomainException("Desconto deve ser entre 1% e 100%.");

        if (fim <= inicio)
            throw new DomainException("A data de fim deve ser posterior à data de início.");

        return new Promocao
        {
            Id                  = Guid.NewGuid(),
            JogoId              = jogoId,
            PercentualDesconto  = percentualDesconto,
            Inicio              = inicio,
            Fim                 = fim
        };
    }

    // Verifica se a promoção está ativa no momento atual
    public bool EstaAtiva()
        => DateTime.UtcNow >= Inicio && DateTime.UtcNow <= Fim;
}