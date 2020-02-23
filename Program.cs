using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WordCounter.Common;

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
               services.AddSingleton<Connector, Connector>();
               services.AddHostedService<MessageHandler>();
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
