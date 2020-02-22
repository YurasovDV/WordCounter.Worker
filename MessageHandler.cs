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
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var t = new Task(async () =>
            {
                _connection = await GetConnection();

                using (var channel = _connection.CreateModel())
                {
                    var processor = new Processor();

                    channel.ExchangeDeclare(Constants.ArticlesExchange, ExchangeType.Fanout);
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queueName, Constants.ArticlesExchange, Constants.RoutingKey);
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
                }
            });
            t.Start();
            return t;
        }

        private async Task<IConnection> GetConnection()
        {
            return await Task.Run(async () =>
             {
                 var factory = new ConnectionFactory()
                 {
                     HostName = Environment.GetEnvironmentVariable(Constants.RabbitMqHost),
                     Port = int.Parse(Environment.GetEnvironmentVariable(Constants.RabbitMqPort)),
                     UserName = Environment.GetEnvironmentVariable(Constants.RabbitMqUser),
                     Password = Environment.GetEnvironmentVariable(Constants.RabbitMqPass),
                 };


                 var end = DateTime.UtcNow.AddMinutes(3);
                 Exception toLog = null;
                 while (DateTime.UtcNow < end)
                 {
                     try
                     {
                         var connection = factory.CreateConnection();
                         return connection;
                     }
                     catch (Exception ex)
                     {
                         toLog = ex;
                         _logger.LogWarning($"connect to queue failed: {ex.Message}");
                     }
                     await Task.Delay((int)TimeSpan.FromSeconds(2).TotalMilliseconds);
                 }
                 if (toLog != null)
                 {
                     throw toLog;
                 }
                 throw new Exception("no retries left");

             });
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
