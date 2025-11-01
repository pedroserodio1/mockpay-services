using System.ComponentModel.DataAnnotations;
using PaymentService.Enum;

namespace PaymentService.DTO;
public class UpdatePaymentStatusRequest
{
    [Required]
    public string TxId { get; set; } = string.Empty;

    [Required]
    // A ação que o Worker quer que a gente faça (EXPIRE ou APPROVE)
    public PaymentStatus Action { get; set; } 

    // Adicione esta propriedade se o Worker precisar de verificar a chave de segurança
    // public string SecretKey { get; set; } 
}