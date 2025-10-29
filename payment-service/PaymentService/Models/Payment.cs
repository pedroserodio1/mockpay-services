namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; } // Chave primária
    public string TxId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal Amount { get; set; } = 0;
    public string Method { get; set; } = null!;
    public string OwnerUserId { get; set; } = null!; // ID do usuário dono deste pagamento
}