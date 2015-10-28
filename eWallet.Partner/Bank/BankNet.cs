using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Partner.Bank
{
    public class BankNet
    {
        public static dynamic config;
        public string SendOrder(string service_code, string trans_id, string product_code, string amount, 
            string shipping_fee, string tax, string bank_code, string confirm_url)
        {
            string trans_key = String.Concat(trans_id, config.merchant_code, product_code, amount, shipping_fee, tax, config.merchant_key);
            trans_key = eWallet.Common.Security.GenMd5Hash(trans_key);
            BankNetGW.PaymentGatewayPortTypeClient client = new BankNetGW.PaymentGatewayPortTypeClient();
            return client.Send_GoodInfo_Ext2(trans_id, config.merchant_code, config.country_code,product_code,string.Empty,
                amount, shipping_fee, tax, config.url_success + "?" + confirm_url, config.url_fail + "?" + confirm_url, trans_key, bank_code, "720");
        }
    }
}
