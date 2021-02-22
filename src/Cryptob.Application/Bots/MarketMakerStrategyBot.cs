using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cryptob.Core;
using Cryptob.Core.Configuration.Strategies;
using Cryptob.Core.Exchange;
using CryptoExchange.Net.ExchangeInterfaces;
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
                    "Trading settings: SpreadInQuoteCoin={@SpreadInQuoteCoin}{@Quote}, TradeAssetAllocationInPercentage={@TradeAssetAllocationPercentage}%",
                    _marketMakerConfig.SpreadInQuoteCoin,
                    _marketMakerConfig.Quote.ToString(),
                    _marketMakerConfig.TradeAssetAllocationPercentage);


                var tickCounter = 0;
                while (true)
                {
                    try
                    {
                        tickCounter++;

                        if (tickCounter > _marketMakerConfig.StopAfterTicks)
                        {
                            _logger.LogInformation(
                                "\n============================================================================================================\n");
                            _logger.LogInformation(
                                "Tick stop limit reached.Exiting... StopAfterTick={@StopAfterTick}",
                                _marketMakerConfig.StopAfterTicks);
                            await HandlePendingOrders(symbol);
                            _logger.LogInformation(
                                "\n========================================================END====================================================\n");
                            break;
                        }

                        _logger.LogInformation(
                            "\n============================================================================================================\n");
                        _logger.LogInformation(
                            "Start of new tick. TickInterval={@TickIntervalInSeconds}, TickCount={@tickCounter}",
                            _marketMakerConfig.TickIntervalInSeconds,
                            tickCounter);
                        //var symbolInfo = await _binanceSpotExchange.GetSymbolInfoAsync(symbol);

                        await HandlePendingOrders(symbol);

                        _logger.LogInformation("Preparing for new orders...");
                        var orderBook = await _binanceSpotExchange.GetOrderBookAsync(
                            symbol,
                            5);

                        var baseCurrencyBalance = await _binanceSpotExchange.GetBalanceAsync(_marketMakerConfig.Base);
                        var quoteCurrencyBalance = await _binanceSpotExchange.GetBalanceAsync(_marketMakerConfig.Quote);

                        var lastBid = Coin.Round(
                            orderBook.Bids.First().Price,
                            _marketMakerConfig.Quote);

                        var lastAsk = Coin.Round(
                            orderBook.Asks.First().Price,
                            _marketMakerConfig.Quote);

                        var realMarketPrice = Coin.Round(
                            (await _binanceSpotExchange.Get24HourPriceAsync(symbol)).LastPrice,
                            _marketMakerConfig.Quote);

                        var marketPrice = Coin.Round(
                            (lastAsk + lastBid) / 2,
                            _marketMakerConfig.Quote);


                        var buyPrice = marketPrice - _marketMakerConfig.SpreadInQuoteCoin;
                        var sellPrice = marketPrice + _marketMakerConfig.SpreadInQuoteCoin;

                        //var buyQuantity = Coin.Round(
                        //    Math.Max(12, quoteCurrencyBalance.CommonTotal * _marketMakerConfig.TradeAssetAllocationPercentage / 100) /
                        //    marketPrice,
                        //    _marketMakerConfig.Base);
                        //var sellQuantity = buyQuantity; // same as buy quantity

                        var buyQuantity = Coin.Round(_marketMakerConfig.TradeFixedSizeInQuote / marketPrice, _marketMakerConfig.Base);
                        var sellQuantity = buyQuantity;

                        _logger.LogInformation(
                            "Order Params. BuyPrice={@buyPrice}, MarketPrice={@marketPrice}, SellPrice={@sellPrice}, RealMarketPrice={@realMarketPrice}, BuyQty={@buyQuantity}, SellQty={@sellQuantity}, Bid={@lastBid}, Ask={@lastAsk}",
                            buyPrice,
                            marketPrice,
                            sellPrice,
                            realMarketPrice,
                            buyQuantity,
                            sellQuantity,
                            lastBid,
                            lastAsk);

                        var sellingCost = sellPrice * sellQuantity;
                        var buyingCost = buyPrice * buyQuantity;
                        var fees = (sellingCost + buyingCost) * 0.001m;
                        var profit = (sellPrice * sellQuantity) - (buyQuantity * buyPrice) - fees;
                        _logger.LogInformation("Is it profitable? sellingCost={@sellingCost}, buyingCost={@buyingCost}, fees={@fees}, profit={@profit}. All price in {@Quote}",
                                sellingCost,
                                buyingCost,
                                fees,
                                profit,
                                _marketMakerConfig.Quote.ToString());

                        if (profit > 0.1m)
                        {

                            _logger.LogInformation(
                                "Expected Profit: {@profit}{@Quote}",
                                profit,
                                _marketMakerConfig.Quote.ToString());

                            var hasEnoughToPlaceBuyOrder =
                                buyPrice * buyQuantity < quoteCurrencyBalance.CommonAvailable;
                            var hasEnoughToPlaceSellOrder = baseCurrencyBalance.CommonAvailable > sellQuantity;

                            if (!hasEnoughToPlaceBuyOrder)
                            {
                                _logger.LogError(
                                    "Not enough {@Quote} to place buy order",
                                    _marketMakerConfig.Quote.ToString());
                            }

                            if (!hasEnoughToPlaceSellOrder)
                            {
                                _logger.LogError(
                                    "Not enough {@Base} to place sell order",
                                    _marketMakerConfig.Base.ToString());
                            }

                            if (hasEnoughToPlaceBuyOrder && hasEnoughToPlaceSellOrder)
                            {
                                // TODO: scenarios
                                // 1. No pending orders - (new orders can be placed)
                                // 2. Both pending
                                // 3. Order partially completed - convert pending orders to market order (only some quantities are sold or bought)
                                // 4. only one of two orders are completed - convert pending limit order to market order

                                // TODO: re-balance trading pair

                                var ordersResult = await Task.WhenAll(
                                    _binanceSpotExchange.PlaceLimitBuyOrderAsync(
                                        symbol,
                                        buyQuantity,
                                        buyPrice,
                                        _marketMakerConfig.Test),
                                    _binanceSpotExchange.PlaceLimitSellOrderAsync(
                                        symbol,
                                        sellQuantity,
                                        sellPrice,
                                        _marketMakerConfig.Test));

                                if (ordersResult[0] && ordersResult[1])
                                {
                                    _logger.LogInformation("Buy and Sell orders placed successfully.");
                                }
                                else
                                {
                                    _logger.LogWarning("Buy and Sell orders NOT placed successfully.");
                                    await HandlePendingOrders(symbol);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Not enough profit to place the order. SKIPPING.... sellingCost={@sellingCost}, buyingCost={@buyingCost}, fees={@fees}, profit={@profit}. All price in {@Quote}",
                                sellingCost,
                                buyingCost,
                                fees,
                                profit,
                                _marketMakerConfig.Quote.ToString());
                        }

                        Thread.Sleep(_marketMakerConfig.TickIntervalInSeconds * 1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error. Cancelling all open orders");
                        await HandlePendingOrders(symbol);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when running {@MarketMaker} strategy bot, exiting...", Strategy.MarketMaker.ToString());
            }
        }

        private async Task HandlePendingOrders(string symbol)
        {
            await CancelOpenOrders(symbol);
        }

        private async Task CancelOpenOrders(string symbol)
        {
            _logger.LogInformation("Cancelling any open orders for {@symbol}", symbol);
            var cancelledOrders = await _binanceSpotExchange.CancelAllOpenOrdersAsync(symbol);
            if (cancelledOrders.Any())
            {
                _logger.LogInformation("Orders cancelled successfully {@cancelledOrders}", cancelledOrders);
            }
        }

        private async Task<bool> ConvertAllOpenLimitOrdersToMarketOrder(string symbol)
        {
            _logger.LogInformation("Converting any open limit orders for {@symbol} to market order", symbol);
            var openOrders = (await _binanceSpotExchange.GetOpenOrdersAsync(symbol)).ToList();
            var orderIds = openOrders.Select(o => o.OrderId).ToList();
            if (!orderIds.Any())
            {
                _logger.LogInformation("No Open Orders found for symbol {@symbol}", symbol);
                return true;
            }

            var limitToMarketConversionTasks = openOrders.Select(order => _binanceSpotExchange.ConvertLimitToMarketOrder(symbol, order.OrderId, _marketMakerConfig.Test));
            var result = await Task.WhenAll(limitToMarketConversionTasks);
            if (result.All(r => r))
            {
                _logger.LogInformation("Orders converted from limit to market successfully {@orderIds}", orderIds);
                return true;
            }

            _logger.LogWarning("Unable to convert all Limit orders to market order, {@orderIds}", orderIds);
            return false;
        }

    }
}
