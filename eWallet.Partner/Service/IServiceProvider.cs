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
                case "EVNHCM":
                case "EVNHN":
                    return new EVN(code);
                case "SENDO":
                    return new SENDO();
                case "FPTS":
                    return new FPTS();
                case "VINHOME":
                    return new VINHOME();
                default:
                    return null;
            }
        }
    }
    public interface IServiceProvider
    {
        dynamic payment_check_bill(string service, string bill_code);
        dynamic payment_bill(string service, string bill_code, long amount, string ref_id);
    }
}
