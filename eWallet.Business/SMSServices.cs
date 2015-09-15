using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class SMSServices : BaseBusiness
    {
        public override Data.DynamicObj Process(Data.DynamicObj request)
        {
            dynamic request_message = request;

            string _func = request_message.function;
            switch (_func)
            {
                //case "mo":
                //    request_message = MOSMS(request_message);
                //    break;
                //case "mt":
                //    request_message = MTSMS(request_message);
                //    break;
                case "send":
                    request_message = Send(request_message);
                    break;
                case "receive":
                    request_message = Receive(request_message);
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

        private dynamic Receive(dynamic request_message)
        {
            throw new NotImplementedException();
        }

        private dynamic Send(dynamic request_message)
        {
            try
            {
                dynamic mt = new Data.DynamicObj();
                mt = request_message.request;
                mt.request_id = data.GetNextSquence("sms_mt_" + DateTime.Today.ToString("yyyyMMdd"));
                mt.message_order = 1;
                mt.status = "NEW";

                Processing.SMS.DataHelper.Save("sms_mt_message", mt);
                HttpWebResponse result = SendSMS(mt.request_id, mt.receiver, mt.message_order, mt.message_content);
                if (result != null && result.StatusCode == HttpStatusCode.OK)
                {
                    mt.status = "DONE";
                    request_message.error_code = "00";
                    request_message.error_message = "Success";
                }
                else
                {
                    mt.status = "ERROR";
                    request_message.error_code = "01";
                    request_message.error_message = result.StatusDescription;
                }
                Processing.SMS.DataHelper.Save("sms_mt_message", mt);
            }
            catch
            {
                request_message.error_code = "96";
                request_message.error_message = "The system has some error(s). Please try late!";
            }
            return request_message;
        }

        /// <summary>
        /// SMS He thong -> Khach hang
        /// </summary>
        /// <param name="request_message"></param>
        /// <returns></returns>
        private dynamic MTSMS(dynamic request_message)
        {
            try
            {
                dynamic mt = new Data.DynamicObj();
                mt = request_message.request;

                mt.status = "NEW";

                Processing.SMS.DataHelper.Save("sms_mt_message", mt);
                HttpWebResponse result = SendSMS(mt);
                if (result != null && result.StatusCode == HttpStatusCode.OK)
                {
                    mt.status = "DONE";
                    request_message.error_code = "00";
                    request_message.error_message = "Success";
                }
                else
                {
                    mt.status = "ERROR";
                    request_message.error_code = "01";
                    request_message.error_message = result.StatusDescription;
                }
                Processing.SMS.DataHelper.Save("sms_mt_message", mt);
            }
            catch
            {
                request_message.error_code = "96";
                request_message.error_message = "The system has some error(s). Please try late!";
            }
            return request_message;
        }

        /// <summary>
        /// SMS Khach hang -> He thong
        /// </summary>
        /// <param name="request_message"></param>
        /// <returns></returns>
        private dynamic MOSMS(dynamic request_message)
        {
            //Gui len -> save db
            //Boc tach yeu cau -> Tao yeu cau den cac he thong tuong ung
            dynamic mo = new Data.DynamicObj();
            mo = request_message.request;
            mo._id = Processing.SMS.DataHelper.GetNextSquence("sms_mo_" + DateTime.Today.ToString("yyyyMMdd"));
            mo.status = "NEW";
            Processing.SMS.DataHelper.Save("sms_mo_message", mo);

            mo.content = mo.content.ToUpper();
            mo.content = mo.content.Replace("  ", " ");

            string[] sms_contents = mo.content.Split(' ');
            dynamic result = new Data.DynamicObj();
            switch (sms_contents[1])
            {
                case "DK":
                    mo.status = "PROCESSING";
                    Processing.SMS.DataHelper.Save("sms_mo_message", mo);
                    result = Register(mo.msisdn, sms_contents);
                    string content=String.Empty;
                    string error_code = result.error_code;
                    switch (error_code)
                    {
                        case "00":
                            content = "Chuc mung ban da dang ky thanh cong tai khoan GNC Payment. Mat khau dang nhap cua ban la " + result.password;
                            break;
                        case "01":
                            content = "So dien thoai nay da dang ky tai khoan GNC Payment";
                            break;
                    }
                    if (result.error_code != "96")
                    {
                        dynamic mt = new Data.DynamicObj();
                        mt.system = "core_sms";
                        mt.module = "sms";
                        mt.type = "one_way";
                        mt.function="mt";
                        mt.request = new Data.DynamicObj();
                        mt.request._id = Processing.SMS.DataHelper.GetNextSquence("sms_mt_" + DateTime.Today.ToString("yyyyMMdd"));
                        mt.request.msisdn = mo.msisdn;
                        mt.request.short_code = mo.short_code;
                        mt.request.mo_seq = mo.mo_seq;
                        mt.request.content = content;
                        mt.request.command_code = "GNCPAY";
                        mt.status = "NEW";
                        DateTime dt = DateTime.Now;
                        mt.system_created_time = dt.ToString("yyyyMMddHHmmss");
                        mt.system_created_date = dt.ToString("yyyyMMdd");
                        data.Save("core_request", mt);
                    }
                    break;
                default:
                    request_message.error_code = "01";
                    request_message.error_message = "Cu phap khong hop le. Vui long lien he CSKH GNCPAY de duoc ho tro.";
                    mo.status = "ERROR";
                    Processing.SMS.DataHelper.Save("sms_mo_message", mo);
                    return request_message;
            }

            mo.status = "DONE";
            Processing.SMS.DataHelper.Save("sms_mo_message", mo);
            request_message.error_code = result.error_code;
            request_message.error_message = result.error_message;
            return request_message;
        }

        private dynamic Register(string mobile, string[] sms_contents)
        {
            dynamic _resp = new Data.DynamicObj();
            try
            {
                dynamic _request = new Data.DynamicObj();
                _request._id = Guid.NewGuid().ToString();
                _request.system = "partner_api";
                _request.module = "profile";
                _request.function = "register";
                _request.type = "two_way";
                _request.request = new Data.DynamicObj();
                _request.request.full_name = sms_contents[3];
                _request.request.id = mobile;
                _request.request.password = Guid.NewGuid().ToString().Substring(0,6);
                _request.request.physical_id = sms_contents[2];

                DateTime dt = DateTime.Now;
                _request.system_created_time = dt.ToString("yyyyMMddHHmmss");
                _request.system_created_date = dt.ToString("yyyyMMdd");
                _request.status = "NEW";
                data.Save("core_request", _request);
                _resp = Business.BusinessFactory.GetBusiness("profile").GetResponse(_request._id);
                _resp.password = _request.request.password;
            }
            catch
            {
                _resp.error_code = "96";
                _resp.error_message = "System Errors";
            }
            return _resp;
        }

        public SMSServices()
        {
            Processing.SMS.DataHelper = new Data.MongoHelper(
                   System.Configuration.ConfigurationSettings.AppSettings["SMS_DB_SERVER"],
                   System.Configuration.ConfigurationSettings.AppSettings["SMS_DB_DATABASE"]
                   );
        }

        public static HttpWebResponse SendSMS(dynamic sms_mt)
        {
            try
            {
                string GATEWAY_URL = "http://115.84.179.243:6002/smsmt/gncpay?src=6173&dest={0}&mtseq={1}&msgtype=Text&msgtitle=&msgbody={2}&moseq={3}&procresult=1&mttotalseg=1&mtsegref=1&cpid=10002&serviceid=201&username=gncpay&password=gncpay@smsgwgncmedia";
                GATEWAY_URL = string.Format(GATEWAY_URL,
                    sms_mt.msisdn,
                    sms_mt._id,
                    sms_mt.content,
                    sms_mt.mo_seq
                    );
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GATEWAY_URL);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return null;
        }
        public static HttpWebResponse SendSMS(long request_id, string receiver, int message_order, string message_content)
        {
            try
            {
                string GATEWAY_URL = "http://115.84.179.243:6002/smsmt/gncpay?src=6173&dest={0}&mtseq={1}&msgtype=Text&msgtitle=&msgbody={2}&moseq={3}&procresult=1&mttotalseg=1&mtsegref=1&cpid=10002&serviceid=201&username=gncpay&password=gncpay@smsgwgncmedia";
                GATEWAY_URL = string.Format(GATEWAY_URL,
                    receiver, 
                    request_id,
                    message_content,
                    message_order
                    );
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GATEWAY_URL);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (Exception e)
            {
                
            }
            return null;
        }
    }
}
