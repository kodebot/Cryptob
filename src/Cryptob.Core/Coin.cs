
namespace Cryptob.Core
{
    public class Coin : StringEnum<Coin>
    {
        public static Coin BTC = Create(nameof(BTC));
        public static Coin USDT = Create(nameof(USDT));
    }
}
