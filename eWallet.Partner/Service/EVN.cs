using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Partner.Service
{
    public class EVN : IServiceProvider
    {
        private Data.MongoHelper _helper = null;
        private string provider = "EVNHN";
        public EVN()
        {
            _helper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_core", "ewallet_core");
        }
        Random rd = new Random();


        public EVN(string provider)
        {
            this.provider = provider;
            _helper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_core", "ewallet_core");
        }
        
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
                bill.customer_id = "INVALID_" + bill_code;
                bill.customer_name = "";
                bill.provider = new Data.DynamicObj();
                bill.provider.code = provider;
                bill.provider.name = "";
                bill.service = new Data.DynamicObj();
                bill.service.code = service;
                bill.service.name = "";
                bill.amount = 0;
                bill.status = "INVALID";
            }
            return bill;
        }

        public dynamic payment_bill(string service, string bill_code, long amount, string ref_id)
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
                bill.customer_id = "INVALID_" + bill_code;
                bill.customer_name = "";
                bill.provider = new Data.DynamicObj();
                bill.provider.code = provider;
                bill.provider.name = "";
                bill.service = new Data.DynamicObj();
                bill.service.code = service;
                bill.service.name = "";
                bill.amount = 0;
                bill.status = "INVALID";
            }
            else
            {
                bill.status = "PAID";
                bill.payment_transaction_ref = ref_id;
                _helper.Save("billing_info",bill);
                return bill;
            }
            return bill;
        }
    }
}
