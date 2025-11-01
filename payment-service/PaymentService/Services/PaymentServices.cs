using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Domains;
using PaymentService.DTO;
using PaymentService.Enum;
using PaymentService.Models;

namespace PaymentService.Services;

public class PaymentServices(AppDbContext dbContext, RabbitMQPublisher rabbitMQ)
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly RabbitMQPublisher _rabbitPublisher = rabbitMQ;

    public async Task<Payment?> GetPaymentByIdAsync(string id, string userId)
    {
        return await _dbContext.Payments.FirstOrDefaultAsync(p =>
            p.TxId == id && p.OwnerUserId == userId
        );

    }

    public async Task<Payment> CreatePaymentAsync(PaymentCreateDTO paymentDTO, string ownerUserId)
    {

        string hash = Guid.NewGuid().ToString("N").Substring(0, 16);

        var txId = $"tx_{hash}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TxId = txId,
            Amount = paymentDTO.Amount,
            Method = paymentDTO.Method,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OwnerUserId = ownerUserId
        };

        switch (payment.Method.ToLower())
        {
            case "pix":
            case "boleto":
                payment.Status = PaymentStatus.PENDING;
                payment.ExpiresAt = (paymentDTO.Method == "pix")
                    ? DateTime.UtcNow.AddMinutes(2)
                    : DateTime.UtcNow.AddDays(3);
                payment.PaymentHistory.Add(new Domains.PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {txId} do usuario {ownerUserId} foi criado.",
                    Timestamp = DateTime.UtcNow
                });

                TimeSpan delay = payment.ExpiresAt.Value - DateTime.UtcNow;

                if (delay.TotalMilliseconds > 0)
                {
                    await _rabbitPublisher.PublishExpirationCheck(payment.TxId, delay);
                }

                break;
            case "credit_card":
                if (paymentDTO.Card == null)
                {
                    // LOG 1: Erro de validação
                    Console.WriteLine($"[LOG-CRITICO] Erro de validação: Dados do cartão ausentes para TxId {payment.TxId}.");
                    throw new KeyNotFoundException("Dados do cartão são obrigatórios.");
                }

                // LOG 2: Confirma que o processamento de cartão foi iniciado
                Console.WriteLine($"[LOG-INFO] Processamento de Cartão de Crédito iniciado. TxId: {payment.TxId}, User: {ownerUserId}.");

                payment.PaymentHistory.Add(new Domains.PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {txId} do usuario {ownerUserId} foi criado.",
                    Timestamp = DateTime.UtcNow
                });

                var random = new Random();
                int randomDelaySeconds = random.Next(5, 30);
                TimeSpan delayCredit = TimeSpan.FromSeconds(randomDelaySeconds);

                long delayMs = Math.Max(0, (long)delayCredit.TotalMilliseconds);

                // LOG 3: Mostrar o delay calculado
                Console.WriteLine($"[LOG-DELAY] Calculado delay de {randomDelaySeconds} segundos para aprovação de cartão.");

                // LOG 4: A chamada para o publisher
                await _rabbitPublisher.PublishDelayedApproval(payment.TxId, delayMs);
                Console.WriteLine($"[LOG-RABBIT] Chamada BEM-SUCEDIDA ao PublishDelayedApproval para TxId: {payment.TxId}.");

                break;
            default:
                throw new KeyNotFoundException("Metodo de pagamento não registrado");
        }

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        return payment;
    }

    public async Task<bool> SimulatePayment(string id)
    {
        Payment? payment = await _dbContext.Payments.SingleOrDefaultAsync(u => u.TxId == id);

        if (payment == null)
        {
            return false;
        }

        if (payment.Status != PaymentStatus.PENDING)
        {
            return false;
        }


        if (payment.ExpiresAt.HasValue && payment.ExpiresAt.Value <= DateTime.UtcNow)
        {

            payment.Status = PaymentStatus.EXPIRED;
            payment.PaymentHistory.Add(new Domains.PaymentStory
            {
                Status = payment.Status.ToString(),
                Description = $"O pagamento {id} foi expirado",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
            return false;
        }


        payment.Status = PaymentStatus.PAID;
        payment.PaidAt = DateTime.UtcNow;
        payment.PaymentHistory.Add(new Domains.PaymentStory
        {
            Status = payment.Status.ToString(),
            Description = $"O pagamento {id} foi pago.",
            Timestamp = DateTime.UtcNow
        });


        await _dbContext.SaveChangesAsync();

        await _rabbitPublisher.PublishPaymentApproved(payment);

        return true;
    }

    public async Task UpdateStatusInternalAsync(UpdatePaymentStatusRequest request)
    {

        var payment = await _dbContext.Payments
            .SingleOrDefaultAsync(p => p.TxId == request.TxId);

        if (payment == null)
        {
            throw new KeyNotFoundException($"TxId '{request.TxId}' não encontrado para atualização de status.");
        }

        PaymentStatus currentStatus = payment.Status;

        switch (request.Action)
        {
            case PaymentStatus.EXPIRED:
                if (currentStatus == PaymentStatus.PENDING)
                {
                    payment.Status = PaymentStatus.EXPIRED;
                }
                else
                {
                    return;
                }
                break;

            case PaymentStatus.PAID:
                if (currentStatus == PaymentStatus.PENDING || currentStatus == PaymentStatus.PENDING)
                {
                    payment.Status = PaymentStatus.PAID;
                    payment.PaidAt = DateTime.UtcNow;
                    await _rabbitPublisher.PublishPaymentApproved(payment);
                }
                else
                {
                    throw new InvalidOperationException($"Não é possível aprovar um pagamento em status: {currentStatus}");
                }
                break;

            default:
                throw new ArgumentException($"Ação de status '{request.Action}' não permitida.");
        }

        payment.PaymentHistory.Add(new Domains.PaymentStory
        {
            Status = payment.Status.ToString(),
            Description = $"O pagamento {request.TxId} foi pago.",
            Timestamp = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }
}