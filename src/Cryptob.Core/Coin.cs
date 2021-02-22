
using System;

namespace Cryptob.Core
{
    public class Coin : StringEnum<Coin>
    {
        public static Coin BTC = Create(nameof(BTC));
        public static Coin USDT = Create(nameof(USDT));
        public static Coin ADA = Create(nameof(ADA));

        public static decimal Round(decimal value, Coin coin)
        {
            if (coin == BTC)
            {
                return Decimal.Round(value, 6);
            }

            if (coin == USDT)
            {
                return decimal.Round(value, 2); // 5 for ADA, 2 for BTC
            }

            if (coin == ADA)
            {
                return decimal.Round(value, 1);
            }

            throw new NotImplementedException();
        }
    }
}
