using eWallet.Topup.vn.xpay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace eWallet.Topup
{

    public class xpay : IProvider
    {
        #region "Helper Functions"

        static internal class RequestType
        {
            // LogichPayService
            public const String LOGIN = "login";
            public const String LOGOUT = "logout";
            public const String BALANCE = "balance";
            public const String CHANGE_PASSWORD = "changepass";
            public const String TOPUP = "topup";
            public const String PREPAID = "prepaid";
            public const String POSTPAID = "postpaid";
            public const String HANDSHAKE = "handshake";
            public const String TRANSACTION_HISTORY = "transhist";
            public const String GET_TRANSACTION_TRACE_LIST = "gettracelist";
            public const String TRANSACTION_HISTORY_BY_DATE = "transdate";
            public const String SUMMARY_REPORT = "sumreport";
            public const String DETAIL_REPORT = "detailreport";
        }
        String base64Encode(String data)
        {
            if (data.Length == 0) return "";
            Byte[] encData_byte = new Byte[data.Length - 1];
            encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
            String encodedData = Convert.ToBase64String(encData_byte);
            return encodedData;
        }
        string GenerateMD5(string original)
        {
            //Declarations
            Byte[] originalBytes;
            Byte[] encodedBytes;
            MD5 md5;

            //Instantiate MD5CryptoServiceProvider, get bytes for original password and compute hash (encoded password)
            md5 = new MD5CryptoServiceProvider();
            originalBytes = ASCIIEncoding.Default.GetBytes(original);
            encodedBytes = md5.ComputeHash(originalBytes);

            //Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes).Replace("-", "").ToLower();
        }
        string Hex2String(string hexvalue)
        {
            StringBuilder sb = new StringBuilder();
            string s;

            if (hexvalue.Length < 2) return "";

            while (hexvalue.Length > 0)
            {
                s = hexvalue.Substring(0, 2);
                int n = Convert.ToInt32(s, 16);
                char c = (char)n;
                sb.Append(c);
                hexvalue = hexvalue.Substring(2);
            }
            return sb.ToString();
        }
        string String2Hex(string stringvalue)
        {
            int len;
            StringBuilder sb = new StringBuilder();

            len = stringvalue.Length;
            if (len == 0) return "";
            for (int i = 0; i < len; i++)
            {
                string Char2Convert = stringvalue.Substring(i, 1);
                char c = Char2Convert.ToCharArray(0, 1)[0];
                string n = Convert.ToString(c, 16).PadLeft(2, '0');

                sb.Append(n);
            }
            return sb.ToString();
        }

        string ByteArray2HexaString(byte[] bb)
        {
            string s, result = "";

            for (int i = 0; i < bb.Length; i++)
            {
                s = Convert.ToString(bb[i], 16).PadLeft(2, '0');
                result += s.ToUpper();
            }
            return result;
        }

        byte[] HexaString2ByteArray(String hexvalue)
        {
            string s;
            int i = 0;
            byte[] result = new byte[hexvalue.Length / 2];

            while (hexvalue.Length > 0)
            {
                s = hexvalue.Substring(0, 2);
                int n = Convert.ToInt32(s, 16);
                byte c = (byte)n;
                result[i] = c;
                hexvalue = hexvalue.Substring(2);
                i++;
            }

            return result;
        }

        String base64Decode(String data)
        {
            UTF8Encoding encoder = new System.Text.UTF8Encoding();
            Decoder utf8Decode = encoder.GetDecoder();

            Byte[] todecode_byte = Convert.FromBase64String(data);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            Char[] decoded_char = new Char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            String result = new String(decoded_char);
            return result;
        }

        private string GetSign(dynamic request)
        {
            String signRules = "";
            String sign;
            string reqtype = request.reqtype.ToString();
            switch (reqtype)
            {
                case RequestType.LOGIN:
                    signRules = request.reqtype + "|" + request.username + "|" + request.password + "|" + request.version + "|" + request.partnercode;
                    break;
                case RequestType.CHANGE_PASSWORD:
                    signRules = request.reqtype + "|" + request.sessionid + "|" + request.password + "|" + request.new_pass;
                    break;
                case RequestType.BALANCE:
                    signRules = request.reqtype + "|" + request.sessionid;
                    break;
                case RequestType.TOPUP:
                    signRules = request.reqtype + "|" + request.sessionid + "|" + request.product_type + "|" + request.topup_account + "|" + request.amount;
                    break;
                case RequestType.POSTPAID:
                    signRules = request.reqtype + "|" + request.sessionid + "|" + request.product_type + "|" + request.topup_account + "|" + request.amount;
                    break;
                case RequestType.PREPAID:
                    signRules = request.reqtype + "|" + request.sessionid + "|" + request.product_type + "|" + request.quantity;
                    break;
                case RequestType.TRANSACTION_HISTORY:
                    signRules = request.reqtype + "|" + request.sessionid + "|" + request.trace_number;
                    break;
                default:
                    break;
            }

            sign = GenerateMD5(signRules);
            return sign;
        }

        string genXMLString(dynamic request)
        {
            Data.DynamicObj _request = (Data.DynamicObj)request;
            string content = String.Empty;
            foreach (string key in _request.dictionary.Keys)
            {
                content += String.Join("<", key, ">", _request.dictionary[key].ToString(), "</", key, ">");
            }
            return String.Join("<request", content, "</request>");
        }

        dynamic fromXMLString(string xmlRequest)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlRequest);
            dynamic response = new Data.DynamicObj();

            response.reqtype = doc.GetElementById("reqtype").Value;
            response.sessionid = doc.GetElementById("sessionid").Value;
            response.state = doc.GetElementById("state").Value;
            response.message = doc.GetElementById("message").Value;
            response.username = doc.GetElementById("username").Value;
            response.accountid = doc.GetElementById("accountid").Value;
            response.balance = doc.GetElementById("balance").Value;
            response.tracenumber = doc.GetElementById("tracenumber").Value;
            response.ord = doc.GetElementById("ord").Value;
            response.transtime = doc.GetElementById("transtime").Value;
            response.partnertrace = doc.GetElementById("partnertrace").Value;
            response.product_type = doc.GetElementById("product_type").Value;
            response.quantity = doc.GetElementById("quantity").Value;
            response.amount = doc.GetElementById("amount").Value;
            response.topup_account = doc.GetElementById("topup_account").Value;
            response.topup_value = doc.GetElementById("topup_value").Value;
            response.account_name = doc.GetElementById("account_name").Value;
            response.account_number2 = doc.GetElementById("account_number2").Value;
            response.content = doc.GetElementById("content").Value;
            response.trans_type = doc.GetElementById("trans_type").Value;
            response.url = doc.GetElementById("url").Value;
            response.secret_string = doc.GetElementById("secret_string").Value;
            response.permision = doc.GetElementById("permision").Value; ;
            response.must_update_data = doc.GetElementById("must_update_data").Value;
            //response._products = r._products;

            return response;
        }

        string genProductType(string provider, long amount)
        {
            return String.Empty;
        }
        #endregion
        private dynamic _config = null;
        private static vn.xpay.PartnerService api;

        private static Data.MongoHelper _data;

        private static string session_id;
        private static long balance = 0;
        public xpay()
        {

        }

        public xpay(dynamic config)
        {
            _config = config;
            api = new PartnerService();
            api.Url = config.gateway_url;
            _data = new Data.MongoHelper(_config.log_server, _config.log_db);
        }

        private void login()
        {
            dynamic request = new Data.DynamicObj();
            request.reqtype = "login";
            request.username = _config.user_name;
            request.appid = "partner";
            request.password = GenerateMD5(_config.password);
            request.version = _config.version;
            request.partnercode = _config.partner_code;
            request.requestid = DateTime.Now.ToString("HHmmss");

            dynamic response = processRequest(request); ;
            if (response.state = 0)
            {
                session_id = response.sessionid;
                balance = response.balance;
            }
        }

        private void logout()
        {
            dynamic request = new Data.DynamicObj();
            request.reqtype = "login";
            request.sessionid = session_id;
            request.requestid = DateTime.Now.ToString("HHmmss");
            dynamic response = processRequest(request);
        }

        private void handshake()
        {
            dynamic request = new Data.DynamicObj();
            request.reqtype = "handshake";
            request.sessionid = session_id;
            request.requestid = DateTime.Now.ToString("HHmmss");
            dynamic response = processRequest(request);
        }

        public dynamic Topup(string provider, string account_number, int amount, string ref_id)
        {
            dynamic request = new Data.DynamicObj();
            request.reqtype = "topup";
            request.sessionid = session_id;
            request.requestid = ref_id;
            request.topup_account = account_number;
            request.product_type = genProductType(provider, amount);
            request.amount = amount.ToString();
            
            dynamic response = processRequest(request);
            return response;
        }

        private dynamic processRequest(dynamic request)
        {
            dynamic _log = request;
            _log._id = Guid.NewGuid().ToString();
            _log.partner_system_code = "xpay";
            _log.type = "send";
            _data.Insert("partner_log", _log);
            request.sign = GetSign(request);
            string result = api.UserRequest(base64Encode(request.ToXmlString()));
            result = base64Decode(result);

            dynamic response =  fromXMLString(result);
            _log = response;
            _log.log_ref_id = _log._id;
            _log._id = Guid.NewGuid().ToString();
            _log.partner_system_code = "xpay";
            _log.type = "received";

            _data.Insert("partner_log", _log);
            return response;
        }
    }
}
