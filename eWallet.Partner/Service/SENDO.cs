using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Partner.Service
{
    public class SENDO : IServiceProvider
    {
        private Data.MongoHelper _helper = null;
        private const string provider = "SENDO";
        public SENDO()
        {
            _helper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_business", "ewallet_business");
        }
        Random rd = new Random();
        public dynamic payment_check_bill(string service, string bill_code)
        {
             dynamic bill = _helper.Get("billing_info",
                 Query.And(
                  Query.EQ("service.code", service),
                 Query.EQ("provider.code", provider),
                 Query.EQ("code", bill_code)
                 )
               );
             if (bill == null)
             {
                 bill = new Data.DynamicObj();
                 bill._id = Guid.NewGuid().ToString();
                 bill.code = bill_code;
                 bill.customer_id = provider + bill_code;
                 bill.customer_name = "NGUYEN TRAN QUANG ANH";
                 bill.provider = new Data.DynamicObj();
                 bill.provider.code = provider;
                 bill.provider.name = "Mạng thương mại điện tử SENDO";
                 bill.service = new Data.DynamicObj();
                 bill.service.code = service;
                 bill.service.name = "Hóa đơn Thương mại điện tử";
                 bill.amount = 1000 * rd.Next(1000);
                 bill.status = "NEW";
                 _helper.Save("billing_info", bill);
             }
            return bill;
        }
    }
}
