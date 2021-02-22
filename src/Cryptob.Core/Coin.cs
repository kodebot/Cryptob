
using System;

namespace Cryptob.Core
{
    public class Coin : StringEnum<Coin>
    {
        public static Coin BTC = Create(nameof(BTC));
        public static Coin USDT = Create(nameof(USDT));

        public static decimal Round(decimal value, Coin coin)
        {
            if (coin == BTC)
            {
                return Decimal.Round( value, 7);
            }

            if (coin == USDT)
            {
                return decimal.Round( value, 2);
            }

            throw new NotImplementedException();
        }
    }
}
