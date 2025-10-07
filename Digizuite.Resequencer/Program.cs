using Digizuite.Common.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.Resequencer;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;

    private static Dictionary<string, List<(int, ReadOnlyMemory<byte>)>> _sequenceBuffer = new();
    private static object _bufferLock = new();
    
    static async Task Main()
    {
        await SetupConnectionsAsync();
        await DeclareAsync();

        if (_channel is null)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        var originalConsumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var correlationId = ea.BasicProperties.CorrelationId;
                var orderId = ea.BasicProperties.Headers?["OrderId"];

                if (correlationId is null || orderId is null)
                {
                    Console.WriteLine("[Resequencer] CorrelationId or OrderId is missing.");
                    return;
                }
                
                AddToBuffer(correlationId, (int) orderId, ea.Body.ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine("[Resequencer] Failed to consume message");
            }
        };

        originalConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var correlationId = ea.BasicProperties.CorrelationId;
                var orderId = ea.BasicProperties.Headers?["OrderId"] ?? 1;

                if (correlationId is null)
                {
                    Console.WriteLine("[Resequencer] Found no correlationId");
                    return;
                }
                
                AddToBuffer(correlationId, (int) orderId, ea.Body.ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine("[Resequencer] Failed to consume message");
            }
        };
        

        await _channel.BasicConsumeAsync(Queues.HandledQueue, true, consumer);
        await _channel.BasicConsumeAsync(Queues.Mp4OriginalQueue, true, originalConsumer);
        
        Console.WriteLine("[Resequencer] Ready to receive messages");
        Console.ReadLine();
    }

    private static async Task AddToBuffer(string correlationId, int orderId, ReadOnlyMemory<byte> body)
    {
        var fullSequence = false;
        
        lock (_bufferLock)
        {
            if (!_sequenceBuffer.TryGetValue(correlationId, out var list))
            {
                list = [];
                _sequenceBuffer[correlationId] = list;
            }
        
            list.Add((orderId, body));

            Console.WriteLine("[Resequencer] Added {0} for correlation id {1} to buffer", orderId, correlationId);

            if (_sequenceBuffer[correlationId].Count == 3)
            {
                Console.WriteLine("[Resequencer] Full sequence for {0} sending to storage!", correlationId);
                fullSequence = true;
            }
        }
        
        if (fullSequence)
            await ProcessSequenceAsync(correlationId);
    }

    private static async Task ProcessSequenceAsync(string correlationId)
    {
        if (_channel is null)
            return;
        
        List<(int, ReadOnlyMemory<byte>)> messages;
        
        lock (_bufferLock)
        {
            if (!_sequenceBuffer.Remove(correlationId, out messages!))
            {
                Console.WriteLine("[Resequencer] Warning: Sequence {0} not found", correlationId);
                return;
            }
        }
        
        var properties = new BasicProperties()
        {
            CorrelationId = correlationId
        };
        
        var sorted = messages
            .OrderBy(x => x.Item1)
            .ToArray();
        
        foreach (var item in sorted)
            await _channel.BasicPublishAsync(Exchanges.StorageExchange, $"store.file", true, properties, item.Item2);
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

        await _channel.ExchangeDeclareAsync(Exchanges.HandledExchange, ExchangeType.Direct, false, true);

        await _channel.QueueDeclareAsync(Queues.HandledQueue, false, false, true);
        await _channel.QueueBindAsync(Queues.HandledQueue, Exchanges.HandledExchange, "press.handled.mp4");
        await _channel.QueueBindAsync(Queues.HandledQueue, Exchanges.HandledExchange, "web.handled.mp4");
    }
}