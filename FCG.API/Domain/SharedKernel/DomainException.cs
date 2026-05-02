// ============================================================
// DomainException.cs — Exceção de domínio
//
// Exceção customizada para representar violações de regras
// de negócio. Usada em todas as entidades do domínio.
//
// Por que uma exceção própria e não ArgumentException?
// - Permite distinguir erros de negócio de erros técnicos
// - O middleware de erros vai capturar DomainException e
//   retornar HTTP 400 (Bad Request) automaticamente
// - Erros técnicos inesperados retornam HTTP 500
// ============================================================

namespace FCG.API.Domain.SharedKernel;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}