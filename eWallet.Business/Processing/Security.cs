using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business.Processing
{
    public class Security
    {
        public static Data.MongoHelper DataHelper = null;
        private static Core.Crypto.OTP _otp = new Core.Crypto.OTP();

        public static string GenOTP(string user_id)
        {
            string otp = _otp.GetNextOTP();
            dynamic user_otp = new Data.DynamicObj();
            user_otp._id = Guid.NewGuid().ToString();
            user_otp.user_id = user_id;
            user_otp.otp = Common.Security.GenPasswordHash(user_id, otp);
            user_otp.expired_time = DateTime.Now.AddMinutes(5);
            DataHelper.Insert("user_otp",user_otp);
            return otp;
        }

        public static bool IsValidOTP(string user_id, string otp)
        {
            string user_otp = Common.Security.GenPasswordHash(user_id, otp);
            long total = 0;
            dynamic current = DataHelper.ListPagging("user_otp",
                Query.And(
                    Query.EQ("otp", user_otp),
                    Query.EQ("user_id", user_id)
                ),
                SortBy.Descending("expired_time"),
                1,
                0,
                out total
                );
            bool is_valid = total > 0 && current[0] != null && (current[0].expired_time.ToLocalTime() > DateTime.Now);
            return is_valid;
        }
    }
}
