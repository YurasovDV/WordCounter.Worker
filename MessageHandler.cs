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

namespace WordCounter.Worker
{
    public class MessageHandler : BackgroundService
    {
        private bool _disposed = false;
        private readonly ILogger<MessageHandler> _logger;
        private readonly IEnvironmentFacade _environment;
        private readonly Connector _connector;
        private IConnection _queueconnection;
        private EventingBasicConsumer _consumer;

        public MessageHandler(ILogger<MessageHandler> logger, IEnvironmentFacade environment, Connector connector)
        {
            _logger = logger;
            _environment = environment;
            _connector = connector;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var t = new Task(() =>
            {
                _queueconnection = GetConnection();
                WaitForDb();
                using (var channel = _queueconnection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.ArticlesExchange, ExchangeType.Fanout);
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queueName, Constants.ArticlesExchange, Constants.RoutingKey);

                    _consumer = new EventingBasicConsumer(channel);
                    _consumer.Received += Handle;
                    channel.BasicConsume(queueName, autoAck: true, _consumer);

                    while (!stoppingToken.IsCancellationRequested)
                    {

                    }
                }
            });
            t.Start();
            return t;
        }


        private void Handle(object sender, BasicDeliverEventArgs e)
        {
            var bytes = e.Body;
            var msg = JsonConvert.DeserializeObject<BusinessMessage>(Encoding.UTF8.GetString(bytes));
            var _processor = new WordCounterProcessor();
            _processor.Process(msg);
        }

        private void WaitForDb()
        {
            _connector.EnsureDbIsUp(_logger, _environment.BuildDbSettings());
        }

        private IConnection GetConnection()
        {
            return _connector.ConnectToQueue(_logger, _environment.BuildQueueSettings());
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                if (_consumer != null)
                {
                    _consumer.Received -= Handle;
                }

                if (_queueconnection != null)
                {
                    _queueconnection.Dispose();
                }
                base.Dispose();
            }
        }
    }
}
