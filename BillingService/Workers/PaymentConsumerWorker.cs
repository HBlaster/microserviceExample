using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BillingService.Data;
using BillingService.Events;
using BillingService.Models;

namespace BillingService.Workers;

public class PaymentConsumerWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentConsumerWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "payment.exchange";
    private const string QueueName = "billing.queue";

    public PaymentConsumerWorker(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentConsumerWorker> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectToRabbitMQAsync();

        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("[BillingService] Evento recibido: {Message}", message);

            try
            {
                var paymentEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(message);

                if (paymentEvent is not null)
                {
                    await ProcessPaymentAsync(paymentEvent);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillingService] Error procesando evento");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );

        // Mantener el worker vivo hasta que se cancele
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectToRabbitMQAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            UserName = _config["RabbitMQ:Username"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest"
        };

        var retries = 0;
        while (retries < 10)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Fanout,
                    durable: true
                );

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                await _channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: string.Empty
                );

                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false
                );

                _logger.LogInformation("[BillingService] Conectado a RabbitMQ, escuchando en '{Queue}'", QueueName);
                break;
            }
            catch (Exception ex)
            {
                retries++;
                _logger.LogWarning("[BillingService] RabbitMQ no disponible, reintento {Retry}/10: {Error}", retries, ex.Message);
                await Task.Delay(3000);
            }
        }
    }

    private async Task ProcessPaymentAsync(PaymentCreatedEvent paymentEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var invoice = new Invoice
        {
            PaymentId = paymentEvent.PaymentId,
            OrderId = paymentEvent.OrderId,
            Amount = paymentEvent.Amount,
            Currency = paymentEvent.Currency,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{paymentEvent.PaymentId:D5}",
            IssuedAt = DateTime.UtcNow
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "[BillingService] Factura generada: {InvoiceNumber} para PaymentId={PaymentId}",
            invoice.InvoiceNumber, invoice.PaymentId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}