using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class ProileServices : BaseBusiness
    {
        public override eWallet.Data.DynamicObj Process(eWallet.Data.DynamicObj request)
        {
            dynamic request_message = request;


            string _func = request_message.function;
            switch (_func)
            {
                case "register":
                    request_message = register(request_message);
                    break;
                case "get_detail":
                    request_message = get_detail(request_message);
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

        private dynamic get_detail(dynamic request_message)
        {
            dynamic request = request_message.request;
            Processing.ERROR error = Processing.ERROR.SYSTEM_ERROR;
            try
            {
                dynamic _profile = Processing.Profile.Get(request.user_name);
                if (_profile == null)
                {
                    error = Processing.ERROR.PROFILE_NOT_EXISTED;
                }
                else
                {
                    request_message.response = _profile;
                    error = Processing.ERROR.SUCCESS;
                }
            }
            catch
            {
            }
            request_message.error_code = ((int)error).ToString().PadLeft(2, '0');
            switch (error)
            {
                case eWallet.Business.Processing.ERROR.SYSTEM_ERROR:
                    request_message.error_message = "Có lỗi trong quá trình xử lý. Vui lòng thử lại sau";
                    break;
                case eWallet.Business.Processing.ERROR.SUCCESS:
                    request_message.error_message = "Đăng ký thành công";
                    break;
                case eWallet.Business.Processing.ERROR.PROFILE_NOT_EXISTED:
                    request_message.error_message = "Tài khoản chưa được đăng ký. Vui lòng kiểm tra lại";
                    break;
                default:
                    request_message.error_message = "Có lỗi trong quá trình xử lý. Vui lòng thử lại sau";
                    break;
            }
            return request_message;
        }


        public ProileServices()
        {
            Processing.Profile.DataHelper = new Data.MongoHelper(
                   System.Configuration.ConfigurationSettings.AppSettings["PROFILE_DB_SERVER"],
                   System.Configuration.ConfigurationSettings.AppSettings["PROFILE_DB_DATABASE"]
                   );
        }
        public dynamic register(dynamic request)
        {
            long id = 0;
            Processing.ERROR error = Processing.ERROR.SYSTEM_ERROR;
            request.request.password = Guid.NewGuid().ToString().Split('-')[0];
            try
            {
                error = Processing.Profile.Register(request.request.id, request.request.full_name, request.request.mobile, out id);
            }
            catch
            {
                error = Processing.ERROR.SYSTEM_ERROR;
            }
            request.error_code = ((int)error).ToString().PadLeft(2, '0');
            switch (error)
            {
                case eWallet.Business.Processing.ERROR.SYSTEM_ERROR:
                    request.error_message = "Có lỗi trong quá trình xử lý. Vui lòng thử lại sau";
                    break;
                case eWallet.Business.Processing.ERROR.SUCCESS:
                    request.error_message = "Đăng ký thành công";

                    //Tạo tài khoản
                    dynamic open_account_request = new Data.DynamicObj();
                    open_account_request._id = Guid.NewGuid().ToString();
                    open_account_request.system = "core_profile";
                    open_account_request.module = "finance";
                    open_account_request.function = "open_account";
                    open_account_request.type = "one_way";
                    open_account_request.request = new Data.DynamicObj();
                    open_account_request.request.profile_id = id;
                    open_account_request.request.group = (int)101;
                    open_account_request.status = "NEW";
                    data.Insert("core_request", open_account_request);

                    //Gui sms
                    dynamic mt = new Data.DynamicObj();
                    mt.system = "core_sms";
                    mt.module = "sms";
                    mt.type = "one_way";
                    mt.function = "mt";
                    mt.request = new Data.DynamicObj();
                    mt.request._id = Processing.SMS.DataHelper.GetNextSquence("sms_mt_" + DateTime.Today.ToString("yyyyMMdd"));
                    mt.request.msisdn = request.request.mobile;
                    mt.request.short_code = "6073";
                    mt.request.mo_seq = 0;
                    mt.request.content = String.Format("Chuc mung ban {0} da dang ky thanh cong tai khoan GNC Payment voi ten dang nhap {0}", request.request.full_name, request.request.id);
                    mt.request.command_code = "GNCPAY";
                    mt.status = "NEW";
                    DateTime dt = DateTime.Now;
                    mt.system_created_time = dt.ToString("yyyyMMddHHmmss");
                    mt.system_created_date = dt.ToString("yyyyMMdd");
                    data.Save("core_request", mt);
                    break;
                case eWallet.Business.Processing.ERROR.PROFILE_EXISTED:
                    request.error_message = "Tài khoản đã được đăng ký. Vui lòng kiểm tra lại";
                    break;
                default:
                    break;
            }

            return request;
        }
    }
}
