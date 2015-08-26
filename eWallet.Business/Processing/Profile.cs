using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business.Processing
{
    public enum ERROR
    {
        SYSTEM_ERROR = 96,
        SUCCESS = 0,
        PROFILE_EXISTED = 1,
        PROFILE_NOT_EXISTED = 2
    }
    public class Profile
    {
        public static Data.MongoHelper DataHelper = null;
        public string NextId()
        {
            long our_epoch = 1314220021721;
            long seq_id;
            long now_millis;
            int shard_id = 5;
            //seq_id = DataHelper.GetNextSquence("seq_profile") % 1024;
            seq_id = 18 % 1024;
            now_millis = DateTime.Now.Millisecond;

            long result = (now_millis - our_epoch) << 23;
            result = result | (shard_id << 10);
            result = result | (seq_id);
            return result.ToString();
        }
        public static ERROR Register(string user_name, string full_name, string mobile, out long id)
        {
            id = 0;
            try
            {
                while (mobile.StartsWith("0"))
                {
                    mobile = mobile.Substring(1, mobile.Length - 1);
                }
                mobile = "84" + mobile;
                bool _isExisted = checkExisted(user_name, mobile);
                if (_isExisted) return ERROR.PROFILE_EXISTED;

                dynamic profile = new Data.DynamicObj();
                profile.full_name = full_name;
                //profile.password = Common.Security.GenPasswordHash(user_name, password);
                string _prefix = DateTime.Today.ToString("yy") + DateTime.Today.DayOfYear.ToString().PadLeft(3,'0');
                id = long.Parse(String.Concat(_prefix,DataHelper.GetNextSquence("account_" + _prefix).ToString().PadLeft(5, '0')));
                profile._id = id;
                profile.user_name = user_name;
                profile.mobile = mobile;
                profile.status = "ACTIVED";

                DataHelper.Insert("profile", profile);

                return ERROR.SUCCESS;
            }
            catch
            {
                return ERROR.SYSTEM_ERROR;
            }
        }
        public static dynamic Get(string user_name)
        {
            IMongoQuery _queryMobile = Query.EQ("user_name", user_name);

            dynamic _current = DataHelper.Get("profile",
                _queryMobile
                );
            return _current;
        }
        public static dynamic Get(long id)
        {
            IMongoQuery _queryMobile = Query.EQ("_id", id);

            dynamic _current = DataHelper.Get("profile",
                _queryMobile
                );
            return _current;
        }
        public static ERROR Login(string user_name, string password)
        {
            try
            {
                password = Common.Security.GenPasswordHash(user_name, password);
                bool _isExisted = checkValidated(user_name, password);

                if (_isExisted) return ERROR.SUCCESS;
                return ERROR.PROFILE_NOT_EXISTED;
            }
            catch
            {
                return ERROR.SYSTEM_ERROR;
            }
        }
        private static bool checkExisted(string user_name, string mobile)
        {
            try
            {
                IMongoQuery _queryUser = Query.EQ("user_name", user_name);
                IMongoQuery _queryMobile = Query.EQ("mobile", mobile);
                dynamic _current = DataHelper.Get("profile",
                    Query.Or(_queryMobile,_queryUser)
                    );
                return _current != null;
            }
            catch
            {
                return true;
            }
        }

        private static bool checkValidated(string user_name, string password)
        {
            try
            {
                IMongoQuery _queryId = Query.EQ("user_name", user_name);
                IMongoQuery _queryPassword = Query.EQ("password", password);

                dynamic _current = DataHelper.Get("profile",
                    Query.And(_queryId, _queryPassword));
                return _current != null;
            }
            catch
            {
                return true;
            }
        }
    }
}
