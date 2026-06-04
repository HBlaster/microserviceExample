namespace BillingService.Events;

public class PaymentCreatedEvent
{
    public int PaymentId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
