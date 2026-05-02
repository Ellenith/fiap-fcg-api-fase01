// ============================================================
// PasswordHasherServico.cs — Serviço de hash de senha
//
// Encapsula o IPasswordHasher do ASP.NET Identity.
// Responsável por:
// - Gerar o hash da senha no cadastro
// - Verificar a senha no login comparando com o hash
//
// Nunca armazenamos a senha em texto puro — apenas o hash.
// O hash é gerado com PBKDF2 + salt aleatório, tornando
// impossível reverter o hash para a senha original.
// ============================================================

using FCG.API.Domain.Identidade;
using Microsoft.AspNetCore.Identity;

namespace FCG.API.Infraestrutura.Servicos;

public class PasswordHasherServico
{
    // IPasswordHasher é fornecido pelo ASP.NET Identity
    // Usa PBKDF2 com salt aleatório por padrão
    private readonly IPasswordHasher<Usuario> _hasher;

    public PasswordHasherServico(IPasswordHasher<Usuario> hasher)
    {
        _hasher = hasher;
    }

    // ----------------------------------------------------------
    // Gera o hash da senha
    // Chamado no cadastro antes de salvar o usuário
    //
    // O hash gerado inclui o salt aleatório embutido,
    // então dois hashes da mesma senha são sempre diferentes
    // ----------------------------------------------------------
    public string GerarHash(string senha)
    {
        // O primeiro parâmetro seria o usuário dono da senha,
        // mas como o hash inclui o salt, passamos null — o
        // IPasswordHasher do Identity não usa o usuário no hash
        return _hasher.HashPassword(null!, senha);
    }

    // ----------------------------------------------------------
    // Verifica se a senha informada corresponde ao hash salvo
    // Chamado no login para validar as credenciais
    //
    // Retorna true se a senha for válida, false caso contrário
    // ----------------------------------------------------------
    public bool VerificarSenha(string senhaInformada, string hashSalvo)
    {
        var resultado = _hasher.VerifyHashedPassword(
            null!,
            hashSalvo,
            senhaInformada);

        // PasswordVerificationResult pode ser:
        // Success         → senha correta
        // SuccessRehashNeeded → senha correta mas hash desatualizado
        // Failed          → senha incorreta
        return resultado != PasswordVerificationResult.Failed;
    }
}