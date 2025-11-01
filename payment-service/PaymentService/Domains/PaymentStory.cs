namespace PaymentService.Domains;

public record PaymentStory
{
    public string Status { get; init; } = null!;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Description { get; init; } = null!;
    public string? Reason { get; init; } = null!;
}