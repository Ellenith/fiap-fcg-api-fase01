// ============================================================
// BibliotecaTests.cs — Testes unitários da entidade Biblioteca
//
// Valida as regras de negócio da biblioteca de jogos:
// - Registro de aquisição com dados válidos
// - Rejeição de IDs inválidos
// - Rejeição de preço negativo
//
// A regra mais importante do contexto Biblioteca —
// impedir que o mesmo jogo seja adquirido duas vezes —
// será testada no handler de aquisição (Fase 5),
// pois depende de consulta ao banco de dados.
// ============================================================

using FCG.API.Domain.Biblioteca;
using FCG.API.Domain.SharedKernel;
using FluentAssertions;

namespace FCG.Tests.BibliotecaTests;

public class TesteBiblioteca
{
    // ----------------------------------------------------------
    // Constantes reutilizadas nos testes
    // ----------------------------------------------------------
    private static readonly Guid UsuarioIdValido = Guid.NewGuid();
    private static readonly Guid JogoIdValido    = Guid.NewGuid();
    private const decimal PrecoPagoValido        = 99.90m;

    // ==========================================================
    // TESTES DE REGISTRO — Biblioteca.Registrar()
    // ==========================================================

    [Fact]
    public void Registrar_ComDadosValidos_DeveRetornarBibliotecaCorreto()
    {
        // Act
        var aquisicao = Biblioteca.Registrar(
            UsuarioIdValido,
            JogoIdValido,
            PrecoPagoValido);

        // Assert
        aquisicao.Id.Should().NotBeEmpty("deve gerar um Guid automaticamente");
        aquisicao.UsuarioId.Should().Be(UsuarioIdValido);
        aquisicao.JogoId.Should().Be(JogoIdValido);
        aquisicao.PrecoPago.Should().Be(PrecoPagoValido);
        aquisicao.AdquiridoEm.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Registrar_ComPrecoZero_DevePermitirJogoGratuito()
    {
        // Jogos gratuitos são válidos — preço 0 é permitido
        // Act
        var aquisicao = Biblioteca.Registrar(
            UsuarioIdValido,
            JogoIdValido,
            0m);

        // Assert
        aquisicao.PrecoPago.Should().Be(0m);
    }

    // ----------------------------------------------------------
    // Testes de validação do UsuarioId
    // ----------------------------------------------------------

    [Fact]
    public void Registrar_ComUsuarioIdVazio_DeveLancarDomainException()
    {
        // Act
        var acao = () => Biblioteca.Registrar(
            Guid.Empty,     // UsuarioId inválido
            JogoIdValido,
            PrecoPagoValido);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("UsuarioId é obrigatório.");
    }

    // ----------------------------------------------------------
    // Testes de validação do JogoId
    // ----------------------------------------------------------

    [Fact]
    public void Registrar_ComJogoIdVazio_DeveLancarDomainException()
    {
        // Act
        var acao = () => Biblioteca.Registrar(
            UsuarioIdValido,
            Guid.Empty,     // JogoId inválido
            PrecoPagoValido);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("JogoId é obrigatório.");
    }

    // ----------------------------------------------------------
    // Testes de validação do PrecoPago
    // ----------------------------------------------------------

    [Fact]
    public void Registrar_ComPrecoNegativo_DeveLancarDomainException()
    {
        // Act
        var acao = () => Biblioteca.Registrar(
            UsuarioIdValido,
            JogoIdValido,
            -1m);           // preço negativo inválido

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Preço pago não pode ser negativo.");
    }

    // ----------------------------------------------------------
    // Testes de imutabilidade
    // ----------------------------------------------------------

    [Fact]
    public void Registrar_AdquiridoEm_DeveSerRegistradoEmUtc()
    {
        // O momento da compra deve sempre ser em UTC para
        // evitar problemas com fusos horários diferentes
        // Act
        var aquisicao = Biblioteca.Registrar(
            UsuarioIdValido,
            JogoIdValido,
            PrecoPagoValido);

        // Assert
        aquisicao.AdquiridoEm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Registrar_DuasAquisicoes_DevemTerIdsDiferentes()
    {
        // Cada aquisição deve gerar um Id único
        // Act
        var aquisicao1 = Biblioteca.Registrar(
            UsuarioIdValido,
            JogoIdValido,
            PrecoPagoValido);

        var aquisicao2 = Biblioteca.Registrar(
            UsuarioIdValido,
            Guid.NewGuid(), // jogo diferente
            PrecoPagoValido);

        // Assert
        aquisicao1.Id.Should().NotBe(aquisicao2.Id);
    }
}