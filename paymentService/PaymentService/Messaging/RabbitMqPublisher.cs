using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using PaymentService.Events;

namespace PaymentService.Messaging;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "payment.exchange";

    private RabbitMqPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true
        );

        return new RabbitMqPublisher(connection, channel);
    }

    public async Task PublishPaymentCreatedAsync(PaymentCreatedEvent paymentEvent)
    {
        var message = JsonSerializer.Serialize(paymentEvent);
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            body: body
        );

        Console.WriteLine($"[PaymentService] Evento publicado: PaymentId={paymentEvent.PaymentId}");
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}