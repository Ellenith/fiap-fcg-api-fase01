# FIAP Cloud Games (FCG) — API REST

API REST desenvolvida em .NET 9 para a plataforma FIAP Cloud Games.
Permite o gerenciamento de usuários, catálogo de jogos, promoções e
biblioteca de jogos adquiridos.

Projeto desenvolvido como Tech Challenge Fase 1 da pós-graduação
PosTech FIAP — Arquitetura de Sistemas .NET.

---

## Tecnologias utilizadas

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9 + SQLite
- JWT (JSON Web Token) para autenticação
- Serilog para logs estruturados em arquivo
- xUnit + FluentAssertions para testes unitários
- Swagger / OpenAPI para documentação interativa

---

## Arquitetura

O projeto segue os princípios de **Domain-Driven Design (DDD)**
organizado em um **monolito modular**:

```
FCG/
├── FCG.API/
│   ├── Domain/
│   │   ├── SharedKernel/      (UsuarioId, DomainException)
│   │   ├── Identidade/        (Usuario, IUsuarioRepositorio)
│   │   ├── Catalogo/          (Jogo, Promocao, IJogoRepositorio)
│   │   └── Biblioteca/        (Biblioteca, IBibliotecaRepositorio)
│   ├── Infraestrutura/
│   │   ├── Persistencia/      (FGCDbContext, Repositórios)
│   │   └── Servicos/          (JwtServico, PasswordHasherServico)
│   ├── Controllers/           (UsuarioController, AuthController, JogoController, BibliotecaController)
│   ├── Middlewares/           (ErrorHandlingMiddleware, LoggingMiddleware)
│   └── Logs/                  (Arquivos de log gerados pelo Serilog)
└── FCG.Tests/
    ├── Identidade/            (TesteUsuario.cs)
    ├── Catalogo/              (TesteJogo.cs, TestePromocao.cs)
    └── Biblioteca/            (TesteBiblioteca.cs)
```

---

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet)

Instalar o dotnet-ef globalmente:
```bash
dotnet tool install --global dotnet-ef
```

---

## Como rodar o projeto

**1. Clone o repositório**
```bash
git clone https://github.com/Ellenith/fiap-fcg-api-fase01
cd FCG
```

**2. Restaure os pacotes**
```bash
dotnet restore
```

**3. Aplique as migrations e crie o banco**
```bash
dotnet ef database update --project FCG.API
```

O arquivo `fcg.db` será criado automaticamente na pasta `FGC.API/`.

**4. Rode a API**
```bash
dotnet run --project FGC.API
```

**5. Acesse o Swagger**

https://localhost:{porta}/swagger

---

## Como rodar os testes

```bash
dotnet test
```

Resultado esperado:

Test summary: total: 59; failed: 0; succeeded: 59

---

## Variáveis de configuração

Todas as configurações ficam em `FCG.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=fcg.db"
  },
  "Jwt": {
    "Secret": "fcg-super-secret-key-min-32-chars!!",
    "Issuer": "FCG.API",
    "Audience": "FCG.Client",
    "ExpirationMinutes": 60
  }
}
```

> ⚠️ Em produção, substitua o `Secret` por uma chave forte
> e nunca versione o `appsettings.json` com segredos reais.

---

## Endpoints

### Autenticação
| Método | Endpoint | Descrição | Acesso |
|--------|----------|-----------|--------|
| POST | `/auth/login` | Autenticar e obter JWT | Público |

### Usuários
| Método | Endpoint | Descrição | Acesso |
|--------|----------|-----------|--------|
| POST | `/usuarios` | Cadastrar usuário | Público |
| GET | `/usuarios` | Listar todos os usuários | Admin |
| GET | `/usuarios/{id}` | Buscar usuário por Id | Autenticado |
| PUT | `/usuarios/{id}` | Atualizar nome e senha | Autenticado |

### Jogos
| Método | Endpoint | Descrição | Acesso |
|--------|----------|-----------|--------|
| POST | `/jogos` | Cadastrar jogo | Admin |
| GET | `/jogos` | Listar todos os jogos | Público |
| GET | `/jogos/{id}` | Buscar jogo por Id | Público |
| PUT | `/jogos/{id}` | Atualizar preço | Admin |
| DELETE | `/jogos/{id}` | Remover jogo | Admin |
| GET | `/jogos/promocoes` | Listar promoções ativas | Público |
| POST | `/jogos/{id}/promocoes` | Criar promoção | Admin |

### Biblioteca
| Método | Endpoint | Descrição | Acesso |
|--------|----------|-----------|--------|
| POST | `/biblioteca` | Adquirir jogo | Autenticado |
| GET | `/biblioteca` | Listar jogos adquiridos | Autenticado |

---

## Como usar a API

**1. Cadastre o primeiro usuário — ele vira Admin automaticamente**
```json
POST /usuarios
{
  "nome": "Lucas Silva",
  "email": "lucas@fiap.com.br",
  "senha": "Senha@123"
}
```

