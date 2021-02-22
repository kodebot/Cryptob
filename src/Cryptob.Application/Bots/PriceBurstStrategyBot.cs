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
    public class PriceBurstStrategyBot : IStrategyBot
    {
        private readonly IBinanceSpotExchange _binanceSpotExchange;
        private readonly ILogger<PriceBurstStrategyBot> _logger;
        private readonly PriceBurstConfig _priceBurstConfig;
        public PriceBurstStrategyBot(
            IBinanceSpotExchange binanceSpotExchange,
            IOptions<PriceBurstConfig> priceBurstConfig,
            ILogger<PriceBurstStrategyBot> logger)
        {
            _binanceSpotExchange = binanceSpotExchange;
            _logger = logger;
            _priceBurstConfig = priceBurstConfig.Value;
        }

        public async Task Start()
        {
            _logger.LogInformation("Starting {@PriceBurst} strategy bot", Strategy.PriceBurst.ToString());
            try
            {
                var symbol = _binanceSpotExchange.GetSymbol(_priceBurstConfig.Base, _priceBurstConfig.Quote);
                _logger.LogInformation("Trading pair {@symbol}", symbol);
                _logger.LogInformation("Trading settings: SpreadInQuoteCoin={@SpreadInQuoteCoin}, TradeProfit={@TradeProfitPercentage}%, TradeQuantity={@TradeQuantity}",
                    _priceBurstConfig.TradeDifference, _priceBurstConfig.TradeProfitPercentage, _priceBurstConfig.TradeQuantity);

                var orderId = default(long?);

                while (true)
                {
                    _logger.LogInformation("Start of next tick");
                    var orderBook = await _binanceSpotExchange.GetOrderBookAsync(symbol, 5);
                    var baseCurrencyPrice = (await _binanceSpotExchange.Get24HourPriceAsync(symbol)).LastPrice;
                    var baseCurrencyBalance = _binanceSpotExchange.GetBalanceAsync(_priceBurstConfig.Base);
                    var lastBid = orderBook.Bids.First().Price;
                    var lastAsk = orderBook.Asks.First().Price;
                    var buyPrice = lastBid + _priceBurstConfig.TradeDifference;
                    var sellPrice = lastAsk - _priceBurstConfig.TradeDifference;
                    var profitablePrice = buyPrice * (1 + Convert.ToDecimal(_priceBurstConfig.TradeProfitPercentage) / 100);
                    var difference = lastAsk - profitablePrice;


                   _logger.LogInformation("BuyPrice={@buyPrice}, SellPrice={@sellPrice}, Bid={@lastBid}, Ask={@lastAsk}, Price={@profitablePrice}, Diff={@difference}",
                       buyPrice, sellPrice, lastBid, lastAsk, profitablePrice, difference); 


                    Thread.Sleep(5 * 1000);
                }


                //var btcBalance = await _binanceSpotExchange.GetBalanceAsync(Coin.BTC);
                //_logger.LogInformation("Balance: {@btcBalance}", btcBalance);

                //var usdtBalance = await _binanceSpotExchange.GetBalanceAsync(Coin.USDT);
                //_logger.LogInformation("Balance: {@usdtBalance}", usdtBalance);

                //var symbol = _binanceSpotExchange.GetSymbol(
                //    Coin.BTC,
                //    Coin.USDT);

                //var currentBtcUsdtPrice = await ((IExchangeClient)client).GetTickerAsync("BTCUSDT");
                //_logger.LogInformation("Current Balance: BTC={btcBalance}, USDT={usdtBalance}", btcBalance, usdtBalance);
                //var filters = client.Spot.System.GetExchangeInfo().Data.Symbols.Single(s => s.Name == "BTCUSDT").Filters;

                //var orderBook = await _binanceSpotExchange.GetOrderBookAsync(symbol, 5);
                //_logger.LogDebug("{@orderBook}", orderBook);
                //var buyPrice = orderBook.Bids.FirstOrDefault()?.Price ?? orderBook.Asks.First().Price;
                //var sellPrice = orderBook.Asks.First().Price;

                //await _binanceSpotExchange.PlaceLimitBuyOrderAsync(symbol, 0.01m, buyPrice, false);
                //await _binanceSpotExchange.PlaceMarketBuyOrderAsync(symbol, 0.01m, false);

                //await _binanceSpotExchange.PlaceLimitSellOrderAsync(symbol, 0.01m, sellPrice, false);
                //await _binanceSpotExchange.PlaceMarketSellOrderAsync(symbol, 0.01m, false);

                //var allOrders = await _binanceSpotExchange.GetOpenOrdersAsync("BTCUSDT");
                ////foreach (var binanceOrder in allOrders)
                //{
                //await _binanceSpotExchange.CancelAllOpenOrdersAsync("BTCUSDT");
                //}


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when running {@PriceBurst} strategy bot, exiting...", Strategy.PriceBurst.ToString());
            }
        }

    }
}
