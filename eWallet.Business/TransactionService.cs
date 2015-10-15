using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class TransactionService : BaseBusiness
    {
        public TransactionService()
        {
            Processing.Transaction.DataHelper = new Data.MongoHelper(
                   System.Configuration.ConfigurationSettings.AppSettings["TRANSACTION_DB_SERVER"],
                   System.Configuration.ConfigurationSettings.AppSettings["TRANSACTION_DB_DATABASE"]
                   );
        }
        public override Data.DynamicObj Process(Data.DynamicObj request)
        {
            dynamic request_message = request;
            string _func = request_message.function;
            _func = _func.ToLower();
            switch (_func)
            {
                case "topup":
                    request_message = TopUp(request_message);
                    break;
                case "cashout":
                    request_message = CashOut(request_message);
                    break;
                case "cashin":
                    request_message = CashIn(request_message);
                    break;
                case "deposit":
                    request_message = Deposit(request_message);
                    break;
                case "withdraw":
                    request_message = Withdraw(request_message);
                    break;
                case "confirm":
                    request_message = Confirm(request_message);
                    break;
                case "operation_confirm":
                    request_message = OperationConfirm(request_message);
                    break;
                case "cancel":
                    request_message = Cancel(request_message);
                    break;
                case "transfer":
                    request_message = Transfer(request_message);
                    break;
                case "list_transaction":
                    request_message = ListTransaction(request_message);
                    break;
                case "purchase":
                    request_message = Purchase(request_message);
                    break;
                case "payment":
                    request_message = Payment(request_message);
                    break;
                case "confirm_otp":
                    request_message = ConfirmOtp(request_message);
                    break;
                case "request":
                    request_message = MakeRequest(request_message);
                    break;
                case "list_request":
                    request_message = ListRequest(request_message);
                    break;
                case "make_pre_transaction":
                    request_message = MakeBill(request_message);
                    break;
                case "get_pre_transaction":
                    request_message = GetDetail(request_message);
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

        private dynamic TopUp(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "TOPUP";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.ref_id.ToString();
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.service = request.service;
            tran_info.provider = request.provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.note = "NỘP {0} VNĐ VÀO TÀI KHOẢN {1}, NHÀ CUNG CẤP {2}, DỊCH VỤ {3}";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"), request.ref_id, request.provider, request.service);

            
            ///
            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "TOPUP";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
            request_finance.request.profiles = new long[] { profile._id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.service = tran_info.service;
            request_finance.request.provider = tran_info.provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);
            request_message.error_code = response_finance.error_code;
            request_message.error_message = response_finance.error_message;

            if (response_finance.error_code != "00")
            {
                tran_info.error_code = response_finance.error_code;
                tran_info.error_message = response_finance.error_message;
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                return request_message;
            }
            ///

            if (request.payment_provider == "BANKNET")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
                string _url_return = "transaction_type=DEPOSIT&channel=" + tran_info.channel + "&trans_id=" + tran_info._id + "&amount=" + tran_info.amount;
                string _seq = DateTime.Today.Year + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
                tran_info.bank_ref_id = Processing.Transaction.DataHelper.GetNextSquence("bank_ref_" + _seq).ToString().PadLeft(6, '0');
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info.bank_ref_id,
                    request.payment_provider + "_" + tran_info.created_by,
                    tran_info.amount.ToString(),
                    "0",
                    "0",
                    request.bank,
                    _url_return
                    );
                string[] url_params = banknet_response.Split('|');
                if (url_params[0] == "010")
                {
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    response.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    request_message.response = response;
                    request_message.error_code = "00";
                    request_message.error_message = "Khoi tao giao dich thanh cong";
                    tran_info.status = "WAITING";
                    tran_info.partner_trans_id = response.url_redirect.Split('=')[1];
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }
                else
                {
                    response.url_redirect = "";
                    request_message.error_code = url_params[0];
                    request_message.error_message = "Khoi tao giao dich khong thanh cong";
                    tran_info.error_code = request_message.error_code;
                    tran_info.error_message = request_message.error_message;
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }

            }

            else
            {
                tran_info.status = "WAITING";
                tran_info.error_code = request_message.error_code;
                tran_info.error_message = request_message.error_message;
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
                response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
                request_message.response = response;
            }
            //dynamic confirm_result = ConfirmTopup(tran_info);

            return request_message;
        }

        private dynamic CashOut(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "CASHOUT";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.profile.ToString();
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.service = request.service;
            tran_info.provider = request.provider;
            tran_info.amount = request.amount;
            tran_info.detail = request.receiver;
            tran_info.detail.note = request.note;
            tran_info.note = "RÚT {0} VNĐ VÀO TÀI KHOẢN {1}, {2}, NH {3}";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"), request.receiver.account_number, request.receiver.account_name, request.receiver.account_bank);

            

            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "cashout";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
            request_finance.request.profiles = new long[] { profile._id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.service = tran_info.service;
            request_finance.request.provider = tran_info.provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

            if (response_finance.error_code != "00")
            {
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                request_message.error_code = response_finance.error_code;
                request_message.error_message = response_finance.error_message;
                return request_message;
            }
            ///
            tran_info.status = "WAITING";
            Processing.Transaction.DataHelper.Insert("transactions", tran_info);
            //dynamic confirm_result = ConfirmTopup(tran_info);
            response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
            response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
            response.trans_id = tran_info._id;
            request_message.error_code = response_finance.error_code;
            request_message.error_message = response_finance.error_message;
            request_message.response = response;

            return request_message;
        }

        private dynamic CashIn(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "CASHIN";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.profile.ToString();
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.service = request.service;
            tran_info.provider = request.provider;
            tran_info.amount = request.amount;
            tran_info.detail = (request.sender == null)? new Data.DynamicObj():request.sender;
            tran_info.detail.note = request.note;
            tran_info.note = "NỘP {0} VNĐ VÀO TÀI KHOẢN VÍ";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"));

            dynamic profile = Business.Processing.Profile.Get(request.profile.ToString());
            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "cashin";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            request_finance.request.profiles = new long[] { profile._id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.service = tran_info.service;
            request_finance.request.provider = tran_info.provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

            if (response_finance.error_code != "00")
            {
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                request_message.error_code = response_finance.error_code;
                request_message.error_message = response_finance.error_message;
                return request_message;
            }
            ///

            if (request.payment_provider == "BANKNET")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
                string _url_return = "transaction_type=" + tran_info.transaction_type + "&channel=" + tran_info.channel + "&trans_id=" + tran_info._id + "&amount=" + tran_info.amount;
                string _seq = DateTime.Today.Year + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
                tran_info.bank_ref_id = Processing.Transaction.DataHelper.GetNextSquence("bank_ref_" + _seq).ToString().PadLeft(6, '0');
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info.bank_ref_id,
                    request.payment_provider + "_" + profile._id,
                    tran_info.amount.ToString(),
                    "0",
                    "0",
                    request.bank,
                    _url_return
                    );
                string[] url_params = banknet_response.Split('|');
                if (url_params[0] == "010")
                {
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    response.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    request_message.response = response;
                    request_message.error_code = "00";
                    request_message.error_message = "Khoi tao giao dich thanh cong";
                    tran_info.status = "WAITING";
                    tran_info.partner_trans_id = response.url_redirect.Split('=')[1];
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }
                else
                {
                    response.url_redirect = "";
                    request_message.error_code = "96";
                    request_message.error_message = "Khoi tao giao dich khong thanh cong";
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }

            }

            else
            {
                tran_info.status = "WAITING";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
                response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
                response.trans_id = tran_info._id;
                request_message.response = response;
            }
            //dynamic confirm_result = ConfirmTopup(tran_info);
            request_message.error_code = response_finance.error_code;
            request_message.error_message = response_finance.error_message;
            return request_message;
        }

        private dynamic ListRequest(dynamic request_message)
        {
            dynamic resp = new Data.DynamicObj();
            resp.error_code = "00";
            resp.error_message = "Khoi tao giao dich thanh cong";
            resp.response = Processing.Billing.ListRequest(request_message.request);
            return resp;
        }

        private dynamic MakeRequest(dynamic request_message)
        {
            string prefix = DateTime.Today.ToString("yy") + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
            dynamic request = request_message.request;
            request._id = prefix + Processing.Transaction.DataHelper.GetNextSquence("payment_request_" + prefix).ToString().PadLeft(6, '0');
            request.status = "NEW";

            Processing.Transaction.DataHelper.Insert("payment_request", request);
            request_message.error_code = "00";
            request_message.error_message = "Tao yeu cau thanh cong!";

            return request_message;
        }

        private dynamic Payment(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "PAYMENT";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.request_id = request_message._id;
            tran_info.ref_id = request.product_code;
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.service = request.service;
            tran_info.provider = request.provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.note = "THANH TOÁN DỊCH VỤ {0}, NHÀ CUNG CẤP {1}, ĐƠN HÀNG {2}, SỐ TIỀN {3} VNĐ";
            tran_info.note = String.Format(tran_info.note, request.service, request.provider, request.product_code, request.amount.ToString("N0"));
            response.trans_id = tran_info._id;
            response.amount = request.amount;

            if (request.payment_provider == "BANKNET")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
                string _url_return = "transaction_type=PAYMENT&channel=" + tran_info.channel + "&trans_id=" + tran_info._id + "&amount=" + tran_info.amount;
                string _seq = DateTime.Today.Year + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
                tran_info.bank_ref_id = Processing.Transaction.DataHelper.GetNextSquence("bank_ref_" + _seq).ToString().PadLeft(6, '0');
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info.bank_ref_id,
                    request.payment_provider + "_" + tran_info.created_by,
                    tran_info.amount.ToString(),
                    "0",
                    "0",
                    request.bank,
                    _url_return
                    );
                string[] url_params = banknet_response.Split('|');
                tran_info.service_provider_response = banknet_response;
                if (url_params[0] == "010")
                {
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    response.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    request_message.response = response;
                    request_message.error_code = "00";
                    request_message.error_message = "Khoi tao giao dich thanh cong";
                    tran_info.status = "WAITING";
                    tran_info.partner_trans_id = response.url_redirect.Split('=')[1];
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }
                else
                {

                    request_message.error_code = "96";
                    request_message.error_message = "Khoi tao giao dich khong thanh cong";
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }

            }

            else
            {
                /// Block tai khoan o day
                dynamic request_finance = new Data.DynamicObj();
                request_finance._id = Guid.NewGuid().ToString();
                request_finance.system = "core_transaction";
                request_finance.module = "finance";
                request_finance.function = "payment";
                request_finance.type = "two_way";
                request_finance.request = new Data.DynamicObj();
                dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
                request_finance.request.profiles = new long[] { profile._id };
                request_finance.request.amount = tran_info.amount;
                request_finance.request.business_transaction = tran_info._id;
                request_finance.request.channel = tran_info.channel;
                request_finance.request.service = tran_info.service;
                request_finance.request.provider = tran_info.provider;
                request_finance.status = "NEW";
                data.Insert("core_request", request_finance);

                dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

                if (response_finance.error_code != "00")
                {
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    request_message.error_code = response_finance.error_code;
                    request_message.error_message = response_finance.error_message;
                    return request_message;
                }
                response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
                response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
                request_message.response = response;
                request_message.response = response;
                request_message.error_code = "00";
                request_message.error_message = "Khoi tao giao dich thanh cong";

                tran_info.status = "WAITING";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);

            }
            //dynamic confirm_result = ConfirmTopup(tran_info);
            request_message.error_code = "00";
            request_message.error_message = "Giao dich thanh cong";
            return request_message;
        }

        private dynamic Cancel(dynamic request_message)
        {
            dynamic request = request_message.request;
            string service = request.transaction_type;
            service = service.ToLower();
            dynamic tran_info = Processing.Transaction.DataHelper.Get("transactions",
                        Query.EQ("_id", request.trans_id)
                        );
            if (tran_info == null || tran_info.amount != request.amount)
            {
                request_message.error_code = "90";
                request_message.error_message = "Invalid transaction";
            }
            dynamic response = CancelTransaction(tran_info);
            request_message.error_code = response.error_code;
            request_message.error_message = response.error_message;

            return request_message;
        }

        private dynamic CancelTransaction(dynamic tran_info)
        {
            dynamic request_message = new Data.DynamicObj();
            //dynamic trans = Processing.Transaction.MakeTopup(request);
            dynamic finance_transaction = new Data.DynamicObj();
            finance_transaction._id = Guid.NewGuid().ToString();
            finance_transaction.system = "core_transaction";
            finance_transaction.module = "finance";
            finance_transaction.function = "cancel_transaction";
            finance_transaction.type = "two_way";
            finance_transaction.request = new Data.DynamicObj();
            finance_transaction.request.trans_id = tran_info._id;
            finance_transaction.status = "NEW";
            data.Insert("core_request", finance_transaction);

            dynamic finance_transaction_result = Business.BusinessFactory.GetBusiness("finance").GetResponse(finance_transaction._id);
            if (finance_transaction_result == null)
            {
                request_message.error_code = "96";
                request_message.error_message = "System Error. Please try again late!";
            }
            else
            {
                request_message.error_code = finance_transaction_result.error_code;
                request_message.error_message = finance_transaction_result.error_message;
            }
            tran_info.status = (request_message.error_code == "00") ? "CANCELED" : "ERROR";
            tran_info.error_message = request_message.error_message;
            Processing.Transaction.DataHelper.Save("transactions", tran_info);
            return request_message;
        }

        private dynamic Purchase(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "PURCHASE";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.ref_id;
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.product = request.product;
            tran_info.note = "MUA {0}, SỐ TIỀN {1} VNĐ";
            tran_info.note = String.Format(tran_info.note, request.product, request.amount.ToString("N0"));
            tran_info.note = tran_info.note.ToUpper();
            response.trans_id = tran_info._id;
            response.amount = request.amount;

            if (request.payment_provider == "BANKNET")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
                string _url_return = "transaction_type=PURCHASE&channel=" + tran_info.channel + "&trans_id=" + tran_info._id + "&amount=" + tran_info.amount;
                string _seq = DateTime.Today.Year + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
                tran_info.bank_ref_id = Processing.Transaction.DataHelper.GetNextSquence("bank_ref_" + _seq).ToString().PadLeft(6, '0');
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info.bank_ref_id,
                    request.payment_provider + "_" + tran_info.created_by,
                    tran_info.amount.ToString(),
                    "0",
                    "0",
                    request.bank,
                    _url_return
                    );
                string[] url_params = banknet_response.Split('|');
                if (url_params[0] == "010")
                {
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    response.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    request_message.response = response;
                    request_message.error_code = "00";
                    request_message.error_message = "Khoi tao giao dich thanh cong";
                    tran_info.status = "WAITING";
                    tran_info.partner_trans_id = response.url_redirect.Split('=')[1];
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }
                else
                {
                    request_message.error_code = "96";
                    request_message.error_message = "Khoi tao giao dich khong thanh cong";
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }

            }

            else
            {
                /// Block tai khoan o day
                dynamic request_finance = new Data.DynamicObj();
                request_finance._id = Guid.NewGuid().ToString();
                request_finance.system = "core_transaction";
                request_finance.module = "finance";
                request_finance.function = "purchase";
                request_finance.type = "two_way";
                request_finance.request = new Data.DynamicObj();
                dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
                request_finance.request.profiles = new long[] { profile._id };
                request_finance.request.amount = tran_info.amount;
                request_finance.request.business_transaction = tran_info._id;
                request_finance.request.channel = tran_info.channel;
                request_finance.request.product = tran_info.product;
                request_finance.status = "NEW";
                data.Insert("core_request", request_finance);

                dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

                if (response_finance.error_code != "00")
                {
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    request_message.error_code = response_finance.error_code;
                    request_message.error_message = response_finance.error_message;
                    return request_message;
                }
                response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
                response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
                request_message.response = response;
                request_message.error_code = "00";
                request_message.error_message = "Khoi tao giao dich thanh cong";

                tran_info.status = "WAITING";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);

            }
            //dynamic confirm_result = ConfirmTopup(tran_info);
            request_message.error_code = "00";
            request_message.error_message = "Giao dich thanh cong";
            return request_message;
        }

        private dynamic ListTransaction(dynamic request_message)
        {
            dynamic request = request_message.request;
            var profile = request.profile;
            var page = request.page;
            request_message.response = Processing.Transaction.List(profile, page);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Withdraw(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "WITHDRAW";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.receiver.account_number;
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.note = "RÚT {0} VNĐ VÀO TÀI KHOẢN {1}, {2}, NH {3}";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"), request.receiver.account_number, request.receiver.account_name, request.receiver.account_bank);
            ///
            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "withdraw";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
            request_finance.request.profiles = new long[] { profile._id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.product = tran_info.payment_provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

            if (response_finance.error_code != "00")
            {
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                request_message.error_code = response_finance.error_code;
                request_message.error_message = response_finance.error_message;
                return request_message;
            }
            ///
            tran_info.status = "WAITING";
            Processing.Transaction.DataHelper.Insert("transactions", tran_info);
            //dynamic confirm_result = ConfirmTopup(tran_info);
            response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
            response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);
            request_message.error_code = response_finance.error_code;
            request_message.error_message = response_finance.error_message;
            request_message.response = response;

            return request_message;
        }

        private dynamic Deposit(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "DEPOSIT";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.profile.ToString();
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.note = "NỘP {0} VNĐ VÀO TÀI KHOẢN";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"));
            ///
            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "deposit";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
            request_finance.request.profiles = new long[] { profile._id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.product = tran_info.payment_provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

            if (response_finance.error_code != "00")
            {
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                request_message.error_code = response_finance.error_code;
                request_message.error_message = response_finance.error_message;
                return request_message;
            }
            ///

            if (request.payment_provider == "BANKNET")
            {
                Partner.Bank.BankNet bankNet = new Partner.Bank.BankNet();
                string _url_return = "transaction_type=DEPOSIT&channel=" + tran_info.channel + "&trans_id=" + tran_info._id + "&amount=" + tran_info.amount;
                string _seq = DateTime.Today.Year + DateTime.Today.DayOfYear.ToString().PadLeft(3, '0');
                tran_info.bank_ref_id = Processing.Transaction.DataHelper.GetNextSquence("bank_ref_" + _seq).ToString().PadLeft(6, '0');
                string banknet_response = bankNet.SendOrder(
                    tran_info.service,
                    tran_info.bank_ref_id,
                    request.payment_provider + "_" + tran_info.created_by,
                    tran_info.amount.ToString(),
                    "0",
                    "0",
                    request.bank,
                    _url_return
                    );
                string[] url_params = banknet_response.Split('|');
                if (url_params[0] == "010")
                {
                    url_params[2] = url_params[2].Substring(0, int.Parse(url_params[1]));
                    response.url_redirect = url_params[2].Substring(0, int.Parse(url_params[1]));
                    request_message.response = response;
                    request_message.error_code = "00";
                    request_message.error_message = "Khoi tao giao dich thanh cong";
                    tran_info.status = "WAITING";
                    tran_info.partner_trans_id = response.url_redirect.Split('=')[1];
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }
                else
                {
                    response.url_redirect = "";
                    request_message.error_code = "96";
                    request_message.error_message = "Khoi tao giao dich khong thanh cong";
                    tran_info.status = "ERROR";
                    Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                    return request_message;
                }

            }

            else
            {
                tran_info.status = "WAITING";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);

            }
            //dynamic confirm_result = ConfirmTopup(tran_info);
            request_message.error_code = response_finance.error_code;
            request_message.error_message = response_finance.error_message;
            return request_message;
        }

        private dynamic GetDetail(dynamic request_message)
        {
            string _id = request_message.request.id;
            dynamic trans = Processing.Transaction.DataHelper.Get("pre_transactions", Query.EQ("_id", _id));
            if (trans == null)
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Transaction";
            }
            else
            {
                request_message.error_code = "00";
                request_message.error_message = "Success";
                request_message.response = trans;
            }
            return request_message;
        }

        private dynamic MakeBill(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();
            dynamic service_provider = Processing.Transaction.DataHelper.Get("service_provider", Query.EQ("_id", "PAY_BILL"));
            if (service_provider == null)
            {
                request_message.error_code = "02";
                request_message.error_message = "Unsupported or invalid service";
                return request_message;
            }
            dynamic provider = null;
            foreach (dynamic pvd in service_provider.providers)
                if (pvd._id == request.provider)
                {
                    provider = pvd;
                    break;
                }
            if (provider == null)
            {
                request_message.error_code = "02";
                request_message.error_message = "Unsupported or invalid provider";
                return request_message;
            }
            dynamic tran_info = new Data.DynamicObj();
            tran_info.service = "PAY_BILL";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id;
            string ref_id = request.billing.code;
            //ref_id = ref_id.Substring(ref_id.Length - 6, 6);
            tran_info.ref_id = ref_id;
            tran_info.created_by = request.user_id;
            tran_info.provider = provider;
            tran_info.service = new Data.DynamicObj();
            tran_info.service._id = service_provider._id;
            tran_info.service.name = service_provider.name;

            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.method = request.method;
            tran_info.status = "WAITING";

            //Check han muc o day

            Processing.Transaction.DataHelper.Save("pre_transactions", tran_info);
            request_message.error_code = "00";
            request_message.error_message = "Khoi tao giao dich thanh cong";
            request_message.response = tran_info;
            return request_message;
        }

        /// <summary>
        /// Xac nhan giao dich voi OTP
        /// </summary>
        /// <param name="request_message"></param>
        /// <returns></returns>
        private dynamic ConfirmOtp(dynamic request_message)
        {
            dynamic request = request_message.request;
            request_message.response = new Data.DynamicObj();
            string otp = request.otp;
            /// Block tai khoan o day
            dynamic request_otp = new Data.DynamicObj();
            request_otp._id = Guid.NewGuid().ToString();
            request_otp.system = "core_transaction";
            request_otp.module = "security";
            request_otp.function = "verify_otp";
            request_otp.type = "two_way";
            request_otp.request = new Data.DynamicObj();
            request_otp.request.user_id = request.user_id.ToString();
            request_otp.request.otp = request.otp;
            request_otp.status = "NEW";
            data.Insert("core_request", request_otp);

            dynamic response_otp = BusinessFactory.GetBusiness("security").GetResponse(request_otp._id);
            if (response_otp.error_code == "00")
            {
                return Confirm(request_message);

            }
            else
            {
                return response_otp;
            }
        }

        private dynamic ConfirmPayBill(dynamic _lastestTrans)
        {
            dynamic request_otp = new Data.DynamicObj();
            request_otp._id = Guid.NewGuid().ToString();
            request_otp.system = "core_transaction";
            request_otp.module = "billing";
            request_otp.function = "pay_bill";
            request_otp.type = "two_way";
            request_otp.request = new Data.DynamicObj();
            request_otp.request.service = _lastestTrans.service;
            request_otp.request.provider = _lastestTrans.provider;
            request_otp.request.bill_code = _lastestTrans.ref_id;
            request_otp.request.amount = _lastestTrans.amount;
            request_otp.request.ref_id = _lastestTrans._id;
            request_otp.status = "NEW";
            data.Insert("core_request", request_otp);

            return BusinessFactory.GetBusiness("billing").GetResponse(request_otp._id);
        }

        private dynamic Transfer(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic response = new Data.DynamicObj();

            dynamic tran_info = new Data.DynamicObj();
            tran_info.transaction_type = "TRANSFER";
            tran_info._id = Guid.NewGuid().ToString();// request.user_id
            tran_info.ref_id = request.receiver.user_name;//.profile.ToString();
            tran_info.created_by = request.profile;
            tran_info.channel = request.channel.ToUpper();
            tran_info.payment_provider = request.payment_provider;
            tran_info.service = request.service;
            tran_info.provider = request.provider;
            tran_info.amount = request.amount;
            tran_info.detail = request;
            tran_info.product = request.product;
            tran_info.note = "CHUYỂN KHOẢN {0} VNĐ CHO TÀI KHOẢN {1}, {2}";
            tran_info.note = String.Format(tran_info.note, request.amount.ToString("N0"), request.receiver.user_name, request.receiver.full_name);

            response.trans_id = tran_info._id;
            response.amount = request.amount;
            response.url_redirect = "";

            /// Block tai khoan o day
            dynamic request_finance = new Data.DynamicObj();
            request_finance._id = Guid.NewGuid().ToString();
            request_finance.system = "core_transaction";
            request_finance.module = "finance";
            request_finance.function = "transfer";
            request_finance.type = "two_way";
            request_finance.request = new Data.DynamicObj();
            dynamic profile = Processing.Transaction.DataHelper.Get("profile", Query.EQ("user_name", request.profile));
            request_finance.request.profiles = new long[] { profile._id, request.receiver.id };
            request_finance.request.amount = tran_info.amount;
            request_finance.request.business_transaction = tran_info._id;
            request_finance.request.channel = tran_info.channel;
            request_finance.request.service = tran_info.service;
            request_finance.request.provider = tran_info.provider;
            request_finance.status = "NEW";
            data.Insert("core_request", request_finance);

            dynamic response_finance = Business.BusinessFactory.GetBusiness("finance").GetResponse(request_finance._id);

            if (response_finance.error_code != "00")
            {
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
                request_message.error_code = response_finance.error_code;
                request_message.error_message = response_finance.error_message;
                request_message.response = response;
                return request_message;
            }
            response.url_redirect = "payment/confirm?transaction_type={0}&trans_id={1}&amount={2}";
            response.url_redirect = String.Format(response.url_redirect, tran_info.transaction_type, tran_info._id, tran_info.amount);

            request_message.response = response;
            request_message.error_code = "00";
            request_message.error_message = "Khoi tao giao dich thanh cong";

            tran_info.status = "WAITING";
            Processing.Transaction.DataHelper.Insert("transactions", tran_info);
            return request_message;
        }

        private dynamic OperationConfirm(dynamic request_message)
        {
            dynamic request = request_message.request;
            dynamic operation_request = Processing.Transaction.DataHelper.Get("operation_request", Query.EQ("_id", request.request_id));
            operation_request.confirm = new Data.DynamicObj();
            operation_request.confirm.type = request.confirm_type;
            operation_request.confirm.note = request.confirm_note;
            operation_request.confirm.confirm_by = request.user_id;
            

            if (operation_request.status != "NEW")
            {
                Processing.Transaction.DataHelper.Save("operation_request", operation_request);
                request_message.error_code = "90";
                request_message.error_message = "This request were processed. Not allow to confirm";
                return request_message;
            }

            
            
            dynamic tran_info = Processing.Transaction.DataHelper.Get("transactions",
                        Query.EQ("_id", operation_request.transaction_ref)
                        );

            if (tran_info.status != "PROCESSING")
            {
                request_message.error_code = "91";
                request_message.error_message = "Not allow to confirm this transaction";
                operation_request.status = "FAILED";
                operation_request.confirm.note += " (" + request_message.error_message + ")";
                Processing.Transaction.DataHelper.Save("operation_request", operation_request);
            }
            else
            {
                operation_request.status = request.confirm_type;
                dynamic confirm_response = OperationConfirmTransaction(request.confirm_type, tran_info);
                request_message.error_code = confirm_response.error_code;
                request_message.error_message = confirm_response.error_message;
                if (request_message.error_code != "00")
                {
                    operation_request.status = "FAILED";
                    operation_request.confirm.note += " (" + request_message.error_message + ")";
                }
                Processing.Transaction.DataHelper.Save("operation_request", operation_request);
            }
            return request_message;
        }

        private dynamic Confirm(dynamic request_message)
        {
            dynamic request = request_message.request;
            string service = request.transaction_type;
            service = service.ToLower();
            dynamic tran_info = Processing.Transaction.DataHelper.Get("transactions",
                        Query.EQ("_id", request.trans_id)
                        );
            if (tran_info == null || tran_info.amount != request.amount)
            {
                request_message.error_code = "90";
                request_message.error_message = "Invalid transaction";
            }
            else if (tran_info.status != "WAITING")
            {
                request_message.error_code = "91";
                request_message.error_message = "Giao dịch đã được xử lý";
            }
            else
            {
                dynamic confirm_response = ConfirmTransaction(tran_info);
                request_message.error_code = confirm_response.error_code;
                request_message.error_message = confirm_response.error_message;
            }
            //request_message.response = new Data.DynamicObj();
            //if(tran_info.provider=="BIGP")
            //{
            //    if (request_message.error_code == "00")
            //    {
            //        request_message.response.url_redirect = "";
            //    }
            //    else
            //        request_message.response.url_redirect = "";
            //}
            return request_message;
        }
        private dynamic OperationConfirmTransaction(string type, dynamic tran_info)
        {
            dynamic request_message = new Data.DynamicObj();
            if (type == "COMPLETED")
            {
                dynamic finance_transaction = new Data.DynamicObj();
                finance_transaction._id = Guid.NewGuid().ToString();
                finance_transaction.system = "core_transaction";
                finance_transaction.module = "finance";
                finance_transaction.function = "post_transaction";
                finance_transaction.type = "two_way";
                finance_transaction.request = new Data.DynamicObj();
                finance_transaction.request.trans_id = tran_info._id;
                finance_transaction.status = "NEW";
                data.Insert("core_request", finance_transaction);

                dynamic finance_transaction_result = Business.BusinessFactory.GetBusiness("finance").GetResponse(finance_transaction._id);
                if (finance_transaction_result == null)
                {
                    request_message.error_code = "96";
                    request_message.error_message = "System Error. Please try again late!";
                }
                else
                {
                    request_message.error_code = finance_transaction_result.error_code;
                    request_message.error_message = finance_transaction_result.error_message;
                }
                tran_info.status = (request_message.error_code == "00") ? "COMPLETED" : "ERROR";
            }
            else
            {
                dynamic finance_transaction = new Data.DynamicObj();
                finance_transaction._id = Guid.NewGuid().ToString();
                finance_transaction.system = "core_transaction";
                finance_transaction.module = "finance";
                finance_transaction.function = "cancel_transaction";
                finance_transaction.type = "two_way";
                finance_transaction.request = new Data.DynamicObj();
                finance_transaction.request.trans_id = tran_info._id;
                finance_transaction.status = "NEW";
                data.Insert("core_request", finance_transaction);

                dynamic finance_transaction_result = Business.BusinessFactory.GetBusiness("finance").GetResponse(finance_transaction._id);
                if (finance_transaction_result == null)
                {
                    request_message.error_code = "96";
                    request_message.error_message = "System Error. Please try again late!";
                }
                else
                {
                    request_message.error_code = finance_transaction_result.error_code;
                    request_message.error_message = finance_transaction_result.error_message;
                }
                tran_info.status = (request_message.error_code == "00") ? "CANCELED" : "ERROR";
            }
            
            tran_info.error_message = request_message.error_message;
            Processing.Transaction.DataHelper.Save("transactions", tran_info);
            return request_message;
        }
        private dynamic ConfirmTransaction(dynamic tran_info)
        {
            dynamic request_message = new Data.DynamicObj();
            //dynamic trans = Processing.Transaction.MakeTopup(request);
            string trans_type = tran_info.transaction_type.ToString().ToLower();
            dynamic confirm_type = new Data.DynamicObj();
            bool is_need_confirm_type = false;
            switch(trans_type)
            {
                case "payment":
                    confirm_type = ConfirmPayBill(tran_info);
                    is_need_confirm_type = true;
                    break;
                default:
                    break;
            }
            //switch (trans_type)
            //{
            //    case "topup":
            //        confirm_type = ConfirmTopup(tran_info);
            //        is_need_confirm_type = true;
            //        break;
            //    default:
            //        break;
            //}
            if (is_need_confirm_type && confirm_type.error_code != "00")
            {
                request_message.error_code = confirm_type.error_code;
                request_message.error_message = "System Error. Please try again late!";
                tran_info.status = "ERROR";
                tran_info.error_message ="SERVICE PROVIDER ERROR: " + request_message.error_message;
                Processing.Transaction.DataHelper.Save("transactions", tran_info);
                return request_message;
            }
            

            //Neu giao dich can ke toan thuc hien
            if (tran_info.payment_provider == "GNCA")
            {
                tran_info.status = "PROCESSING";
                dynamic gnc_finance_request = tran_info.detail;
                gnc_finance_request._id = Guid.NewGuid().ToString();
                gnc_finance_request.transaction_ref = tran_info._id;
                gnc_finance_request.type = tran_info.transaction_type;
                gnc_finance_request.profile = tran_info.created_by;
                gnc_finance_request.channel = tran_info.channel;
                gnc_finance_request.amount = tran_info.amount;
                gnc_finance_request.status = "NEW";
                Processing.Transaction.DataHelper.Insert("operation_request", gnc_finance_request);

                request_message.error_code = "00";
                request_message.error_message = "Confirm transaction successful!";
            }
            else
            {
                dynamic finance_transaction = new Data.DynamicObj();
                finance_transaction._id = Guid.NewGuid().ToString();
                finance_transaction.system = "core_transaction";
                finance_transaction.module = "finance";
                finance_transaction.function = "post_transaction";
                finance_transaction.type = "two_way";
                finance_transaction.request = new Data.DynamicObj();
                finance_transaction.request.trans_id = tran_info._id;
                finance_transaction.status = "NEW";
                data.Insert("core_request", finance_transaction);

                dynamic finance_transaction_result = Business.BusinessFactory.GetBusiness("finance").GetResponse(finance_transaction._id);
                if (finance_transaction_result == null)
                {
                    request_message.error_code = "96";
                    request_message.error_message = "System Error. Please try again late!";
                }
                else
                {
                    request_message.error_code = finance_transaction_result.error_code;
                    request_message.error_message = finance_transaction_result.error_message;
                }

                tran_info.status = (request_message.error_code == "00") ? "COMPLETED" : "ERROR";
                tran_info.error_message = request_message.error_message;
            }
            Processing.Transaction.DataHelper.Save("transactions", tran_info);
            return request_message;
        }

        private dynamic ConfirmTopup(dynamic tran_info)
        {
            dynamic request_message = new Data.DynamicObj();
            //dynamic trans = Processing.Transaction.MakeTopup(request);
            dynamic finance_transaction = new Data.DynamicObj();
            finance_transaction._id = Guid.NewGuid().ToString();
            finance_transaction.system = "core_transaction";
            finance_transaction.module = "finance";
            finance_transaction.function = "make_transaction";
            finance_transaction.type = "two_way";
            finance_transaction.request = new Data.DynamicObj();
            finance_transaction.request.accounts = tran_info.detail.user_id;
            finance_transaction.request.trans_code = (tran_info.method == "PREPAID") ? "10.02.00" : "10.01.00";
            finance_transaction.request.created_by = tran_info.created_by;
            finance_transaction.request.amount = tran_info.amount;
            finance_transaction.request.note = "TOPUP " + tran_info.detail.note;
            DateTime dt = DateTime.Now;
            finance_transaction.system_created_time = dt.ToString("yyyyMMddHHmmss");
            finance_transaction.system_created_date = dt.ToString("yyyyMMdd");
            finance_transaction.status = "NEW";
            data.Save("core_request", finance_transaction);

            dynamic finance_transaction_result = Business.BusinessFactory.GetBusiness("finance").GetResponse(finance_transaction._id);
            if (finance_transaction_result == null)
            {
                request_message.error_code = "96";
                request_message.error_message = "System Error. Please try again late!";
                tran_info.status = "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
            }
            else
            {
                request_message.error_code = finance_transaction_result.error_code;
                request_message.error_message = finance_transaction_result.error_message;
                tran_info.status = (request_message.error_code == "00") ? "DONE" : "ERROR";
                Processing.Transaction.DataHelper.Insert("transactions", tran_info);
            }
            return request_message;

        }
    }
}
