using Cryptob.Core;
using Cryptob.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Cryptob
{
    public interface IStrategySelector
    {
        Strategy GetStrategy();
    }

    public class StrategySelector : IStrategySelector
    {
        private readonly GeneralConfig _generalConfig;
        public StrategySelector(IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig.Value;
        }
        public Strategy GetStrategy()
        {
            // TODO: implement smart logic to select the strategy automatically
            return _generalConfig.ActiveStrategy;
        }
    }
}
