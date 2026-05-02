// ============================================================
// Program.cs — FIAP Cloud Games API
// Ponto de entrada da aplicação. Toda configuração de serviços
// e pipeline HTTP fica aqui no modelo minimal hosting do .NET 9
// ============================================================

using FCG.API.Domain.Biblioteca;
using FCG.API.Domain.Catalogo;
using FCG.API.Domain.Identidade;
using FCG.API.Infraestrutura.Persistencia;
using FCG.API.Infraestrutura.Persistencia.Repositorios;
using FCG.API.Infraestrutura.Servicos;
using FCG.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;


// ----------------------------------------------------------
// Serilog — configuração do log em arquivo
//
// Deve ser a primeira coisa configurada na aplicação para
// capturar erros que ocorram durante o próprio startup.
//
// Arquivo de log:
// - Criado na pasta Logs/ dentro do projeto
// - Rotacionado diariamente (um arquivo por dia)
// - Máximo de 7 dias de histórico
// - Nome do arquivo: fcg-log-YYYYMMDD.txt
// ----------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()          // mantém o log no terminal também
    .WriteTo.File(
        path: "Logs/fcg-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// ----------------------------------------------------------
// BUILDER
// WebApplication.CreateBuilder configura o host da aplicação:
// - lê o appsettings.json automaticamente
// - configura o ILogger padrão
// - registra os serviços do ASP.NET Core
// ----------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);


// Garante que logs pendentes são gravados se a aplicação
// encerrar inesperadamente
builder.Host.UseSerilog();

// ==========================================================
// REGISTRO DE SERVIÇOS (Injeção de Dependência)
// Tudo que for adicionado aqui fica disponível via DI em
// qualquer controller, middleware ou serviço da aplicação
// ==========================================================

// ----------------------------------------------------------
// Controllers MVC
// Habilita o padrão Controller → Action para os endpoints.
// Sem isso, o MapControllers() no final não faz nada.
// ----------------------------------------------------------
builder.Services.AddControllers();

// ----------------------------------------------------------
// Swagger / OpenAPI
// Gera a documentação interativa da API acessível em /swagger
// AddEndpointsApiExplorer → descobre os endpoints da API
// AddSwaggerGen           → gera o JSON de especificação
// ----------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Metadados exibidos no topo da página do Swagger
    options.SwaggerDoc("v1", new()
    {
        Title       = "FIAP Cloud Games API",
        Version     = "v1",
        Description = "API REST para gerenciamento de usuários e biblioteca de jogos da FCG"
    });

    // ── Configuração do botão "Authorize" no Swagger ──
    // Sem isso, não é possível testar endpoints protegidos
    // por JWT diretamente pela UI do Swagger

    // Define o esquema de segurança chamado "Bearer"
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Cole aqui o token no formato: Bearer {seu_token_jwt}"
    });

    // Aplica o esquema "Bearer" globalmente a todos os endpoints
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ----------------------------------------------------------
// Autenticação JWT (JSON Web Token)
//
// Fluxo:
// 1. Cliente faz POST /auth/login com e-mail e senha
// 2. API valida credenciais e retorna um JWT assinado
// 3. Cliente envia o JWT no header: Authorization: Bearer {token}
// 4. ASP.NET valida o token antes de cada request protegido
// ----------------------------------------------------------

// Lê o segredo do appsettings.json
// O "!" diz ao compilador que esse valor nunca é null —
// se for null, vai explodir em runtime (configuração obrigatória)
var jwtSecret = builder.Configuration["Jwt:Secret"]!;

