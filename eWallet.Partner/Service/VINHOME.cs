using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Partner.Service
{
    public class VINHOME : IServiceProvider
    {
        private Data.MongoHelper _helper = null;

        public VINHOME()
        {
            _helper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_core", "ewallet_core");
        }

        public dynamic payment_bill(string service, string bill_code, long amount, string ref_id)
        {
                dynamic bill = new Data.DynamicObj();
                bill._id = Guid.NewGuid().ToString();
                bill.code = bill_code;
                bill.customer_id = "BILL_" + bill_code;
                bill.customer_name = "";
                bill.provider = new Data.DynamicObj();
                bill.provider.code = "VINHOME";
                bill.provider.name = "VINHOME BILLING";
                bill.service = new Data.DynamicObj();
                bill.service.code = service;
                bill.service.name = "";
                bill.amount = amount;
                bill.status = "PAID";
                bill.payment_transaction_ref = ref_id;
                _helper.Save("billing_info", bill);
                return bill;
        }
        
        public dynamic payment_check_bill(string service, string bill_code)
        {
            throw new NotImplementedException();
        }
    }
}
