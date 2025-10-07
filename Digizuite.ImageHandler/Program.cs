using System.Text;
using System.Text.Json;
using Digizuite.Common.Constants;
using Digizuite.Common.Helpers;
using Digizuite.Common.Models;
using Digizuite.ImageHandler.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.ImageHandler;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    static async Task Main()
    {
        await SetupConnectionsAsync();
        await DeclareConnectionsAsync();
        
        if (_channel is null)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var correlationId = ea.BasicProperties.CorrelationId;
                
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                
                var file = JsonSerializer.Deserialize<TransferFile>(message);

                if (file is not null && file.Data is not null)
                {
                    file.Data = await WatermarkHelper.WatermarkImage(file.Data);
                    var encoded = EncodingHelper.EncodeMessage(file);

                    var properties = new BasicProperties()
                    {
                        CorrelationId = correlationId,
                        ContentType = file.MimeType,
                    };
                    
                    await _channel.BasicPublishAsync(Exchanges.StorageExchange, $"store.file", true, properties, encoded);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to watermark message: " + ex.Message);
            }
        };

        await _channel.BasicConsumeAsync(Queues.JpegFileQueue, true, consumer);
        
        Console.ReadLine();
    }
    
    private static async Task SetupConnectionsAsync()
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

    private static async Task DeclareConnectionsAsync()
    {
        if (_channel is null) return;

        await _channel.ExchangeDeclareAsync(Exchanges.JpegExchange, ExchangeType.Direct, durable: false);

        await _channel.QueueDeclareAsync(Queues.JpegFileQueue, durable: false);
        await _channel.QueueBindAsync(Queues.JpegFileQueue, Exchanges.JpegExchange, "new.jpeg");
    }
}