// Define JWT como o esquema de autenticação padrão da aplicação
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Parâmetros que o ASP.NET usa para VALIDAR cada token recebido
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida se o campo "iss" do token bate com o Issuer configurado
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],

            // Valida se o campo "aud" do token bate com o Audience configurado
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],

            // Valida se o token não expirou (campo "exp")
            ValidateLifetime         = true,

            // Valida a assinatura digital do token com a chave secreta
            // Garante que o token não foi forjado ou alterado
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// ----------------------------------------------------------
// Autorização por Roles (perfis de acesso)
//
// Nos controllers, o uso fica assim:
//   [Authorize]                    → qualquer usuário logado
//   [Authorize(Roles = "Admin")]   → somente administradores
// ----------------------------------------------------------
builder.Services.AddAuthorization();

// ----------------------------------------------------------
// Entity Framework Core — DbContext
//
// Registra o FGCDbContext com SQLite.
// Lifetime Scoped: uma instância por request HTTP.
// A connection string vem do appsettings.json.
// ----------------------------------------------------------
builder.Services.AddDbContext<FGCDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ----------------------------------------------------------
// Repositórios
//
// Padrão Repository: isola o acesso ao banco do domínio.
// Scoped: uma instância por request HTTP — mesma instância
// do DbContext, garantindo consistência dentro do request.
//
// Formato: Interface → Implementação
// Os controllers recebem a interface via DI, nunca a
// implementação direta — facilita testes e manutenção.
// ----------------------------------------------------------
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IJogoRepositorio, JogoRepositorio>();
builder.Services.AddScoped<IBibliotecaRepositorio, BibliotecaRepositorio>();

// ----------------------------------------------------------
// Serviços de infraestrutura
//
// JwtServico            → gera tokens JWT no login
// PasswordHasherServico → gera e verifica hashes de senha
// IPasswordHasher       → implementação do ASP.NET Identity
//                         injetada no PasswordHasherServico
// ----------------------------------------------------------
builder.Services.AddScoped<JwtServico>();
builder.Services.AddScoped<PasswordHasherServico>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// ==========================================================
// BUILD
// Finaliza o registro de serviços e cria o objeto app.
// Nenhum serviço pode ser registrado após esta linha.
// ==========================================================
var app = builder.Build();

// ==========================================================
// PIPELINE DE MIDDLEWARES
// Cada request HTTP passa por esta sequência na ordem
// em que estão registrados. A ORDEM IMPORTA.
// ==========================================================

if (app.Environment.IsDevelopment())
{
    // Serve o JSON de especificação em /swagger/v1/swagger.json
    app.UseSwagger();

    // Serve a UI interativa em /swagger
    app.UseSwaggerUI();
}

// ----------------------------------------------------------
// Logging
// Deve ser o primeiro middleware do pipeline para registrar
// todos os requests, inclusive os que falham nos próximos
// middlewares. Loga método, path, status e tempo de execução.
// ----------------------------------------------------------
app.UseMiddleware<LoggingMiddleware>();

// ----------------------------------------------------------
// Error Handling
// Deve vir logo após o logging para capturar erros de
// qualquer middleware subsequente.
// Converte exceções em respostas JSON padronizadas.
// ----------------------------------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();

// ----------------------------------------------------------
// HTTPS Redirection
// Redireciona HTTP para HTTPS automaticamente.
// ----------------------------------------------------------
app.UseHttpsRedirection();

// ----------------------------------------------------------
// Authentication
// Lê o token JWT do header Authorization e popula o
// HttpContext.User com as claims do token (id, email, role).
// DEVE vir ANTES de UseAuthorization().
// ----------------------------------------------------------
app.UseAuthentication();

// ----------------------------------------------------------
// Authorization
// Verifica se o usuário tem permissão para acessar o endpoint.
// DEVE vir DEPOIS de UseAuthentication().
// ----------------------------------------------------------
app.UseAuthorization();

// ----------------------------------------------------------
// Map Controllers
// Registra as rotas de todos os controllers da aplicação.
// ----------------------------------------------------------
app.MapControllers();

// ----------------------------------------------------------
// Run
// Inicia o servidor e bloqueia até a aplicação encerrar.
// ----------------------------------------------------------
app.Run();