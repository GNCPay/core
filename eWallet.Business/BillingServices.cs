using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class BillingServices: BaseBusiness
    {
        public BillingServices()
        {
            Processing.Billing.DataHelper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_business", "ewallet_business");
        }
        public BillingServices(dynamic config)
        {
            Processing.Billing.DataHelper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_business", "ewallet_business");
        }
        public override Data.DynamicObj Process(Data.DynamicObj request)
        {
            dynamic request_message = request;


            string _func = request_message.function;
            switch (_func)
            {
                case "check_bill":
                    request_message = CheckBill(request_message);
                    break;
                default:
                    request_message.error_code = "01";
                    request_message.error_message = "Unsupported or invalid function";
                    break;
            }


            request_message.status = "PROCESSED";
            data.Save("core_request", request_message);

            return request_message;
        }

        private dynamic CheckBill(dynamic request_message)
        {
            dynamic request = request_message.request;
            Partner.Service.IServiceProvider partner = Partner.Service.ServiceProvider.GetProvider(request.provider);
            if (partner == null)
            {
                request_message.error_code = "02";
                request_message.error_message = "Unsupported or invalid provider";
                return request_message;
            }
            dynamic bill_info = partner.payment_check_bill(request.service, request.bill_code);
            request_message.response = bill_info;
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

       

        public dynamic UpdateTransaction(dynamic trans_info)
        {
            dynamic resp = new Data.DynamicObj();
            Processing.Billing.UpdateTransactionStatus(trans_info.tran_id, trans_info.pay_by, trans_info.status);
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            return resp;
        }

        public dynamic MakeRequest(dynamic request)
        {
            Processing.Billing.MakeRequest(request);
            dynamic resp = new Data.DynamicObj();
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            return resp;
        }

        public dynamic GetRequest(dynamic request)
        {
            
            dynamic resp = new Data.DynamicObj();
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            resp.request = Processing.Billing.GetRequest(request);
            return resp;
        }
        public dynamic ListRequest(dynamic request)
        {

            dynamic resp = new Data.DynamicObj();
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            resp.requests = Processing.Billing.ListRequest(request);
            return resp;
        }
        public dynamic ListTransactions(dynamic request)
        {
            dynamic resp = new Data.DynamicObj();
            resp.transactions = Processing.Billing.ListTransaction(request.pay_by, DateTime.Today.AddDays(30), DateTime.Today);
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            return resp;
        }

        public dynamic MakeInvoice(dynamic invoice)
        {
            return invoice;
        }

        public dynamic MakeTransaction(dynamic tran_info)
        {
            dynamic resp = new Data.DynamicObj();
            tran_info.shipping_fee = "0";
            tran_info.tax = "0";
            string _id = DateTime.Now.ToString("HHmmss");// new Guid().ToString("");
           
            resp.tran_id = _id;
            tran_info._id = _id;
            tran_info.provider = tran_info.bill_info.provider.code;
            tran_info.service = tran_info.bill_info.service.code;
            tran_info.amount = long.Parse(tran_info.bill_info.amount);
            tran_info.status = "NEW";
            Processing.Billing.SaveTransaction(tran_info);
            if (tran_info.payment_method == "card")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
               
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info._id,
                    tran_info.provider + "_" + tran_info.bill_info.code,
                    tran_info.bill_info.amount,
                    tran_info.shipping_fee,
                    tran_info.tax,
                    tran_info.payment_bank,
                    String.Empty
                    );
                string[] url_params = banknet_response.Split('|');
                if (url_params[0] == "010"){
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    resp.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    Processing.Billing.UpdateBankTransaction(_id, resp.url_redirect.Split('=')[1], "PROCESSING");
                    resp.error_code = "00";
                    resp.error_message = "Khoi tao giao dich thanh cong";
                    
                }
                else
                {
                    resp.error_code = "96";
                    resp.error_message = "Khoi tao giao dich khong thanh cong";
                }
                
            }
            else if (tran_info.payment_method == "account")
            {
                resp.error_code = "00";
                resp.error_message = "Khoi tao giao dich thanh cong";
                Processing.Billing.UpdateTransactionStatus(_id, tran_info.pay_by, "PAID");
            }
            else
            {
                resp.error_code = "99";
                resp.error_message = "Giao dich chua duoc ho tro";
            }
            return resp;
        }
    }
}
