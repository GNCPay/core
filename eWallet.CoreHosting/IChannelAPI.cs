using eWallet.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.CoreHosting
{
    [ServiceContract]
    interface IChannelAPI
    {
        //[OperationContract]
        //string Process(string Request);

        [OperationContract]
        string Process(string Request);

        [OperationContract]
        string Echo();
    }

     [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChannelAPI : IChannelAPI
    {
         private eWallet.Data.MongoHelper _data = null;
         public ChannelAPI()
         {
         }
        public string Echo()
        {
            return DateTime.Now.ToString();
        }

         private dynamic _channel;

         public ChannelAPI(dynamic channel)
        {
            try
            {
                _channel = channel;
                if (_data == null) Start(channel);
            }
            catch (Exception ex)
            { throw ex; }
        }

         private void Start(dynamic channel)
         {
             //profile = new Business.ProileServices();
             //billing = new Business.BillingServices();
             //finance = new Business.FinanceServices();
             _data = new Data.MongoHelper(
                    System.Configuration.ConfigurationManager.AppSettings["CORE_DB_SERVER"],
                    System.Configuration.ConfigurationManager.AppSettings["CORE_DB_DATABASE"]
                    );

             BusinessFactory.AddBusinessModule("profile");
             BusinessFactory.AddBusinessModule("finance");
             BusinessFactory.AddBusinessModule("security");
             BusinessFactory.AddBusinessModule("transaction");
             BusinessFactory.AddBusinessModule("sms");
             BusinessFactory.AddBusinessModule("billing");
         }


         public string Process(string Request)
         {
             try
             {
                 dynamic _dynamicRequest = new eWallet.Data.DynamicObj(Request);
                 _dynamicRequest._id = Guid.NewGuid().ToString();
                 _dynamicRequest.status = "NEW";
                 _data.Insert("core_request", _dynamicRequest);
                 string module = _dynamicRequest.module.ToString();
                 if (_dynamicRequest.type == "two_way")
                 {
                     return Business.BusinessFactory.GetBusiness(module).GetResponse(_dynamicRequest._id).ToString();
                 }
                 else {
                     _dynamicRequest.error_code = "00";
                     _dynamicRequest.error_message = "Success";
                     return _dynamicRequest;
                 }
             }
             catch
             {
                 return String.Empty;
             }
         }

         private string process_frontend_web(dynamic _dynamicRequest)
         {
             return String.Empty;
             //string function_code = _dynamicRequest.function;
             //switch (function_code)
             //{
             //    case "payment_check_bill":
             //        Partner.Service.IServiceProvider provider = Partner.Service.ServiceProvider.GetProvider(_dynamicRequest.provider_code);
             //        if (provider != null)
             //            return provider.payment_check_bill(_dynamicRequest).ToString();
             //        break;
             //    case "profile_register":
             //        return profile.Register(_dynamicRequest).ToString();
             //    case "profile_login":
             //        return profile.Login(_dynamicRequest).ToString();
             //    case "profile_get":
             //        return profile.Get(_dynamicRequest).ToString();
             //    case "payment_make_billing_invoice":
             //        return billing.MakeInvoice(_dynamicRequest).ToString();
             //    case "payment_make_billing_transaction":
             //        return billing.MakeTransaction(_dynamicRequest).ToString();
             //    case "payment_update_billing_transaction":
             //        return billing.UpdateTransaction(_dynamicRequest).ToString();

             //    case "payment_billing_transaction_history":
             //        return billing.ListTransactions(_dynamicRequest).ToString();

             //    case "payment_make_request":
             //        return billing.MakeRequest(_dynamicRequest).ToString();
             //    case "payment_list_request":
             //        return billing.ListRequest(_dynamicRequest).ToString();
             //    case "payment_get_request":
             //        return billing.GetRequest(_dynamicRequest).ToString();

             //    case "payment_list_assign_to_request":
             //        return billing.ListRequest(_dynamicRequest).ToString();
             //    case "payment_list_alert_request":
             //        return billing.ListRequest(_dynamicRequest).ToString();
                 
             //    case "finance_open_account":
             //        return finance.OpenAccount(_dynamicRequest).ToString();
             //    default:
             //        break;
             //}
             //return _dynamicRequest.ToString();
         }
    }
}
