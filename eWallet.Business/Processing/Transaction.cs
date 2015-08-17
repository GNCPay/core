using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business.Processing
{
    public class Transaction
    {
        public static Data.MongoHelper DataHelper = null;

        public static dynamic MakeTopup(dynamic request){
            return request;
        }

        public static dynamic List(long profile, int page)
        {
            IMongoQuery query = Query.EQ("created_by", profile);
            IMongoSortBy sort = SortBy.Descending("system_last_updated_time");
            long total = 0;
            dynamic response = new Data.DynamicObj();
            dynamic[] trans = DataHelper.ListPagging("transactions", query, sort, 10, page,out total);
            response.total = total;
            response.transactions = trans;
            return response;
        }
    }
}
