using MongoDB.Driver.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class FinanceServices : BaseBusiness
    {

        public override void Start()
        {
            dynamic[] coa = Processing.Account.DataHelper.List("finance_chart_of_account", null);
            foreach (dynamic c in coa)
            {
                Processing.Account.chart_of_account.Add(c._id, c);
            }

            dynamic[] tcfg = Processing.Account.DataHelper.List("finance_transaction_cfg", null);
            foreach (dynamic t in tcfg)
            {
                string _id = String.Join("_", t.transaction_type, t.service, t.provider);
                Processing.Account.transaction_cfg.Add(_id, t);
            }
            base.Start();
        }


        public override Data.DynamicObj Process(Data.DynamicObj request)
        {
            dynamic request_message = request;


            string _func = request_message.function;
            _func = _func.ToLower();
            switch (_func)
            {
                case "topup":
                    request_message = Authorization(_func, request_message);
                    break;
                case "cashin":
                    request_message = Authorization(_func,request_message);
                    break;
                case "cashout":
                    request_message = Authorization(_func, request_message);
                    break;
                case "deposit":
                    request_message = Deposit(request_message);
                    break;
                case "withdraw":
                    request_message = Withdraw(request_message);
                    break;
                case "purchase":
                    request_message = Purchase(request_message);
                    break;
                case "payment":
                    request_message = Authorization(_func,request_message);
                    break;
                case "transfer":
                    request_message = Authorization(_func, request_message);
                    break;
                case "open_account":
                    request_message = OpenAccount(request_message);
                    break;
                case "list_account_profile":
                    request_message = ListAccountByProfile(request_message);
                    break;
                case "make_transaction":
                    request_message = MakeTransaction(request_message);
                    break;
                case "check_amount":
                    request_message = CheckAmount(request_message);
                    break;
                case "post_transaction":
                    request_message = Post(request_message);
                    break;
                case "cancel_transaction":
                    request_message = Cancel(request_message);
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

        private dynamic CashIn(dynamic request_message)
        {
            throw new NotImplementedException();
        }

        private dynamic Authorization(string transaction_type, dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", transaction_type, request.service, request.provider);
            string _idAll = String.Join("_", transaction_type, request.service, "ALL");
            _id = _id.ToUpper();
            _idAll = _idAll.ToUpper();
            
            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            if (trans_cfg == null) trans_cfg = Processing.Account.transaction_cfg[_idAll];
            if(trans_cfg == null)
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }
            request._id = Guid.NewGuid().ToString();
            request.transaction = transaction_type;
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Payment(dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", "PAYMENT", request.channel, request.product);
            _id = _id.ToUpper();
            if (!Processing.Account.transaction_cfg.ContainsKey(_id))
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }

            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            request._id = Guid.NewGuid().ToString();
            request.transaction = "PAYMENT";
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Cancel(dynamic request_message)
        {
            dynamic request = request_message.request;

            dynamic master_trans = Processing.Account.DataHelper.Get("finance_transaction", Query.EQ("business_transaction", request.trans_id));
            if (master_trans == null || master_trans.status != "AUTHORIZED")
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Transaction";
                return request_message;
            }

            master_trans.status = "COMPLETED";
            Processing.Account.DataHelper.Save("finance_transaction", master_trans);

            dynamic[] detail_trans = Processing.Account.DataHelper.List("finance_transaction_detail", Query.EQ("parent", master_trans._id));
            foreach (dynamic detail in detail_trans)
            {
                long balance = -1;
                if (detail.amount < 0)
                    balance = Processing.Account.CancelDebitWithBlocked(detail.account, -detail.amount);
                else balance = Processing.Account.CancelCreditWithBlocked(detail.account, detail.amount);
                if (balance > 0)
                {
                    detail.status = "COMPLETED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", detail);
                }
            }
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Transfer(dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", "TRANSFER", request.channel, request.product);
            _id = _id.ToUpper();
            if (!Processing.Account.transaction_cfg.ContainsKey(_id))
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }

            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            request._id = Guid.NewGuid().ToString();
            request.transaction = "TRANSFER";
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Purchase(dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", "PURCHASE", request.channel, request.product);
            _id = _id.ToUpper();
            if (!Processing.Account.transaction_cfg.ContainsKey(_id))
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }

            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            request._id = Guid.NewGuid().ToString();
            request.transaction = "PURCHASE";
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Withdraw(dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", "WITHDRAW", request.channel, request.product);
            _id = _id.ToUpper();
            if (!Processing.Account.transaction_cfg.ContainsKey(_id))
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }

            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            request._id = Guid.NewGuid().ToString();
            request.transaction = "WITHDRAW";
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Post(dynamic request_message)
        {
            dynamic request = request_message.request;

            dynamic master_trans = Processing.Account.DataHelper.Get("finance_transaction", Query.EQ("business_transaction", request.trans_id));
            if (master_trans == null)
            {
                request_message.error_code = "00";
                request_message.error_message = "Success";
                return request_message;
            }
            if (master_trans.status != "AUTHORIZED")
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Transaction";
                return request_message;
            }

            master_trans.status = "COMPLETED";
            Processing.Account.DataHelper.Save("finance_transaction", master_trans);

            dynamic[] detail_trans = Processing.Account.DataHelper.List("finance_transaction_detail", Query.EQ("parent", master_trans._id));
            foreach (dynamic detail in detail_trans)
                if (detail.status == "AUTHORIZED")
                {
                    long balance = -1;
                    if (detail.amount < 0)
                        balance = Processing.Account.PostDebitWithBlocked(detail.account, -detail.amount);
                    else balance = Processing.Account.PostCreditWithBlocked(detail.account, detail.amount);
                    if (balance > 0)
                    {
                        detail.status = "COMPLETED";
                        Processing.Account.DataHelper.Save("finance_transaction_detail", detail);
                    }
                }
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        private dynamic Deposit(dynamic request_message)
        {
            dynamic request = request_message.request;
            string _id = String.Join("_", "DEPOSIT", request.channel, request.product);
            _id = _id.ToUpper();
            if (!Processing.Account.transaction_cfg.ContainsKey(_id))
            {
                request_message.error_code = "01";
                request_message.error_message = "Invalid Service or Product Config";
                return request_message;
            }

            dynamic trans_cfg = Processing.Account.transaction_cfg[_id];
            request._id = Guid.NewGuid().ToString();
            request.transaction = "DEPOSIT";
            request.status = "PENDING";
            Processing.Account.DataHelper.Insert("finance_transaction", request);

            foreach (dynamic trx in trans_cfg.transactions)
            {
                dynamic _detail = new Data.DynamicObj();
                _detail._id = Guid.NewGuid().ToString();
                _detail.parent = request._id;
                _detail.account = long.Parse(String.Format(trx.account, request.profiles));
                _detail.amount = (trx.amount_type == "P") ? request.amount * trx.amount / 100 : trx.amount;
                _detail.note = trx.note;
                _detail.status = "AUTHORIZED";
                long _balance = -1;
                if (_detail.amount < 0)
                    _balance = Processing.Account.DebitBlock(_detail.account, -_detail.amount);
                else
                    _balance = Processing.Account.CreditBlock(_detail.account, _detail.amount);
                if (_balance < 0)
                {
                    _detail.status = "FAILED";
                    request.status = "FAILED";
                    Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
                    Processing.Account.DataHelper.Save("finance_transaction", request);
                    request_message.error_code = "02";
                    request_message.error_message = "Invalid Balance";
                    return request_message;
                }

                Processing.Account.DataHelper.Save("finance_transaction_detail", _detail);
            }
            request.status = "AUTHORIZED";
            Processing.Account.DataHelper.Save("finance_transaction", request);
            request_message.error_code = "00";
            request_message.error_message = "Success";
            return request_message;
        }

        /// <summary>
        /// Kiem tra so tien giao dich
        /// </summary>
        /// <param name="request_message">
        /// - profile_id
        /// - trans_code
        /// </param>
        /// <returns></returns>
        private dynamic CheckAmount(dynamic request_message)
        {
            long amount = 0;
            dynamic req = request_message.request;
            string profile_id = req.profile_id;
            string trans_code = req.trans_code;
            dynamic cfg = Processing.Account.DataHelper.Get("finance_transaction_cfg", Query.EQ("_id", trans_code));
            if (cfg == null)
            {
                request_message.error_code = "02";
                request_message.error_message = "Unsupported or invalid transaction type";
                return request_message;
            }
            return request_message;
            //for (int i = 0; i < cfg.transactions.Length; i++)
            //{
            //    dynamic trans = cfg.transactions[i];
            //    trans._id = master_trans._id + "." + i.ToString().PadLeft(2, '0');
            //    trans.account = String.Format(trans.account, master_trans.accounts);
            //    trans.finance_amount = (trans.amount_type == "F") ? (long)trans.amount : (long)Math.Round((decimal)(trans.amount * master_trans.amount / 100));
            //    MongoCollection accounts = DataHelper._database.GetCollection("finance_account");
            //    FindAndModifyArgs args = new FindAndModifyArgs();
            //    if (trans.type == "debit")
            //    {
            //        args.Query = Query.And(
            //            Query.EQ("_id", trans.account),
            //            Query.GTE("balance", trans.finance_amount));
            //        args.Update = Update.Inc("balance", -trans.finance_amount).Inc("debit", trans.finance_amount);
            //    }
            //    else
            //    {
            //        args.Query =
            //            Query.EQ("_id", trans.account);
            //        args.Update = Update.Inc("balance", trans.finance_amount).Inc("credit", trans.finance_amount);
            //    }
            //    FindAndModifyResult result = accounts.FindAndModify(args);
            //    if (!result.Ok)
            //    {
            //        trans.status = "ERROR";
            //        DataHelper.Save("finance_transaction_detail", trans);
            //        master_trans.status = "ERROR";
            //        return master_trans;
            //    }
            //    else
            //    {
            //        trans.status = "DONE";
            //        DataHelper.Save("finance_transaction_detail", trans);
            //    }
            //}
        }

        private dynamic MakeTransaction(dynamic request_message)
        {
            dynamic req = request_message.request;
            string _acc = req.accounts;
            string[] accounts = _acc.Split('|');
            long amount = req.amount;
            string note = req.note;
            string trans_code = req.trans_code;
            string created_by = req.created_by;

            dynamic master_trans = new Data.DynamicObj();
            master_trans._id = string.Concat(trans_code, ".",
                Processing.Account.DataHelper.GetNextSquence(String.Join("_", "finance_transaction", trans_code, DateTime.Today.DayOfYear.ToString().PadLeft(3, '0'))));
            master_trans.created_by = created_by;
            master_trans.amount = amount;
            master_trans.trans_code = trans_code;
            master_trans.note = note;
            master_trans.accounts = accounts;
            master_trans.status = "NEW";
            dynamic trans_cfg = Processing.Account.DataHelper.Get("finance_transaction_cfg", Query.EQ("_id", master_trans.trans_code));
            if (trans_cfg == null)
            {
                request_message.error_code = "02";
                request_message.error_message = "Unsupported or invalid transaction type";
                return request_message;
            }
            master_trans = Processing.Account.MakeTransacton(master_trans, trans_cfg);
            Processing.Account.DataHelper.Save("finance_transaction", master_trans);
            request_message.response = master_trans;
            if (master_trans.status != "DONE")
            {
                request_message.error_code = "10";
                request_message.error_message = "Transction Error. Please check your account balance";
            }
            else
            {
                request_message.error_code = "00";
                request_message.error_message = "Transaction Success!";
            }
            return request_message;
        }

        private dynamic ListAccountByProfile(dynamic request_message)
        {
            request_message.response = Processing.Account.ListAccountByProfile(request_message.request.profile_id);
            return request_message;
        }
        public FinanceServices()
        {
            Processing.Account.DataHelper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_business", "ewallet_business");
        }

        public FinanceServices(dynamic config)
        {
            Processing.Account.DataHelper = new Data.MongoHelper("mongodb://127.0.0.1:27017/ewallet_business", "ewallet_business");
        }

        public dynamic OpenAccount(dynamic request)
        {
            request.response = Processing.Account.OpenAccount(request.request.profile_id, request.request.group);
            request.error_code = "00";
            request.error_message = "Success";
            return request;
        }

        //public dynamic GetAccountByUser(dynamic _dynamicRequest)
        //{
        //    dynamic resp = new Data.DynamicObj();
        //    resp.account_info= new Data.DynamicObj();
        //    dynamic _profile = Process.Profile.Get(_dynamicRequest.user_name);
        //    if (_profile == null)
        //    {
        //        resp.error_code = "01";
        //        resp.error_message = "Thông tin tài khoản không hợp lệ. Vui lòng thử lại sau";
        //        return resp;
        //    }
        //    dynamic _acct = Process.Account.GetAccount(_profile._id);

        //}
    }
}
