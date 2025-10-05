using System.Text;
using System.Text.Json;
using Digizuite.Common.Constants;
using Digizuite.Common.Helpers;
using RabbitMQ.Client;

namespace Digizuite.Client;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    static async Task Main()
    {
        await SetupConnectionsAsync();
        await DeclareConnectionsAsync();

        while (true)
        {
            Console.WriteLine("Enter what type of file to send...");
            
            var input = Console.ReadLine();
            
            if (input == "exit")
                break;

            switch (input)
            {
                case "mp4":
                    await SendFileAsync(input);
                    break;
                case "png":
                    await SendFileAsync(input);
                    break;
                default:
                    Console.WriteLine("Unsupported file type!");
                    break;
            }
            
            Thread.Sleep(3000);
            Console.Clear();
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

    private static async Task DeclareConnectionsAsync()
    {
        if (_channel is null)
            return;

        await _channel.ExchangeDeclareAsync(Exchanges.FileExchange, ExchangeType.Direct, durable: false);
    }

    private static async Task SendFileAsync(string fileType)
    {
        if (_channel is null)
            return;
        
        var body = new
        {
            fileType
        };

        var properties = new BasicProperties()
        {
            ContentType = fileType
        };

        await _channel.BasicPublishAsync(
            exchange: Exchanges.FileExchange,
            routingKey: "file.queue.new",
            body: EncodingHelper.EncodeMessage(body), 
            basicProperties: properties,
            mandatory: false);
    }
}