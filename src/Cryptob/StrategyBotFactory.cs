using System;
using Cryptob.Application.Bots;
using Cryptob.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptob
{
    public interface IStrategyBotFactory
    {
        IStrategyBot Get(Strategy strategy);
    }

    public class StrategyBotFactory : IStrategyBotFactory
    {
        private readonly IServiceProvider _provider;

        public StrategyBotFactory(IServiceProvider provider)
        {
            _provider = provider;
        }
        public IStrategyBot Get(Strategy strategy)
        {
            if (strategy == Strategy.PriceBurst)
            {
                return _provider.GetService<PriceBurstStrategyBot>();
            }
            else if (strategy == Strategy.BuyDip)
            {
                return _provider.GetService<BuyDipStrategyBot>();
            }
            else if (strategy == Strategy.MarketMaker)
            {
                return _provider.GetService<MarketMakerStrategyBot>();
            }
            else if (strategy == Strategy.SlowFollower)
            {
                return _provider.GetService<SlowFollowerStrategyBot>();
            }

            throw new NotSupportedException($"The strategy specified {strategy} is not supported.");
        }
    }
}
