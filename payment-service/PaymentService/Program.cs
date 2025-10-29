using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/payments", async (
    AppDbContext context,
HttpContext http,
    Payment newPayment) => // O .NET magicamente converte o JSON para a classe
{
    // TODO: Na Fase 3, vamos ler o 'X-User-ID' do header
    // Por enquanto, vamos "fingir" o ID
    newPayment.OwnerUserId = http.Request.Headers["X-User-ID"].ToString();
    newPayment.Status = "CREATED";
    newPayment.TxId = $"tx_{Guid.NewGuid().ToString().Substring(0, 8)}";

    await context.Payments.AddAsync(newPayment);
    await context.SaveChangesAsync();

    return Results.Created($"/api/payments/{newPayment.TxId}", newPayment);
});

// Rota de Health Check
app.MapGet("/api/payments/health", () => Results.Ok(new { Status = "OK" }));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
