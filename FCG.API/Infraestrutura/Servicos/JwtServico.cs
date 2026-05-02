// ============================================================
// JwtServico.cs — Serviço de geração de tokens JWT
//
// Responsável por gerar o token JWT após o login.
// O token contém as claims do usuário (id, email, role)
// e é assinado com a chave secreta do appsettings.json.
//
// O token é enviado ao cliente que o usa em todas as
// requisições subsequentes no header Authorization.
// ============================================================

using FCG.API.Domain.Identidade;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FCG.API.Infraestrutura.Servicos;

public class JwtServico
{
    // Configurações lidas do appsettings.json
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtServico(IConfiguration configuration)
    {
        // Lê as configurações da seção Jwt do appsettings.json
        _secret            = configuration["Jwt:Secret"]!;
        _issuer            = configuration["Jwt:Issuer"]!;
        _audience          = configuration["Jwt:Audience"]!;
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"]!);
    }

    // ----------------------------------------------------------
    // Gera um token JWT para o usuário informado
    //
    // Claims incluídas no token:
    // - sub    → Id do usuário (padrão JWT)
    // - email  → E-mail do usuário
    // - role   → Role do usuário (User ou Admin)
    // - jti    → Id único do token (evita replay attacks)
    //
    // O token é assinado com HMAC-SHA256 usando a chave secreta.
    // Qualquer alteração no token invalida a assinatura.
    // ----------------------------------------------------------
    public string GerarToken(Usuario usuario)
    {
        // Define as claims que serão incluídas no token
        // Claims são informações sobre o usuário que ficam
        // dentro do token e podem ser lidas sem consultar o banco
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,               usuario.Role)
        };

        // Chave de assinatura gerada a partir do segredo
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Cria o token com todas as configurações
        var token = new JwtSecurityToken(
            issuer:            _issuer,
            audience:          _audience,
            claims:            claims,
            expires:           DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        // Serializa o token para string no formato
        // header.payload.signature (formato JWT padrão)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}