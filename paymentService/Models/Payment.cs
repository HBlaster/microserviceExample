using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Models;

public class Payment
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}