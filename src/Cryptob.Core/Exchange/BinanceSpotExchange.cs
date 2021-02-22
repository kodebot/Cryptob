using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Cryptob.Core.Configuration;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.ExchangeInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cryptob.Core.Exchange
{
    public interface IBinanceSpotExchange
    {
        Task<bool> PlaceLimitBuyOrderAsync(string symbol, decimal quantity, decimal price, bool test = false);
        Task<bool> PlaceMarketBuyOrderAsync(string symbol, decimal quantity, bool test = false);
        Task<bool> PlaceLimitSellOrderAsync(string symbol, decimal quantity, decimal price, bool test = false);
        Task<bool> PlaceMarketSellOrderAsync(string symbol, decimal quantity, bool test = false);
        Task<ICommonBalance> GetBalanceAsync(string asset);
        Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int limit);
        Task<IEnumerable<BinanceOrder>> GetAllOrdersAsync(string symbol, int? limit = null);
        Task<IEnumerable<BinanceOrder>> GetOpenOrdersAsync(string symbol);
        Task<bool> CancelOrderAsync(string symbol, long orderId);
        Task<IEnumerable<string>> CancelAllOpenOrdersAsync(string symbol);
        string GetSymbol(string baseCurrency, string quoteCurrency);
        Task<IBinanceTick> Get24HourPriceAsync(string symbol);
    }

    public class BinanceSpotExchange : IBinanceSpotExchange, IDisposable
    {
        private readonly ILogger<BinanceSpotExchange> _logger;
        private readonly BinanceUserConfig _binanceUserConfig;
        private readonly IBinanceClient _client;

        public BinanceSpotExchange(IOptions<BinanceUserConfig> binanceUserConfig, ILogger<BinanceSpotExchange> logger)
        {
            _logger = logger;
            _binanceUserConfig = binanceUserConfig.Value;
            _client = GetClient();
        }
        public async Task<bool> PlaceLimitBuyOrderAsync(string symbol, decimal quantity, decimal price, bool test = false)
        {
            try
            {
                var result = await _client.PlaceTestOrLiveOrderAsync(
                    test,
                    symbol,
                    OrderSide.Buy,
                    OrderType.Limit,
                    quantity,
                    price: price,
                    timeInForce: TimeInForce.GoodTillCancel);
                if (!result.Success)
                {
                    _logger.LogError("Error while placing Limit Buy order for Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}", symbol, quantity, price, test);
                    _logger.LogError("{@Error}", result.Error);
                    return false;

                }

                _logger.LogInformation("Limit Buy order placed successfully. Order info= OrderId={@CommandId}, Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}",
                    result.Data.OrderId, symbol, quantity, price, test);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while placing Limit Buy order for Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}", symbol, quantity, price, test);
                return false;
            }
        }

        public async Task<bool> PlaceMarketBuyOrderAsync(string symbol, decimal quantity, bool test = false)
        {
            try
            {
                var result = await _client.PlaceTestOrLiveOrderAsync(
                    test,
                    symbol,
                    OrderSide.Buy,
                    OrderType.Market,
                    quantity);
                if (!result.Success)
                {
                    _logger.LogError("Error while placing Market Buy order for Symbol={@symbol}, Quantity={@quantity}, Test={@test}", symbol, quantity, test);
                    _logger.LogError("{@Error}", result.Error);
                    return false;

                }

                _logger.LogInformation("Market Buy order placed successfully. Order info= OrderId={@CommandId}, Symbol={@symbol}, Quantity={@quantity}, Test={@test}",
                    result.Data.OrderId, symbol, quantity, test);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while placing Market Buy order for Symbol={@symbol}, Quantity={@quantity}, Test={@test}", symbol, quantity, test);
                return false;
            }
        }

        public async Task<bool> PlaceLimitSellOrderAsync(string symbol, decimal quantity, decimal price, bool test = false)
        {
            try
            {
                var result = await _client.PlaceTestOrLiveOrderAsync(
                    test,
                    symbol,
                    OrderSide.Sell,
                    OrderType.Limit,
                    quantity,
                    price: price,
                    timeInForce: TimeInForce.GoodTillCancel);
                if (!result.Success)
                {
                    _logger.LogError("Error while placing Limit Sell order for Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}", symbol, quantity, price, test);
                    _logger.LogError("{@Error}", result.Error);
                    return false;

                }

                _logger.LogInformation("Limit Sell order placed successfully. Order info= OrderId={@CommandId}, Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}",
                    result.Data.OrderId, symbol, quantity, price, test);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while placing Limit Sell order for Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}", symbol, quantity, price, test);
                return false;
            }
        }

        public async Task<bool> PlaceMarketSellOrderAsync(string symbol, decimal quantity, bool test = false)
        {
            try
            {
                var result = await _client.PlaceTestOrLiveOrderAsync(
                    test,
                    symbol,
                    OrderSide.Sell,
                    OrderType.Market,
                    quantity);
                if (!result.Success)
                {
                    _logger.LogError("Error while placing Market Sell order for Symbol={@symbol}, Quantity={@quantity}, Test={@test}", symbol, quantity, test);
                    _logger.LogError("{@Error}", result.Error);
                    return false;

                }

                _logger.LogInformation("Market Sell order placed successfully. Order info= OrderId={@CommandId}, Symbol={@symbol}, Quantity={@quantity}, Test={@test}",
                    result.Data.OrderId, symbol, quantity, test);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while placing Market Sell order for Symbol={@symbol}, Quantity={@quantity}, Price={@price}, Test={@test}", symbol, quantity, test);
                return false;
            }
        }

        public async Task<ICommonBalance> GetBalanceAsync(string asset)
        {
            _logger.LogDebug("Loading balance for {@asset}", asset);
            var result = await _client.General.GetAccountInfoAsync();

            if (!result.Success)
            {
                _logger.LogError("Error while getting Spot balance for {@asset}, error={@Error}", asset, result.Error);
                throw new Exception($"Unable to get Spot balance for {asset}, error={result.Error?.Message}");
            }

            var balance = result.Data.Balances.FirstOrDefault(
            b => b.Asset.Equals(
                asset,
                StringComparison.OrdinalIgnoreCase));

            if (balance == null)
            {
                _logger.LogWarning("{@asset} not found in returned balances, returning zero balance", asset);
                return new BinanceBalance() { Asset = asset, Free = 0.0m, Locked = 0.0m };
            }

            return balance;
        }

        public async Task<bool> CancelOrderAsync(string symbol, long orderId)
        {
            _logger.LogDebug("Cancelling Spot order {@orderId} for {@symbol}", orderId, symbol);
            var result = await _client.Spot.Order.CancelOrderAsync(symbol, orderId);

            if (!result.Success)
            {
                _logger.LogError("Error while while cancelling Spot Order {@orderId} for {@symbol}, error={@Error}", orderId, symbol, result.Error);
                throw new Exception($"Unable to Cancel Spot Order {orderId} for {symbol} error={result.Error?.Message}");
            }

            if (result.Data.Status == OrderStatus.Canceled)
            {
                _logger.LogInformation("Spot Order {@orderId} for {@symbol} cancelled successfully. {@Data}", orderId, symbol, result.Data);
                return true;
            }
            else
            {
                _logger.LogWarning("Unable to cancel Spot Order {@orderId} for {@symbol}", orderId, symbol);
                return false;
            }
        }
        public async Task<IEnumerable<string>> CancelAllOpenOrdersAsync(string symbol)
        {
            _logger.LogDebug("Cancelling all open Spot orders for {@symbol}", symbol);
            var result = await _client.Spot.Order.CancelAllOpenOrdersAsync(symbol);

            if (!result.Success)
            {
                _logger.LogError("Error while while cancelling all open Spot Orders {@symbol}, error={@Error}", symbol, result.Error);
                throw new Exception($"Unable to Cancel all open Spot Orders for {symbol} error={result.Error?.Message}");
            }

            _logger.LogInformation("Spot Orders {@orderIds} for {@symbol} cancelled successfully", result.Data, symbol);
            return result.Data.Select(x => x.Id);
        }

        public async Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int limit)
        {
            _logger.LogDebug("Retrieving Order Book for {@symbol}, limited to {@limit} records", symbol, limit);
            var result = await _client.Spot.Market.GetOrderBookAsync(
                symbol,
                limit);

            if (!result.Success)
            {
                _logger.LogError("Error while getting Spot Order Book for {@symbol}, error={@Error}", symbol, result.Error);
                throw new Exception($"Unable to get Spot Order Book for {symbol}, error={result.Error?.Message}");
            }

            return result.Data;
        }
        public async Task<IEnumerable<BinanceOrder>> GetAllOrdersAsync(string symbol, int? limit = null)
        {
            _logger.LogDebug("Retrieving all Spot Orders for {@symbol}, limited to {@limit} records", symbol, limit);
            var result = await _client.Spot.Order.GetAllOrdersAsync(symbol, limit: limit);

            if (!result.Success)
            {
                _logger.LogError("Error while getting Spot Orders for {@symbol}, error={@Error}", symbol, result.Error);
                throw new Exception($"Unable to get Spot Orders for {symbol}, error={result.Error?.Message}");
            }

            return result.Data;
        }
        public async Task<IEnumerable<BinanceOrder>> GetOpenOrdersAsync(string symbol)
        {
            _logger.LogDebug("Retrieving Open Spot Orders for {@symbol}", symbol);
            var result = await _client.Spot.Order.GetOpenOrdersAsync(symbol);

            if (!result.Success)
            {
                _logger.LogError("Error while getting Open Spot Orders for {@symbol}, error={@Error}", symbol, result.Error);
                throw new Exception($"Unable to get Open Spot Orders for {symbol}, error={result.Error?.Message}");
            }

            return result.Data;
        }

        public string GetSymbol(string baseCurrency, string quoteCurrency)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(quoteCurrency))
            {
                _logger.LogError("Base and Quote currency must be specified to create symbol. Base={@baseCurrency}, Quote={@quoteCurrency}", baseCurrency, quoteCurrency);
                throw new Exception("Base and Quote currency must be specified to create symbol");
            }

            return $"{baseCurrency.ToUpperInvariant()}{quoteCurrency.ToUpperInvariant()}";
        }

        public async Task<IBinanceTick> Get24HourPriceAsync(string symbol)
        {
            _logger.LogDebug("Retrieving 24 hour price data for {@symbol}", symbol);
            var result = await _client.Spot.Market.Get24HPriceAsync(symbol);

            if (!result.Success)
            {
                _logger.LogError("Error while getting 24 hour price for {@symbol}, error={@Error}", symbol, result.Error);
                throw new Exception($"Unable to get 24 hour price for {symbol}, error={result.Error?.Message}");
            }

            return result.Data;
        }

        private BinanceClient GetClient()
        {
            _logger.LogDebug("Creating new Binance client");
            return new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(
                    _binanceUserConfig.ApiKey,
                    _binanceUserConfig.ApiSecret),
                BaseAddress = _binanceUserConfig.BaseAddress,
            });
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}

