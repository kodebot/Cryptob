namespace Cryptob.Core.Configuration.Strategies
{
    public class PriceBurstConfig
    {
        public string BaseName { get; set; }
        public Coin Base => Coin.Parse(BaseName);
        public string QuoteName { get; set; }
        public Coin Quote => Coin.Parse(QuoteName);
        public decimal TradeDifference { get; set; }
        public double TradeProfitPercentage { get; set; }
        public decimal TradeQuantity { get; set; }
    }
}
