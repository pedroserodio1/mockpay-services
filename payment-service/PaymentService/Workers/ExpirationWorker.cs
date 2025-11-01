
using Microsoft.EntityFrameworkCore;
using PaymentService.Data; 
using PaymentService.Domains;
using PaymentService.Enum; 


public class ExpirationWorker : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirationWorker> _logger;


    public ExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<ExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--- ExpirationWorker iniciado. ---");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            await CheckAndExpirePaymentsAsync();
        }
    }

    private async Task CheckAndExpirePaymentsAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expiredPayments = await dbContext.Payments
                .Where(p => p.Status == PaymentStatus.PENDING
                            && p.ExpiresAt.HasValue
                            && p.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            if (expiredPayments.Count == 0)
            {
                _logger.LogInformation("Nenhum pagamento expirou neste ciclo.");
                return;
            }

            _logger.LogWarning("{Count} pagamentos a expirar...", expiredPayments.Count);

            foreach (var payment in expiredPayments)
            {
                payment.Status = PaymentStatus.EXPIRED;
                payment.PaymentHistory.Add(new PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {payment.TxId} foi expirado",
                    Timestamp = DateTime.UtcNow
                });

            }
            await dbContext.SaveChangesAsync();
        }
    }
}