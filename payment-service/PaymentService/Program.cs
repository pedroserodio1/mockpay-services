using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PaymentService.Data;
using PaymentService.Extensions;
using PaymentService.Models;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);


/* This code snippet is configuring the JSON serialization options for the controllers in the
application. */
builder.Services.AddControllers()
.AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase, true // case-insensitive
            )
        ));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => // "c" são as opções
{
    // 1. (Opcional) Define o título do seu Swagger
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MockPay - PaymentService", Version = "v1" });

    // 2. DEFINE O "BOTÃO AUTHORIZE" (Security Definition)
    //    Estamos a dizer ao Swagger que usamos o padrão "Bearer".
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Autenticação via API Key (JWT ou mockpk_...).\n\n" +
                      "**Não** precisa de escrever 'Bearer'.\n" +
                      "**Insira apenas a sua chave/token abaixo.**",
        In = ParameterLocation.Header, // A chave vai no Header
        Type = SecuritySchemeType.Http, // O tipo é HTTP
        Scheme = "bearer", // O esquema é 'bearer' (isto faz o Swagger adicionar "Bearer " automaticamente)
        BearerFormat = "JWT/ApiKey"
    });

    // 3. APLICA A SEGURANÇA (Security Requirement)
    //    Isto diz ao Swagger para adicionar o ícone de "cadeado"
    //    e usar a definição "Bearer" em todas as chamadas.
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                // A referência ao esquema que definimos acima
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // <-- O ID tem de ser igual ao nome "Bearer"
                }
            },
            new string[] {} // Não precisamos de escopos (scopes)
        }
    });
});

builder.Services.AddServices();

builder.Services.AddHostedService<ExpirationWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
});




app.UseHttpsRedirection();

app.MapControllers();

// Rota de Health Check
app.MapGet("/api/payments/health", () => Results.Ok(new { Status = "OK", Service = "payment-service" }));

app.Run();

