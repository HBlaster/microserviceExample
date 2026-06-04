using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingService.Data;

namespace BillingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly BillingDbContext _db;

    public InvoicesController(BillingDbContext db)
    {
        _db = db;
    }

    // GET api/invoices
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _db.Invoices.ToListAsync();
        return Ok(invoices);
    }

    // GET api/invoices/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();
        return Ok(invoice);
    }

    // GET api/invoices/by-payment/{paymentId}
    [HttpGet("by-payment/{paymentId}")]
    public async Task<IActionResult> GetByPaymentId(int paymentId)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.PaymentId == paymentId);
        if (invoice is null) return NotFound($"No se encontró factura para PaymentId={paymentId}");
        return Ok(invoice);
    }
}