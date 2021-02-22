using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cryptob.Core;
using Cryptob.Core.Configuration.Strategies;
using Cryptob.Core.Exchange;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cryptob.Application.Bots
{
    public class MarketMakerStrategyBot : IStrategyBot
    {
        private readonly ILogger<MarketMakerStrategyBot> _logger;
        private readonly MarketMakerConfig _marketMakerConfig;
        private readonly IBinanceSpotExchange _binanceSpotExchange;

        public MarketMakerStrategyBot(ILogger<MarketMakerStrategyBot> logger,
            IOptions<MarketMakerConfig> marketMakerConfig, IBinanceSpotExchange binanceSpotExchange)
        {
            _logger = logger;
            _marketMakerConfig = marketMakerConfig.Value;
            _binanceSpotExchange = binanceSpotExchange;
        }

        public async Task Start()
        {
            _logger.LogInformation(
                "Starting {@MarketMaker} strategy bot",
                Strategy.MarketMaker.ToString());
            try
            {
                var symbol = _binanceSpotExchange.GetSymbol(
                    _marketMakerConfig.Base,
                    _marketMakerConfig.Quote);
                _logger.LogInformation(
                    "Trading pair {@symbol}",
                    symbol);
                _logger.LogInformation(
                    "Trading settings: TradeDifference={@TradeDifference}{@Quote}, TradeAssetAllocationInPercentage={@TradeAssetAllocationPercentage}%",
                    _marketMakerConfig.TradeDifference,
                    _marketMakerConfig.Quote.ToString(),
                    _marketMakerConfig.TradeAssetAllocationPercentage);


                while (true)
                {
                    _logger.LogInformation("Start of next tick");
                    var orderBook = await _binanceSpotExchange.GetOrderBookAsync(
                        symbol,
                        5);
                    var baseCurrencyPrice = Coin.Round( (await _binanceSpotExchange.Get24HourPriceAsync(symbol)).LastPrice, _marketMakerConfig.Quote);
                    var baseCurrencyBalance = await _binanceSpotExchange.GetBalanceAsync(_marketMakerConfig.Base);
                    var quoteCurrencyBalance = await _binanceSpotExchange.GetBalanceAsync(_marketMakerConfig.Quote);
                    var lastBid = Coin.Round( orderBook.Bids.First().Price, _marketMakerConfig.Quote);
                    var lastAsk = Coin.Round(orderBook.Asks.First().Price, _marketMakerConfig.Quote);
                    var buyPrice = baseCurrencyPrice + _marketMakerConfig.TradeDifference;
                    var sellPrice = baseCurrencyPrice - _marketMakerConfig.TradeDifference;

                    // todo: check we have enough free balance
                    // todo: make sure we have enough balance for both buy and sell order

                    var buyQuantity = Coin.Round(quoteCurrencyBalance.CommonTotal * _marketMakerConfig.TradeAssetAllocationPercentage / 100 / baseCurrencyPrice, _marketMakerConfig.Base);
                    var sellQuantity = buyQuantity; // same as buy quantity


                    _logger.LogInformation(
                        "BuyPrice={@buyPrice}, BuyQty={@buyQuantity}, SellPrice={@sellPrice}, SellQty={@sellQuantity}, Bid={@lastBid}, Ask={@lastAsk}, Price={@baseCurrencyPrice}",
                        buyPrice,
                        buyQuantity,
                        sellPrice,
                        sellQuantity,
                        lastBid,
                        lastAsk,
                        baseCurrencyPrice);

                    // TODO: scenarios
                    // 1. No pending orders - (new orders can be placed)
                    // 2. Both pending
                    // 3. Order partially completed - convert pending orders to market order (only some quantities are sold or bought)
                    // 4. only one of two orders are completed - convert pending limit order to market order



                    Thread.Sleep(5 * 1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when running {@MarketMaker} strategy bot, exiting...", Strategy.MarketMaker.ToString());
            }
        }
    }
}
