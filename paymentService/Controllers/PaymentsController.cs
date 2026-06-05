
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Events;
using PaymentService.Messaging;
using PaymentService.Models;
using Microsoft.AspNetCore.SignalR;
using PaymentService.Hubs;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentsDbContext _db;
    private readonly RabbitMqPublisher _publisher;
    private readonly IHubContext<PaymentHub> _hubContext;

    public PaymentsController(PaymentsDbContext db, RabbitMqPublisher publisher, IHubContext<PaymentHub> hubContext)
    {
        _db = db;
        _publisher = publisher;
        _hubContext = hubContext;
    }

    // GET api/payments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _db.Payments.ToListAsync();
        return Ok(payments);
    }

    // GET api/payments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    // POST api/payments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var payment = new Payment
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        await _publisher.PublishPaymentCreatedAsync(new PaymentCreatedEvent
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CreatedAt = payment.CreatedAt
        });
        await _hubContext.Clients.All.SendAsync("PaymentCreated", payment);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }
}

public record CreatePaymentRequest(string OrderId, decimal Amount, string Currency);