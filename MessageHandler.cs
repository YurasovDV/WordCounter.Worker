using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WordCounter.Common;

namespace WordCountWorker
{
    public class MessageHandler : BackgroundService
    {
        private bool _disposed = false;
        private IConnection _connection;
        private readonly ILogger<MessageHandler> _logger;

        public MessageHandler(ILogger<MessageHandler> logger)
        {
            _logger = logger;
            _connection = GetConnection();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var processor = new Processor();

                channel.ExchangeDeclare(Constants.ArticlesExchange, ExchangeType.Fanout);
                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queueName, Constants.ArticlesExchange, Constants.RoutingKey);
                Console.WriteLine("LISTENING");
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, e) =>
                {
                    var bytes = e.Body;
                    var msg = JsonConvert.DeserializeObject<BusinessMessage>(Encoding.UTF8.GetString(bytes));
                    processor.Process(msg);
                };
                channel.BasicConsume(queueName, autoAck: true, consumer);

                while (!stoppingToken.IsCancellationRequested)
                {

                }

                Console.WriteLine("gracefully exited");
                return Task.CompletedTask;
            }
        }

        private IConnection GetConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable(Constants.RabbitMqHost),
                Port = int.Parse(Environment.GetEnvironmentVariable(Constants.RabbitMqPort)),
                UserName = Environment.GetEnvironmentVariable(Constants.RabbitMqUser),
                Password = Environment.GetEnvironmentVariable(Constants.RabbitMqPass),
            };

            int retriesLeft = 6;
            while (true)
            {
                retriesLeft--;
                try
                {
                    var connection = factory.CreateConnection();
                    return connection;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"connect to queue failed: {ex.Message}");
                    if (retriesLeft == 0)
                    {
                        _logger.LogError("no retries left");
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2).Milliseconds);
                }
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                if (_connection != null)
                {
                    _connection.Dispose();
                }
                base.Dispose();
            }
        }
    }
}
