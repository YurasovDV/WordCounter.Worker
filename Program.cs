using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WordCountWorker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
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
