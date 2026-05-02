// ============================================================
// TesteUsuario.cs — Testes unitários da entidade Usuario
//
// Segue o padrão TDD: cada teste valida uma regra de negócio
// específica da entidade Usuario.
//
// Nomenclatura dos testes:
// Metodo_Cenario_ResultadoEsperado
//
// Exemplo:
// Criar_ComEmailInvalido_DeveLancarDomainException
// ============================================================

using FCG.API.Domain.Identidade;
using FCG.API.Domain.SharedKernel;
using FluentAssertions;

namespace FCG.Tests.Identidade;

public class TesteUsuario
{
    // ----------------------------------------------------------
    // Constantes reutilizadas nos testes
    // Evita repetição e facilita manutenção
    // ----------------------------------------------------------
    private const string NomeValido      = "Lucas Silva";
    private const string EmailValido     = "lucas@fiap.com.br";
    private const string SenhaValida     = "Senha@123";
    private const string SenhaHashValida = "hash_simulado_123";

    // ==========================================================
    // TESTES DE CRIAÇÃO — Usuario.Create()
    // ==========================================================

    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarUsuarioCorreto()
    {
        // Act
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Assert
        usuario.Id.Should().NotBeEmpty("deve gerar um Guid automaticamente");
        usuario.Name.Should().Be(NomeValido);
        usuario.Email.Should().Be(EmailValido.ToLowerInvariant());
        usuario.PasswordHash.Should().Be(SenhaHashValida);
        usuario.Role.Should().Be("User", "role padrão deve ser User");
        usuario.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Criar_ComoAdmin_DeveRetornarUsuarioComRoleAdmin()
    {
        // Act
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida,
            "Admin");

        // Assert
        usuario.Role.Should().Be("Admin");
    }

    // ----------------------------------------------------------
    // Testes de validação do Nome
    // ----------------------------------------------------------

