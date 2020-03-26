using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WordCounter.Common;
using WordCounter.Worker.DAL;

namespace WordCounter.Worker
{
    public class MessageHandler : BackgroundService
    {
        private bool _disposed = false;
        private readonly ILogger<MessageHandler> _logger;
        private readonly IEnvironmentFacade _environment;
        private readonly Connector _connector;
        private readonly IWordCountersRepository _wordCountersRepository;
        private readonly IElasticClient _elasticClient;
        private IConnection _queueconnection;
        private EventingBasicConsumer _consumer;

        public MessageHandler(ILogger<MessageHandler> logger, IEnvironmentFacade environment, Connector connector, IWordCountersRepository wordCountersRepository, IElasticClient elasticClient)
        {
            _logger = logger;
            _environment = environment;
            _connector = connector;
            _wordCountersRepository = wordCountersRepository;
            _elasticClient = elasticClient;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var t = new Task(() =>
            {
                _logger.LogInformation("WordCounter.Worker: waiting for connections");
                _queueconnection = GetQueueConnection();
                _logger.LogInformation("WordCounter.Worker: connected to queue");
                _wordCountersRepository.WaitForDb();
                _logger.LogInformation("WordCounter.Worker: connected to db");
                EnsureElasticIsUp();
                _logger.LogInformation("WordCounter.Worker: connected to elastic");
                using (var channel = _queueconnection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.ArticlesExchange, ExchangeType.Fanout);
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queueName, Constants.ArticlesExchange, Constants.RoutingKey);

                    _consumer = new EventingBasicConsumer(channel);
                    _consumer.Received += Handle;
                    channel.BasicConsume(queueName, autoAck: true, _consumer);

                    _logger.LogInformation("WordCounter.Worker: LISTENING");

                    while (!stoppingToken.IsCancellationRequested)
                    {

                    }
                }
            });
            t.ContinueWith(finished => _logger.LogError(finished.Exception?.Message), TaskContinuationOptions.OnlyOnFaulted);
            t.Start();
            return t;
        }

        private void EnsureElasticIsUp()
        {
            bool ping()
            {
                var response = _elasticClient.Search<BusinessMessage>(spec => spec
                 .From(0)
                 .Size(1)
                 .Query(q => q.MatchAll()));

                if (response.IsValid)
                {
                    return true;
                }
                return false;
            }

            _connector.EnsureIsUp(_logger, _environment.BuildElasticSettings(), ping);
        }

        private void Handle(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                _logger.LogInformation("WordCounter.Worker: got message");
                var bytes = e.Body;
                var msg = JsonConvert.DeserializeObject<BusinessMessage>(Encoding.UTF8.GetString(bytes));
                var _processor = new WordCounterProcessor();
                var processResult = _processor.Process(msg);
                if (processResult.Status == OperationStatus.Success)
                {
                    _wordCountersRepository.Create(new CountResultRow() { CorrelationId = msg.CorrelationId, WordCount = processResult.Data.WordCount });
                    var addToIndexInBackground = new Task(
                        () => { 
                            _elasticClient.IndexDocument<BusinessMessage>(msg); 
                        })
                        .ContinueWith(finished => _logger.LogError(finished.Exception?.Message), TaskContinuationOptions.OnlyOnFaulted);
                    addToIndexInBackground.Start();
                    
                }
                else
                {
                    var errors = string.Join(';', processResult.Errors);
                    _logger.LogError($"msg {msg.CorrelationId} was not processed: {errors}");
                    // send another message to queue
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during message processing: {ex.Message}");
                throw;
            }
        }

        private IConnection GetQueueConnection()
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
