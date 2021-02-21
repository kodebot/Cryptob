using System;
using System.Threading.Tasks;
using Cryptob.Application.Bots;
using Cryptob.Core.Configuration;
using Cryptob.Core.Configuration.Strategies;
using Cryptob.Core.Exchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Sinks.Telegram;

namespace Cryptob
{
    class Program
    {
        public static IConfigurationRoot Configuration;

        static async Task<int> Main(string[] args)
        {
            try
            {
                // Build configuration
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json",
                        false)
                    .Build();

                ConfigureLogger();

                await LaunchAsync(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal: {ex.Message}, {ex.StackTrace}");
                return 1;
            }
        }

        private static void ConfigureLogger()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Information, theme: AnsiConsoleTheme.Code)
                .WriteTo.RollingFile("Logs\\cryptob-{Date}.log")
                .MinimumLevel.Debug()
                .Enrich.FromLogContext();

            AddTelegramLogger(loggerConfig);
            Log.Logger = loggerConfig.CreateLogger();
        }


        static async Task LaunchAsync(string[] args)
        {
            // Create service collection
            Log.Debug("Creating service collection");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Create service provider
            Log.Debug("Building service provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Debug("Starting service");
                var app = serviceProvider.GetRequiredService<App>();
                await app.Run();
                Log.Debug("Ending service");
                Console.WriteLine("Press <ANY> key to close");
                Console.Read();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running service");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Add logging
            serviceCollection.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            }));

            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<IConfigurationRoot>(Configuration);
            serviceCollection.Configure<GeneralConfig>(Configuration.GetSection("general"));
            serviceCollection.Configure<BinanceUserConfig>(Configuration.GetSection("binanceUserConfig"));
            serviceCollection.Configure<SlowFollowerConfig>(Configuration.GetSection("strategies:slowfollower"));
            serviceCollection.Configure<PriceBurstConfig>(Configuration.GetSection("strategies:priceBurst"));
            serviceCollection.AddTransient<PriceBurstStrategyBot>();
            serviceCollection.AddTransient<BuyDipStrategyBot>();
            serviceCollection.AddTransient<MarketMakerStrategyBot>();
            serviceCollection.AddTransient<SlowFollowerStrategyBot>();

            serviceCollection.AddTransient<IStrategySelector, StrategySelector>();
            serviceCollection.AddTransient<IStrategyBotFactory, StrategyBotFactory>();
            serviceCollection.AddTransient<IBinanceSpotExchange, BinanceSpotExchange>();
            serviceCollection.AddSingleton<App>();
        }
        private static void AddTelegramLogger(LoggerConfiguration loggerConfig)
        {
            var token = Configuration.GetSection("telegramConfig").GetValue<string>("botToken");
            var chatId = Configuration.GetSection("telegramConfig").GetValue<string>("botChatId");
            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(chatId))
            {
                Log.Debug("Telegram bot setting found, configuring Telegram logger...");
                loggerConfig.WriteTo.Telegram(
                    token,
                    chatId,
                    restrictedToMinimumLevel: LogEventLevel.Error);
            }
        }
    }
}