**2. Faça login e copie o token**
```json
POST /auth/login
{
  "email": "lucas@fiap.com.br",
  "senha": "Senha@123"
}
```

**3. Use o token no header de todas as requisições protegidas**

Authorization: Bearer {seu_token}

No Swagger, clique em **Authorize 🔒** e cole o token no formato acima.

**4. Cadastre um jogo**
```json
POST /jogos
{
  "titulo": "Minecraft",
  "descricao": "Jogo de construção e sobrevivência.",
  "preco": 99.90
}
```

**5. Crie uma promoção**
```json
POST /jogos/{id}/promocoes
{
  "percentualDesconto": 20,
  "inicio": "2026-01-01T00:00:00",
  "fim": "2099-12-31T23:59:59"
}
```

> ⚠️ As datas devem ser informadas no horário de Brasília (UTC-3).

**6. Adquira um jogo como usuário comum**
```json
POST /biblioteca
{
  "jogoId": "{id_do_jogo}"
}
```

---

## Regras de negócio

**Usuários**
- O primeiro usuário cadastrado recebe a role `Admin` automaticamente
- E-mail deve ser único e ter formato válido
- Senha deve ter no mínimo 8 caracteres com letra maiúscula,
  letra minúscula, número e caractere especial
- Usuário só pode editar o próprio perfil
- Admin pode editar qualquer usuário

**Jogos**
- Somente Admin pode cadastrar, atualizar e remover jogos
- Título deve ser único no catálogo
- Preço não pode ser negativo (jogos gratuitos são permitidos)

**Promoções**
- Somente Admin pode criar promoções
- Desconto deve ser entre 1% e 100%
- Data de fim deve ser posterior à data de início
- Datas informadas em horário de Brasília (UTC-3)
- Datas armazenadas em UTC para consistência
- O preço com desconto é calculado em tempo real — sem job agendado

**Biblioteca**
- Um usuário não pode adquirir o mesmo jogo duas vezes
- O preço pago é registrado no momento da compra e nunca é alterado
- Usuário só visualiza a própria biblioteca

---

## Datas e fuso horário

As datas são armazenadas internamente em **UTC** para garantir
consistência. Ao exibir para o cliente, as datas são convertidas
automaticamente para o **horário de Brasília (UTC-3)**.

Ao informar datas na criação de promoções, use o horário de Brasília:

"inicio": "2026-05-01T10:00:00"  → salvo como 2026-05-01T13:00:00Z
"fim":    "2026-05-31T23:59:59"  → salvo como 2026-06-01T02:59:59Z

---

## Logs

Os logs são gerados automaticamente na pasta `FCG.API/Logs/`
com rotação diária. Cada arquivo segue o padrão:

fcg-log-YYYYMMDD.txt

Exemplo de entrada no log:

2026-04-29 10:00:00 [INF] Request iniciado: POST /usuarios
2026-04-29 10:00:00 [INF] Request concluído: POST /usuarios → 201 em 45ms
2026-04-29 10:00:01 [ERR] Erro não tratado: E-mail já cadastrado.

---

## Testes

O projeto segue **TDD (Test-Driven Development)** com testes unitários
cobrindo as principais regras de negócio do domínio:

| Arquivo | Testes | Cobertura |
|---------|--------|-----------|
| TesteUsuario.cs | 21 | Criação, validações, atualização, roles |
| TesteJogo.cs | 11 | Criação, validações, atualização de preço |
| TestePromocao.cs | 15 | Criação, validações, período, GetCurrentPrice |
| TesteBiblioteca.cs | 7 | Registro, validações, imutabilidade |
| **Total** | **59** | |

---

## Decisões arquiteturais

**SQLite** foi escolhido por simplicidade no ambiente de desenvolvimento.
A troca para SQL Server ou PostgreSQL requer apenas alterar o provider
no `Program.cs` e a connection string no `appsettings.json`.

**Gateway de pagamento** não foi implementado nesta fase — a aquisição
de jogos é simulada diretamente. Será integrado em fases futuras.

**Primeiro usuário como Admin** — decisão de MVP para simplificar
o bootstrap da plataforma sem necessidade de seed manual.

**Datas em UTC** — todas as datas são armazenadas em UTC no banco
e convertidas para horário local (UTC-3) apenas na exibição,
garantindo consistência independente do fuso horário do servidor.

**Promoções sem job agendado** — a expiração é calculada em tempo
real via `GetCurrentPrice()` a cada consulta, eliminando a necessidade
de processos em background no MVP.

**Senha validada no domínio** — as regras de senha ficam encapsuladas
em `Usuario.ValidarSenha()`, garantindo que nenhum código externo
consiga criar um usuário com senha inválida.

---

## Documentação DDD

O Event Storming e os diagramas de agregados estão disponíveis em:

https://miro.com/app/board/uXjVGm-r5uI=/?share_link_id=739444277432

---

*FIAP Cloud Games · Tech Challenge Fase 1 · PosTech FIAP*
