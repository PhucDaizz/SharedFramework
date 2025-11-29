using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SharedLibrary.Configuration;
using SharedLibrary.Interfaces;
using System.Text;
using System.Text.Json;

namespace SharedLibrary.Services
{
    public class RabbitMQPublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQPublisher> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public static async Task<RabbitMQPublisher> CreateAsync(IOptions<RabbitMQSettings> options, ILogger<RabbitMQPublisher> logger)
        {
            var publisher = new RabbitMQPublisher(options, logger);

            var factory = new ConnectionFactory
            {
                HostName = publisher._settings.HostName,
                Port = publisher._settings.Port,
                UserName = publisher._settings.UserName,
                Password = publisher._settings.Password,
                VirtualHost = publisher._settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            publisher._connection = await factory.CreateConnectionAsync();
            publisher._channel = await publisher._connection.CreateChannelAsync();

            return publisher;
        }

        public async Task PublishAsync<T>(string exchange, string exchangeType, string routingKey, T message)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized.");

            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Published message to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized.");

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Published message to queue {Queue}", queueName);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}
