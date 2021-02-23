using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFR_Messaging.Config;

namespace SFR_Messaging
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) => config.AddEnvironmentVariables("SFR_"))
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<KafkaSettings>(hostContext.Configuration.GetSection(nameof(KafkaSettings)));
                    services.AddHostedService<CoreBankingSystem.CoreBankingSystem>();
                    services.AddHostedService<MoneyLaunderingService.MoneyLaunderingService>();
                    services.AddHostedService<TransactionAnalyticsService.TransactionAnalyticsService>();
                });
    }
}