    [Fact]
    public void Criar_ComNomeVazio_DeveLancarDomainException()
    {
        // Act
        var acao = () => Usuario.Create(
            "",
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Nome é obrigatório.");
    }

    [Fact]
    public void Criar_ComNomeMuitoCurto_DeveLancarDomainException()
    {
        // Act — nome com apenas 1 caractere
        var acao = () => Usuario.Create(
            "A",
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Nome deve ter pelo menos 2 caracteres.");
    }

    [Fact]
    public void Criar_ComNomeMuitoLongo_DeveLancarDomainException()
    {
        // Arrange — gera uma string com 101 caracteres
        var nomeLongo = new string('A', 101);

        // Act
        var acao = () => Usuario.Create(
            nomeLongo,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Nome deve ter no máximo 100 caracteres.");
    }

    // ----------------------------------------------------------
    // Testes de validação do Email
    // ----------------------------------------------------------

    [Fact]
    public void Criar_ComEmailVazio_DeveLancarDomainException()
    {
        // Act
        var acao = () => Usuario.Create(
            NomeValido,
            "",
            SenhaValida,
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("E-mail é obrigatório.");
    }

    [Theory]
    [InlineData("emailsemarroba")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    [InlineData("email@dominio")]
    [InlineData("email sem espaço@dominio.com")]
    public void Criar_ComEmailInvalido_DeveLancarDomainException(string emailInvalido)
    {
        // Act
        var acao = () => Usuario.Create(
            NomeValido,
            emailInvalido,
            SenhaValida,
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Formato de e-mail inválido.");
    }

    [Fact]
    public void Criar_ComEmailEmMaiusculo_DeveConverterParaMinusculo()
    {
        // Arrange
        var emailMaiusculo = "Lucas@FIAP.COM.BR";

        // Act
        var usuario = Usuario.Create(
            NomeValido,
            emailMaiusculo,
            SenhaValida,
            SenhaHashValida);

        // Assert — e-mail deve ser normalizado para minúsculo
        usuario.Email.Should().Be("lucas@fiap.com.br");
    }

    // ----------------------------------------------------------
    // Testes de validação da Senha
    // ----------------------------------------------------------

    [Fact]
    public void Criar_ComSenhaVazia_DeveLancarDomainException()
    {
        // Act
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha é obrigatória.");
    }

    [Fact]
    public void Criar_ComMenosDeOitoCaracteres_DeveLancarDomainException()
    {
        // Act — senha com menos de 8 caracteres
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "S@1a",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve ter no mínimo 8 caracteres.");
    }

    [Fact]
    public void Criar_SemLetraMaiuscula_DeveLancarDomainException()
    {
        // Act — senha sem letra maiúscula
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "senha@123",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve conter pelo menos uma letra maiúscula.");
    }

    [Fact]
    public void Criar_SemLetraMinuscula_DeveLancarDomainException()
    {
        // Act — senha sem letra minúscula
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "SENHA@123",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve conter pelo menos uma letra minúscula.");
    }

    [Fact]
    public void Criar_SemNumero_DeveLancarDomainException()
    {
        // Act — senha sem número
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "Senha@abc",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve conter pelo menos um número.");
    }

    [Fact]
    public void Criar_SemCaractereEspecial_DeveLancarDomainException()
    {
        // Act — senha sem caractere especial
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            "Senha1234",
            SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve conter pelo menos um caractere especial.");
    }

    // ----------------------------------------------------------
    // Testes de validação da Role
    // ----------------------------------------------------------

    [Theory]
    [InlineData("Superadmin")]
    [InlineData("Moderador")]
    [InlineData("")]
    [InlineData("user")]    // minúsculo — case sensitive
    [InlineData("admin")]   // minúsculo — case sensitive
    public void Criar_ComRoleInvalida_DeveLancarDomainException(string roleInvalida)
    {
        // Act
        var acao = () => Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida,
            roleInvalida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Role inválida. Use 'User' ou 'Admin'.");
    }

    // ==========================================================
    // TESTES DE ATUALIZAÇÃO — Usuario.UpdateProfile()
    // ==========================================================

    [Fact]
    public void AtualizarPerfil_ComDadosValidos_DeveAtualizarNomeEHash()
    {
        // Arrange
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        var novoNome = "Lucas Pereira";
        var novoHash = "novo_hash_456";

        // Act
        usuario.UpdateProfile(novoNome, SenhaValida, novoHash);

        // Assert
        usuario.Name.Should().Be(novoNome);
        usuario.PasswordHash.Should().Be(novoHash);

        // Email e Role não devem ser alterados pelo UpdateProfile
        usuario.Email.Should().Be(EmailValido.ToLowerInvariant());
        usuario.Role.Should().Be("User");
    }

    [Fact]
    public void AtualizarPerfil_ComNomeVazio_DeveLancarDomainException()
    {
        // Arrange
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Act
        var acao = () => usuario.UpdateProfile("", SenhaValida, SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Nome é obrigatório.");
    }

    [Fact]
    public void AtualizarPerfil_ComSenhaFraca_DeveLancarDomainException()
    {
        // Arrange
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Act — senha sem maiúscula
        var acao = () => usuario.UpdateProfile(NomeValido, "senha@123", SenhaHashValida);

        // Assert
        acao.Should().Throw<DomainException>()
            .WithMessage("Senha deve conter pelo menos uma letra maiúscula.");
    }

    // ==========================================================
    // TESTES DE MUDANÇA DE ROLE — Usuario.ChangeRole()
    // ==========================================================

    [Fact]
    public void AlterarRole_ParaAdmin_DeveAtualizarRole()
    {
        // Arrange
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Act
        usuario.ChangeRole("Admin");

        // Assert
        usuario.Role.Should().Be("Admin");
    }

    [Fact]
    public void AlterarRole_ComRoleInvalida_DeveLancarDomainException()
    {
        // Arrange
        var usuario = Usuario.Create(
            NomeValido,
            EmailValido,
            SenhaValida,
            SenhaHashValida);

        // Act
        var acao = () => usuario.ChangeRole("Superadmin");

        // Assert
        acao.Should().Throw<DomainException>();
    }
}