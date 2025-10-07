using System.Text;
using System.Text.Json;
using Digizuite.Common.Constants;
using Digizuite.Common.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.Storage;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    private static string _filePath = string.Empty;
    
    static async Task Main()
    {
        await SetupConnectionsAsync();
        await DeclareAsync();
        
        if (_channel is null)
            return;
        
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                
                var file = JsonSerializer.Deserialize<TransferFile>(message);
                
                if (file?.Data is not null && file.MimeType == "image/jpeg" && !string.IsNullOrEmpty(_filePath))
                    await File.WriteAllBytesAsync(_filePath + $"/{file.FileName}", file.Data);

                Console.WriteLine("[Storage] Stored correlationId: {0}", ea.BasicProperties.CorrelationId);
            }
            catch (Exception)
            {
                Console.WriteLine("[Storage] Failed to store file");
            }
        };
        
        Console.WriteLine("[Storage] Ready to store or setup");
        await _channel.BasicConsumeAsync(Queues.StorageQueue, true, consumer);

        while (true)
        {
            var input = Console.ReadLine();
            
            if (input == "exit")
                break;

            if (input == "file")
            {
                Console.WriteLine("[Storage] Enter file path...");
                
                var typedPath = Console.ReadLine();

                if (Directory.Exists(typedPath))
                {
                    Console.WriteLine("[Storage] Valid directory, saving");
                    _filePath = typedPath;
                }
                else
                    Console.WriteLine("[Storage] Invalid path");
            }
        }
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

    private static async Task DeclareAsync()
    {
        if (_channel is null) return;

        await _channel.ExchangeDeclareAsync(Exchanges.StorageExchange, ExchangeType.Direct, false, true);

        await _channel.QueueDeclareAsync(Queues.StorageQueue, false, false, true);
        await _channel.QueueBindAsync(Queues.StorageQueue, Exchanges.StorageExchange, "store.file");
    }
}