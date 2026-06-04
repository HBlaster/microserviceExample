using System.ComponentModel.DataAnnotations.Schema;

namespace BillingService.Models;

public class Invoice
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}