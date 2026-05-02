// ============================================================
// UserLibrary.cs — Agregado raiz do contexto Biblioteca
//
// Representa a biblioteca de jogos adquiridos por um usuário.
// No contexto Biblioteca, o usuário é chamado de "Comprador"
// e referenciado apenas pelo UsuarioId (Shared Kernel).
//
// Regras de negócio encapsuladas aqui:
// - Um usuário não pode adquirir o mesmo jogo duas vezes
// - A data de aquisição é registrada automaticamente em UTC
// - O preço pago é gravado no momento da compra e nunca
//   é alterado, mesmo que o preço do jogo mude depois
// ============================================================

using FCG.API.Domain.SharedKernel;
using FCG.API.Domain.Catalogo;

namespace FCG.API.Domain.Biblioteca;

public class Biblioteca
{
    // ----------------------------------------------------------
    // Propriedades
    // ----------------------------------------------------------

    public Guid Id { get; private set; }

    // Identificador do Comprador — referencia o User do
    // contexto Identidade via Shared Kernel (UsuarioId)
    public Guid UsuarioId { get; private set; }

    // Identificador do jogo adquirido — referencia o Jogo
    // do contexto Catálogo. Apenas o Id é armazenado aqui,
    // mantendo os contextos desacoplados
    public Guid JogoId { get; private set; }

    // Preço pago no momento da aquisição.
    // É uma fotografia do preço naquele instante —
    // imutável após o registro da compra
    public decimal PrecoPago { get; private set; }

    // Data e hora da aquisição em UTC
    public DateTime AdquiridoEm { get; private set; }

    // Propriedade de navegação para o jogo adquirido
    // Carregada via Include no repositório
    public Jogo? Jogo { get; set; }

    // ----------------------------------------------------------
    // Construtor privado
    // ----------------------------------------------------------
    private Biblioteca() { }

    // ----------------------------------------------------------
    // Método de fábrica
    //
    // Registra a aquisição de um jogo pelo usuário.
    //
    // Parâmetros:
    //   usuarioId  → id do comprador (Shared Kernel)
    //   jogoId     → id do jogo adquirido
    //   precoPago  → preço no momento da compra
    // ----------------------------------------------------------
    public static Biblioteca Registrar(Guid usuarioId, Guid jogoId, decimal precoPago)
    {
        ValidarUsuarioId(usuarioId);
        ValidarJogoId(jogoId);
        ValidarPrecoPago(precoPago);

        return new Biblioteca
        {
            Id         = Guid.NewGuid(),
            UsuarioId  = usuarioId,
            JogoId     = jogoId,
            PrecoPago  = precoPago,
            AdquiridoEm = DateTime.UtcNow
        };
    }

    // ----------------------------------------------------------
    // Validações privadas
    // ----------------------------------------------------------

    private static void ValidarUsuarioId(Guid usuarioId)
    {
        // Guid.Empty significa que nenhum id foi fornecido
        if (usuarioId == Guid.Empty)
            throw new DomainException("UsuarioId é obrigatório.");
    }

    private static void ValidarJogoId(Guid jogoId)
    {
        if (jogoId == Guid.Empty)
            throw new DomainException("JogoId é obrigatório.");
    }

    private static void ValidarPrecoPago(decimal precoPago)
    {
        // Jogos gratuitos (precoPago = 0) são permitidos
        if (precoPago < 0)
            throw new DomainException("Preço pago não pode ser negativo.");
    }
}