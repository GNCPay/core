using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Topup
{
    public interface IProvider
    {
        dynamic Topup(string provider, string account_number, int amount, string ref_id);
    }

    public static class ProviderFactory
    {
        public static IProvider GetProvider(string provider_code)
        {
            provider_code = provider_code.ToLower();
            switch (provider_code)
            {
                case "xpay":
                    return new xpay();
                default: return null;
            }
        }
    }
}
