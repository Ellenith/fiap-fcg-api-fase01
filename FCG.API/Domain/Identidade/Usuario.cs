// ============================================================
// Usuario.cs — Agregado raiz do contexto Identidade
//
// Encapsula todas as regras de negócio relacionadas ao usuário:
// - Validação de e-mail
// - Validação de força da senha
// - Controle de role (perfil de acesso)
//
// Princípio DDD aplicado aqui: a entidade protege seus próprios
// invariantes. Nenhuma regra de negócio do usuário deve existir
// fora desta classe.
// ============================================================

using FCG.API.Domain.SharedKernel;
using System.Text.RegularExpressions;

namespace FCG.API.Domain.Identidade;

public class Usuario
{
    // ----------------------------------------------------------
    // Propriedades
    // Setters privados garantem que o estado do usuário só pode
    // ser alterado pelos métodos da própria classe
    // ----------------------------------------------------------

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    // Nunca armazenamos a senha em texto puro.
    // Apenas o hash gerado pelo PasswordHasher é salvo.
    public string PasswordHash { get; private set; } = string.Empty;

    // Role define o nível de acesso:
    // "User"  → acesso à plataforma e biblioteca
    // "Admin" → acesso total, incluindo gestão de jogos
    public string Role { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    // ----------------------------------------------------------
    // Construtor privado
    // Força o uso do método estático Create() como único
    // ponto de entrada para criar um usuário válido.
    // ----------------------------------------------------------
    private Usuario() { }

    // ----------------------------------------------------------
    // Método de fábrica (Factory Method)
    //
    // Parâmetros:
    //   name          → nome de exibição do usuário
    //   email         → e-mail (usado como login)
    //   senha         → senha em texto puro (validada aqui)
    //   passwordHash  → hash da senha (gerado pelo PasswordHasher)
    //   role          → "User" ou "Admin" (padrão: "User")
    // ----------------------------------------------------------
    public static Usuario Create(
        string name,
        string email,
        string senha,
        string passwordHash,
        string role = "User")
    {
        // Valida todos os campos antes de criar o objeto
        ValidarNome(name);
        ValidarEmail(email);
        ValidarSenha(senha);
        ValidarRole(role);

        return new Usuario
        {
            Id           = Guid.NewGuid(),
            Name         = name.Trim(),
            Email        = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role         = role,
            CreatedAt    = DateTime.UtcNow
        };
    }

    // ----------------------------------------------------------
    // Atualiza nome e senha do usuário
    // ----------------------------------------------------------
    public void UpdateProfile(string name, string senha, string passwordHash)
    {
        ValidarNome(name);
        ValidarSenha(senha);

        Name         = name.Trim();
        PasswordHash = passwordHash;
    }

    // ----------------------------------------------------------
    // Promove ou rebaixa a role do usuário
    // Só o Admin pode chamar este método (controlado no handler)
    // ----------------------------------------------------------
    public void ChangeRole(string newRole)
    {
        ValidarRole(newRole);
        Role = newRole;
    }

    // ----------------------------------------------------------
    // Validações privadas
    // ----------------------------------------------------------

    private static void ValidarNome(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome é obrigatório.");

        if (name.Trim().Length < 2)
            throw new DomainException("Nome deve ter pelo menos 2 caracteres.");

        if (name.Trim().Length > 100)
            throw new DomainException("Nome deve ter no máximo 100 caracteres.");
    }

    private static void ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("E-mail é obrigatório.");

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);

        if (!emailRegex.IsMatch(email))
            throw new DomainException("Formato de e-mail inválido.");
    }

    public static void ValidarSenha(string senha)
    {
        // Público para permitir validação antes de gerar o hash
        // no controller, sem precisar duplicar as regras

        if (string.IsNullOrWhiteSpace(senha))
            throw new DomainException("Senha é obrigatória.");

        if (senha.Length < 8)
            throw new DomainException("Senha deve ter no mínimo 8 caracteres.");

        if (!senha.Any(char.IsUpper))
            throw new DomainException("Senha deve conter pelo menos uma letra maiúscula.");

        if (!senha.Any(char.IsLower))
            throw new DomainException("Senha deve conter pelo menos uma letra minúscula.");

        if (!senha.Any(char.IsDigit))
            throw new DomainException("Senha deve conter pelo menos um número.");

        if (!senha.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new DomainException("Senha deve conter pelo menos um caractere especial.");
    }

    private static void ValidarRole(string role)
    {
        var validRoles = new[] { "User", "Admin" };

        if (!validRoles.Contains(role))
            throw new DomainException("Role inválida. Use 'User' ou 'Admin'.");
    }
}