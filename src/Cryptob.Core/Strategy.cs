
namespace Cryptob.Core
{
    public class Strategy : StringEnum<Strategy>
    {
        public static Strategy PriceBurst = Create(nameof(PriceBurst));
        public static Strategy MarketMaker = Create(nameof(MarketMaker));
        public static Strategy SlowFollower = Create(nameof(SlowFollower));
        public static Strategy BuyDip = Create(nameof(BuyDip));
    }
}
