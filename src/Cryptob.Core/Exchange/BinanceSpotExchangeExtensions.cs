using System;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;

namespace Cryptob.Core.Exchange
{
    public static class BinanceSpotExchangeExtensions
    {
        public static Task<WebCallResult<BinancePlacedOrder>> PlaceTestOrLiveOrderAsync(
            this IBinanceClient client,
            bool test,
            string symbol,
            OrderSide side,
            OrderType type,
            Decimal? quantity = null,
            Decimal? quoteOrderQuantity = null,
            string? newClientOrderId = null,
            Decimal? price = null,
            TimeInForce? timeInForce = null,
            Decimal? stopPrice = null,
            Decimal? icebergQty = null,
            OrderResponseType? orderResponseType = null,
            int? receiveWindow = null,
            CancellationToken ct = default(CancellationToken))
        {
            var spotOrderClient = client.Spot.Order;
            return test
                ? spotOrderClient.PlaceTestOrderAsync(
                    symbol,
                    side,
                    type,
                    quantity,
                    quoteOrderQuantity,
                    newClientOrderId,
                    price,
                    timeInForce,
                    stopPrice,
                    icebergQty,
                    orderResponseType,
                    receiveWindow,
                    ct)
                : spotOrderClient.PlaceOrderAsync(
                    symbol,
                    side,
                    type,
                    quantity,
                    quoteOrderQuantity,
                    newClientOrderId,
                    price,
                    timeInForce,
                    stopPrice,
                    icebergQty,
                    orderResponseType,
                    receiveWindow,
                    ct);
        }
    }
}