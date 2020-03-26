using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WordCounter.Common;
using WordCounter.Worker.DAL;
using System;
using Nest;

namespace WordCounter.Worker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
               services.AddSingleton<IEnvironmentFacade, EnvironmentFacade>();
               services.AddTransient<IWordCountersRepository, WordCountersRepository>();
               services.AddSingleton<Connector, Connector>();
               services.AddHostedService<MessageHandler>();
               services.AddDbContext<CountResultsContext>();
               services.AddSingleton<IElasticClient>(BuildElasticClient);
           })
           .ConfigureLogging(logBuilder =>
           {
               logBuilder.ClearProviders();
               logBuilder.AddConsole();
           });

            await hostBuilder.RunConsoleAsync();
        }

        private static IElasticClient BuildElasticClient(IServiceProvider servProv)
        {
            var envFacade = servProv.GetService<IEnvironmentFacade>();
            if (envFacade == null)
            {
                throw new ArgumentNullException(nameof(envFacade));
            }
            var elasticSettings = envFacade.BuildElasticSettings();
            var clientSettings = new ConnectionSettings(new Uri(elasticSettings.HostName + ":" + elasticSettings.Port))
                .DefaultIndex(elasticSettings.IndexName)
                .DefaultMappingFor<BusinessMessage>(m => 
                    m.PropertyName(m2 => m2.CorrelationId, "correlation_id")
                    .PropertyName(m2 => m2.Content, "content"));

            IElasticClient client = new ElasticClient(clientSettings);
            return client;
        }
    }
}
