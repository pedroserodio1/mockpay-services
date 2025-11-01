using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PaymentService.Data;
using PaymentService.Extensions;
using PaymentService.Models;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

var rabbitUser = builder.Configuration["RABBITMQ_USER"];
var rabbitPass = builder.Configuration["RABBITMQ_PASS"];

// 1. Adicione um log de console
Console.WriteLine($"[DEBUG] Config: RABBITMQ_USER lido como: {rabbitUser}");
Console.WriteLine($"[DEBUG] Config: RABBITMQ_PASS lido como: {rabbitPass}");    


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
builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MockPay - PaymentService", Version = "v1" });

    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Autenticação via API Key (JWT ou mockpk_...).\n\n" +
                      "**Não** precisa de escrever 'Bearer'.\n" +
                      "**Insira apenas a sua chave/token abaixo.**",
        In = ParameterLocation.Header, 
        Type = SecuritySchemeType.Http, 
        Scheme = "bearer", 
        BearerFormat = "JWT/ApiKey"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" 
                }
            },
            new string[] {} 
        }
    });
});

builder.Services.AddServices();


builder.Configuration.AddInMemoryCollection(new[]
{
    new KeyValuePair<string, string>("RabbitMQ:HostName", "rabbit-mq"),
    new KeyValuePair<string, string>("RabbitMQ:UserName", builder.Configuration["RABBITMQ_USER"]),
    new KeyValuePair<string, string>("RabbitMQ:Password", builder.Configuration["RABBITMQ_PASS"]) 
});

builder.Services.AddSingleton<RabbitMQPublisher>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<RabbitMQPublisher>());

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

