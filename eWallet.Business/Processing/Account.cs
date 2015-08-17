using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business.Processing
{
    public class Account
    {
        public static Hashtable chart_of_account = new Hashtable();
        public static Hashtable transaction_cfg = new Hashtable();
        public static Data.MongoHelper DataHelper = null;
        public Account()
        {
           
        }
        public static dynamic OpenAccount(long profile_id, int account_gl)
        {
            dynamic response = new Data.DynamicObj();
            if (!chart_of_account.ContainsKey(account_gl))
            {
                return null;
            }
            try
            {
                dynamic _current = chart_of_account[account_gl];
                dynamic _acc = new Data.DynamicObj();
                _acc._id = long.Parse(String.Concat(account_gl.ToString(), profile_id.ToString()));
                _acc.type = _current.type;
                _acc.name = _current.name + " - " + profile_id.ToString();
                _acc.profile = profile_id;
                _acc.status = "ACTIVE";
                if (_current.type != "P")
                {
                    _acc.balance = (long)0;
                    _acc.credit = (long)0;
                    _acc.debit = (long)0;
                    _acc.blocked = (long)0;
                    _acc.available_balance = (long)0;
                    _acc.today_credit = (long)0;
                    _acc.today_debit = (long)0;
                }
                List<long> _childs = new List<long>();
                try
                {
                    if (_current.child_id != null)
                        foreach (int _c in _current.child_id)
                        {
                            dynamic _child = OpenAccount(profile_id, _c);
                            if (_child != null)
                                _childs.Add(
                                    _child._id
                                    );
                        }
                }
                catch { }
                _acc.child_id = _childs.ToArray();

                DataHelper.Insert("finance_account", _acc);
                return _acc;
            }
            catch(Exception ex)
            {
                response.error = "96";
                response.message = ex.Message;
                return response;
            }
            
        }
        public static dynamic OpenAccount2(string profile_id, string group)
        {
            dynamic _group_cfg = DataHelper.Get("finance_account_group", Query.EQ("_id", group));
            dynamic _acc = new Data.DynamicObj();
            _acc._id = group + profile_id + "0";
            _acc.balance = 0;// (long)10000000;
            _acc.name = "TAI KHOAN GIAO DICH - TRANSACTION ACCOUNT";
            _acc.credit = 0;// (long)10000000;
            _acc.debit = 0;// (long)10000000;
            _acc.blocked = 0;// (long)10000000;
            _acc.limit = _group_cfg.default_limit;
            _acc.status = "ACTIVED";

            dynamic _acc_fee = new Data.DynamicObj();
            _acc_fee._id = group + profile_id + "1";
            _acc_fee.name = "TAI KHOAN PHI - FEE ACCOUNT";
            _acc_fee.balance = 0;// (long)10000000;
            _acc_fee.credit = 0;// (long)10000000;
            _acc_fee.debit = 0;// (long)10000000;
            _acc_fee.blocked = 0;// (long)10000000;
            _acc_fee.status = "ACTIVED";

            dynamic _acc_loyalty = new Data.DynamicObj();
            _acc_loyalty._id = group + profile_id + "2";
            _acc_loyalty.name = "TAI KHOAN DIEM THUONG - LOYALTY ACCOUNT";
            _acc_loyalty.balance = 0;// (long)10000000;
            _acc_loyalty.credit = 0;// (long)10000000;
            _acc_loyalty.debit = 0;// (long)10000000;
            _acc_loyalty.blocked = 0;// (long)10000000;
            _acc_loyalty.status = "ACTIVED";

            DataHelper.Save("finance_account", _acc);
            DataHelper.Save("finance_account", _acc_fee);
            DataHelper.Save("finance_account", _acc_loyalty);

            return _acc;
        }


        public static long DebitBlock(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account),
                    Query.GTE("available_balance", amount)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", amount).Inc("available_balance",-amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("available_balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }

        public static long CreditBlock(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", -amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("available_balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }

        public static long PostDebitWithBlocked(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account),
                    Query.GTE("balance", amount)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", -amount).Inc("balance",-amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }

        public static long CancelDebitWithBlocked(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account),
                    Query.GTE("available_balance", amount)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", -amount).Inc("available_balance", amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("available_balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }

        public static long PostCreditWithBlocked(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", amount).Inc("balance", amount).Inc("available_balance", amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }

        public static long CancelCreditWithBlocked(long account, long amount)
        {
            try
            {
                MongoCollection accountCollection = DataHelper.Database.GetCollection("finance_account");
                FindAndModifyArgs args = new FindAndModifyArgs();
                args.Query = Query.And(
                    Query.EQ("_id", account)
                    );
                args.Update = MongoDB.Driver.Builders.Update.Inc("blocked", amount);
                FindAndModifyResult result = accountCollection.FindAndModify(args);
                return result.ModifiedDocument.GetElement("available_balance").Value.ToInt64();
            }
            catch
            {
                return -1;
            }
        }
        public static dynamic GetAccount(string account_id)
        {
            IMongoQuery _query = Query.EQ("_id", account_id);
            return DataHelper.Get("finance_account", _query);
        }

        public static dynamic ListAccountByProfile(long profile_id)
        {
            IMongoQuery _query = Query.And(
                    Query.EQ("profile",profile_id),
                    Query.NE("type","P")
                );

            return DataHelper.List("finance_account", _query);
        }

        internal static dynamic MakeTransacton(dynamic master_trans, dynamic cfg)
        {
            try
            {
                for (int i = 0; i < cfg.transactions.Length; i++)
                {
                    dynamic trans = cfg.transactions[i];
                    trans._id = master_trans._id + "." + i.ToString().PadLeft(2, '0');
                    trans.account = String.Format(trans.account, master_trans.accounts);
                    trans.finance_amount = (trans.amount_type == "F") ? (long)trans.amount : (long)Math.Round((decimal)(trans.amount * master_trans.amount / 100));
                    MongoCollection accounts = DataHelper._database.GetCollection("finance_account");
                    FindAndModifyArgs args = new FindAndModifyArgs();
                    if (trans.type == "debit")
                    {
                        args.Query = Query.And(
                            Query.EQ("_id", trans.account),
                            Query.GTE("balance", trans.finance_amount));
                        args.Update = Update.Inc("balance", -trans.finance_amount).Inc("debit", trans.finance_amount);
                    }
                    else
                    {
                        args.Query =
                            Query.EQ("_id", trans.account);
                        args.Update = Update.Inc("balance", trans.finance_amount).Inc("credit", trans.finance_amount);
                    }
                    FindAndModifyResult result = accounts.FindAndModify(args);
                    if (!result.Ok)
                    {
                        trans.status = "ERROR";
                        DataHelper.Save("finance_transaction_detail", trans);
                        master_trans.status = "ERROR";
                        return master_trans;
                    }
                    else
                    {
                        trans.status = "DONE";
                        DataHelper.Save("finance_transaction_detail", trans);
                    }
                }
                master_trans.status = "DONE";
            }
            catch
            {
                master_trans.status = "ERROR";
            }
            return master_trans;
        }
    }
}
