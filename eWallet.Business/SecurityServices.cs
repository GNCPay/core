﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class SecurityServices : BaseBusiness
    {
        private static bool IsTest;
        public SecurityServices()
        {
            Processing.Security.DataHelper = new Data.MongoHelper(
                   System.Configuration.ConfigurationSettings.AppSettings["SECURITY_DB_SERVER"],
                   System.Configuration.ConfigurationSettings.AppSettings["SECURITY_DB_DATABASE"]
                   );
            string mode = System.Configuration.ConfigurationSettings.AppSettings["MODE"];
            IsTest = mode != "LIVE";
        }

        public override Data.DynamicObj Process(Data.DynamicObj request)
        {
            dynamic request_message = request;
            string _func = request_message.function;
            switch (_func)
            {
                case "login":
                    request_message = login(request_message);
                    break;
                case "gen_otp":
                    request_message = gen_otp(request_message);
                    break;
                case "verify_otp":
                    request_message = verify_otp(request_message);
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

        private dynamic verify_otp(dynamic request_message)
        {
            bool is_valid = (IsTest)? request_message.request.otp.ToString() == "12345": Processing.Security.IsValidOTP(request_message.request.user_id, request_message.request.otp);
            if (is_valid)
            {
                request_message.error_code = "00";
                request_message.error_message = "Ma xac thuc hop le";
            }
            else
            {
                request_message.error_code = "02";
                request_message.error_message = "Ma xac thuc khong hop le";
            }
            return request_message;
        }

        private dynamic gen_otp(dynamic request_message)
        {
            request_message.error_code = "00";
            request_message.error_message = "Tao OTP thanh cong";
            request_message.response = new Data.DynamicObj();
            request_message.response.otp = (IsTest)? "12345": Processing.Security.GenOTP(request_message.request.user_id);

            dynamic send_sms = new Data.DynamicObj();
           if(!IsTest)
            try
            {
                send_sms._id = Guid.NewGuid().ToString();
                send_sms.system = "core_security";
                send_sms.module = "sms";
                send_sms.type = "one_way";
                send_sms.function = "send";
                send_sms.request = new Data.DynamicObj();
                
                send_sms.request.receiver = Processing.Profile.Get(request_message.request.user_id.ToString()).mobile;
                
                send_sms.request.message_content = "MA XAC THUC GIAO DICH CUA BAN LA: " + request_message.response.otp;
                send_sms.request.service_code = "GNCPAY";
                send_sms.status = "NEW";
                data.Insert("core_request", send_sms);
            }
            catch { }
            return request_message;
        }

        private dynamic login(dynamic request_message)
        {
            dynamic request = request_message.request;
            Processing.ERROR error = Processing.ERROR.SYSTEM_ERROR;
            try
            {
                error = Processing.Profile.Login(request.id, request.password);
            }
            catch
            {
                error = Processing.ERROR.SYSTEM_ERROR;
            }
            request_message.error_code = ((int)error).ToString().PadLeft(2, '0');
            switch (error)
            {
                case eWallet.Business.Processing.ERROR.SYSTEM_ERROR:
                    request_message.error_message = "Có lỗi trong quá trình xử lý. Vui lòng thử lại sau";
                    break;
                case eWallet.Business.Processing.ERROR.SUCCESS:
                    request_message.response = Processing.Profile.Get(request.id);
                    request_message.error_message = "Đăng nhập thành công";
                    break;
                case eWallet.Business.Processing.ERROR.PROFILE_NOT_EXISTED:
                    request_message.error_message = "Tài khoản chưa được đăng ký. Vui lòng kiểm tra lại";
                    break;
                default:
                    request_message.error_code = "96";
                    request_message.error_message = "Có lỗi trong quá trình xử lý. Vui lòng thử lại sau";
                    break;
            }
            return request_message;
        }
    }
}
