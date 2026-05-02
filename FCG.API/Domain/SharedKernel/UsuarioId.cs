// ============================================================
// UsuarioId.cs — Shared Kernel
//
// Value Object que representa o identificador único de um
// usuário. É o único elemento compartilhado entre os três
// contextos: Identidade, Catálogo e Biblioteca.
//
// Por que um Value Object e não um Guid simples?
// - Evita confundir IDs de entidades diferentes (GameId, UserId)
// - Torna o código mais expressivo e seguro em tempo de compilação
// - Centraliza qualquer futura lógica de geração de ID
// ============================================================

namespace FCG.API.Domain.SharedKernel;

public sealed class UsuarioId
{
    // Valor interno — somente leitura após criação
    public Guid Value { get; }

    // Construtor privado — força o uso do método Create()
    // garantindo que nunca seja criado um UsuarioId inválido
    private UsuarioId(Guid value)
    {
        Value = value;
    }

    // Cria um novo UsuarioId com um Guid gerado automaticamente
    // Usado no momento do cadastro do usuário
    public static UsuarioId New() => new(Guid.NewGuid());

    // Reconstrói um UsuarioId a partir de um Guid existente
    // Usado ao carregar o usuário do banco de dados
    public static UsuarioId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UsuarioId não pode ser vazio.");

        return new UsuarioId(value);
    }

    // Permite comparar dois UsuarioId pelo valor interno
    // Sem isso, a comparação seria por referência de objeto
    public override bool Equals(object? obj)
        => obj is UsuarioId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    // Permite exibir o ID como string legível em logs e respostas
    public override string ToString() => Value.ToString();

    // Conversão implícita para Guid
    // Permite usar UsuarioId diretamente onde um Guid é esperado
    // Exemplo: entity.Id = usuarioId  (sem precisar de .Value)
    public static implicit operator Guid(UsuarioId id) => id.Value;
}