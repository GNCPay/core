using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business.Processing
{
    public class Billing
    {
        public static Data.MongoHelper DataHelper = null;

        internal static dynamic ListRequest(dynamic request)
        {
            IMongoQuery query = Query.NE("_id", String.Empty);
            if (request.request_by != null) query = Query.And(Query.EQ("request_by", request.request_by));
            if (request.status != null) query = Query.And(Query.EQ("status", request.status));
            if (request.assign_to != null) query = Query.And(Query.EQ("assign_to", request.assign_to));
            long total = 0;
            return DataHelper.ListPagging("payment_request", query,SortBy.Descending("contact_time_key"),20,1,out total);
        }
        internal static void MakeRequest(dynamic request)
        {
            request._id = Guid.NewGuid().ToString();
            request.contact_time_key = long.Parse(DateTime.Parse(request.contact_time).ToString("yyyyddMMHHmm"));
            DataHelper.Save("billing_request", request);
        }
        internal static void SaveTransaction(dynamic tran_info)
        {
            DataHelper.Save("billing_transaction", tran_info);
        }
        internal static void UpdateBankTransaction(string id, string bank_id, string status)
        {
            DataHelper.UpdateObject("billing_transaction", Query.EQ("_id", id), Update.Set("bank_transaction_id", bank_id).Set("bank_transaction_status", status));
        }

        internal static void UpdateTransactionStatus(string id, string pay_by, string status)
        {
            IMongoQuery _queryId = Query.EQ("_id", id);
            IMongoQuery _queryPayBy = Query.EQ("pay_by", pay_by);

            DataHelper.UpdateObject("billing_transaction", Query.And(_queryId,_queryPayBy), Update.Set("status", status));
        }

        internal static dynamic ListTransaction(string pay_by, DateTime from_date, DateTime to_date)
        {
            IMongoQuery _queryPayBy = Query.EQ("pay_by", pay_by);
            return DataHelper.List("billing_transaction", _queryPayBy);
        }

        internal static dynamic GetRequest(dynamic request)
        {
            IMongoQuery _queryId = Query.EQ("_id", request.id);
            return DataHelper.Get("billing_request", _queryId);
        }
    }
}
