using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace eWallet.API.Controllers
{
    public class SMSController : Controller
    {
        public ActionResult Index(string src, string dest, string moseq, string cmdcode, string msgbody, string username, string password)
        {
            CoreAPI.ChannelAPIClient client = new CoreAPI.ChannelAPIClient();
            dynamic request = new Data.DynamicObj();
            request.module = "sms";
            request.function = "echo";
            dynamic response = new Data.DynamicObj(client.Process(request.ToString()));
            return new HttpStatusCodeResult(HttpStatusCode.OK);  // OK = 200
        }
    }
}