using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Partner.Service
{
    public class ServiceProvider
    {
        public static IServiceProvider GetProvider(string code)
        {
            switch (code)
            {
                case "ESSE_EVNHN":
                    return new EVN();
                case "ECOM_SENDO":
                    return new SENDO();
                case "ECOM_FPTS":
                    return new FPTS();
                default:
                    return null;
            }
        }
    }
    public interface IServiceProvider
    {
        dynamic payment_check_bill(string service, string bill_code);
    }
}
