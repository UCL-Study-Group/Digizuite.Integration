using Digizuite.Common.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;
namespace Digizuite.Recipient;

class Program
{
  private static IConnection? _connection;
  private static IChannel? _channel;
  static async Task Main()
  {
    await SetupConnectionsAsync();
    await DeclareAsync();

    if (_channel is null)
      return;

    var consumer = new AsyncEventingBasicConsumer(_channel);

    //Consumer fra "new mp4" køen
    consumer.ReceivedAsync += async (model, ea) =>
    {
      try
      {
        var routingKey = ea.RoutingKey;
        var id = ea.BasicProperties.CorrelationId;
        
        Console.WriteLine("[Recipient] Received message with routing key: {0} and CorrelationId: {1}", routingKey, id);

        await Task.Delay(5000);

        var properties = new BasicProperties()
        {
          CorrelationId = id,
        };

        await _channel.BasicPublishAsync(
          exchange: Exchanges.RecipientExchange,
          routingKey: string.Empty,
          body: ea.Body,
          basicProperties: properties, 
          mandatory: true);

        Console.WriteLine("[Recipient] Message broadcasted to Web, Press & Original queues");
      }

      catch (Exception ex)
      {
        Console.WriteLine("[Recipient] Failed to receive MP4-file {0}", ex.Message);
      }

    };

    Console.WriteLine("[Recipient] Ready to route!");
    await _channel.BasicConsumeAsync("mp4.queue.new", autoAck: true, consumer);
    
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

  private static async Task DeclareAsync()
  {
    if (_channel is null)
      return;

    //exchanges
    await _channel.ExchangeDeclareAsync(Exchanges.RecipientExchange, ExchangeType.Fanout, durable: false);

    //queues
    await _channel.QueueDeclareAsync("mp4.queue.new", durable: false, exclusive: false, autoDelete: false);
    await _channel.QueueDeclareAsync(Queues.Mp4WebQueue, durable: false, exclusive: false, autoDelete: false);
    await _channel.QueueDeclareAsync(Queues.Mp4PressQueue, durable: false, exclusive: false, autoDelete: false);
    await _channel.QueueDeclareAsync(Queues.Mp4OriginalQueue, durable: false, exclusive: false, autoDelete: false);

    await _channel.QueueBindAsync("mp4.queue.new", Exchanges.Mp4Exchange, "new.mp4");
    await _channel.QueueBindAsync(Queues.Mp4WebQueue, Exchanges.RecipientExchange, string.Empty);
    await _channel.QueueBindAsync(Queues.Mp4PressQueue, Exchanges.RecipientExchange, string.Empty);
    await _channel.QueueBindAsync(Queues.Mp4OriginalQueue, Exchanges.RecipientExchange, string.Empty);
  }
}