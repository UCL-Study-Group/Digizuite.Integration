using Digizuite.Common.Constants;
using Digizuite.Common.Helpers;
using Digizuite.Common.Models;
using RabbitMQ.Client;

namespace Digizuite.Client.Services;

public class RabbitService
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    public async Task SetupConnectionsAsync()
    {
        try
        {
            if (_connection is not null && _connection.IsOpen)
                return;

            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            _connection = await factory.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to establish connection. Perhaps RabbitMQ hasen't been started?");
        }
    }

    public async Task DeclareConnectionsAsync()
    {
        if (_channel is null)
            return;

        await _channel.ExchangeDeclareAsync(Exchanges.FileExchange, ExchangeType.Direct, durable: false);
    }

    public static async Task SendFileAsync(TransferFile file)
    {
        if (_channel is null)
            return;

        var id = Guid.NewGuid().ToString();

        var properties = new BasicProperties()
        {
            CorrelationId = id,
            ContentType = file.MimeType,
        };

        Console.WriteLine("[Client] Sending file with correlationId: {0}", id);

        await _channel.BasicPublishAsync(
            exchange: Exchanges.FileExchange,
            routingKey: "file.queue.new",
            body: EncodingHelper.EncodeMessage(file), 
            basicProperties: properties,
            mandatory: false);
    }
}