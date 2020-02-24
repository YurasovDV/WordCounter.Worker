using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WordCounter.Common;
using WordCounter.Worker.DAL;

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
           })
           .ConfigureLogging(logBuilder =>
           {
               logBuilder.ClearProviders();
               logBuilder.AddConsole();
           });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
