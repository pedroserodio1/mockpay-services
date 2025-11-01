namespace PaymentService.DTO
{
    public class PaymentCreateDTO
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = null!;

        public CardDetailsDTO? Card { get; set; }
    }
}