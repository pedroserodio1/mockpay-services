using PaymentService.Domains;
using PaymentService.Enum;

namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; } // Chave primária
    public string TxId { get; set; } = null!;
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public decimal Amount { get; set; } = 0;
    public string Method { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; } = null;
    public DateTime? ExpiresAt { get; set; } = null;
    public List<PaymentStory> PaymentHistory { get; set; } = new();

    public string OwnerUserId { get; set; } = null!; // ID do usuário dono deste pagamento
}