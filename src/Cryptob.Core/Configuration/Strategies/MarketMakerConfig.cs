namespace Cryptob.Core.Configuration.Strategies
{
    public class MarketMakerConfig
    {
        public string BaseName { get; set; }
        public Coin Base => Coin.Parse(BaseName);
        public string QuoteName { get; set; }
        public Coin Quote => Coin.Parse(QuoteName);
        public decimal SpreadInQuoteCoin { get; set; }
        public decimal TradeAssetAllocationPercentage { get; set; }
        public int TickIntervalInSeconds { get; set; }
        public decimal TradeFixedSizeInQuote { get; set; }
        public int StopAfterTicks { get; set; }
        public bool Test { get; set; }
    }
}
