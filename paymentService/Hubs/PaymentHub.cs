using Microsoft.AspNetCore.SignalR;

namespace PaymentService.Hubs;

public class PaymentHub : Hub
{
    // El servidor empuja eventos al cliente
    // No necesitamos métodos invocados desde el cliente por ahora
}