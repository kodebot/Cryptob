using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cryptob
{

    public class App
    {
        private readonly ILogger<App> _logger;
        private readonly IStrategySelector _strategySelector;
        private readonly IStrategyBotFactory _strategyBotFactory;

        public App(
            ILogger<App> logger,
            IStrategySelector strategySelector,
            IStrategyBotFactory strategyBotFactory)
        {
            _logger = logger;
            _strategySelector = strategySelector;
            _strategyBotFactory = strategyBotFactory;
        }

        public async Task Run()
        {
            _logger.LogInformation("App started");

            _logger.LogInformation("Selecting strategy...");
            var strategy = _strategySelector.GetStrategy();
            _logger.LogInformation("Selected Strategy {@strategy}", strategy.ToString());

            _logger.LogInformation("Creating bot...");
            var bot = _strategyBotFactory.Get(strategy);
            _logger.LogInformation("Created bot for strategy {@strategy}", strategy.ToString());

            _logger.LogInformation("Starting bot...");
            await bot.Start();
        }
    }
}
