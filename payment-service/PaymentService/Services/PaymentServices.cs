using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Domains;
using PaymentService.DTO;
using PaymentService.Enum;
using PaymentService.Models;

namespace PaymentService.Services;

public class PaymentServices(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

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
                    ? DateTime.UtcNow.AddMinutes(30)
                    : DateTime.UtcNow.AddDays(3);
                payment.PaymentHistory.Add(new Domains.PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {txId} do usuario {ownerUserId} foi criado.",
                    Timestamp = DateTime.UtcNow
                });
                break;
            case "credit_card":
                if (paymentDTO.Card == null)
                {
                    throw new KeyNotFoundException("Dados do cartão são obrigatórios.");
                }

                payment.PaymentHistory.Add(new Domains.PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {txId} do usuario {ownerUserId} foi criado.",
                    Timestamp = DateTime.UtcNow
                });

                string simulationStatus = SimulateCardAuthorization(paymentDTO.Card);

                string statusCard = simulationStatus;

                payment.Status = (statusCard == "APPROVED") ? PaymentStatus.PAID : PaymentStatus.FAILED;
                payment.PaidAt = DateTime.UtcNow;
                payment.PaymentHistory.Add(new Domains.PaymentStory
                {
                    Status = payment.Status.ToString(),
                    Description = $"O pagamento {txId} do usuario {ownerUserId} foi {payment.Status.ToString()}.",
                    Timestamp = DateTime.UtcNow,
                    Reason = statusCard
                });
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

        return true;
    }

    private string SimulateCardAuthorization(CardDetailsDTO card)
{
    // Lógica de simulação "de brincadeira":
    // Se o cartão terminar em 1111, aprova.
    // Se terminar em 4444, recusa por saldo.
    if (card.Number.EndsWith("1111")) return "APPROVED";
    if (card.Number.EndsWith("4444")) return "DECLINED_INSUFFICIENT_FUNDS";
    
    // Padrão
    return "DECLINED";
}
}