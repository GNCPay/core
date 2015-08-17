using MongoDB.Driver.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace eWallet.Notification
{
    public partial class SMS : ServiceBase
    {

        eWallet.Data.MongoHelper _data = new eWallet.Data.MongoHelper();
        Hashtable listRunningModule = new Hashtable();
        Hashtable listTemplate = new Hashtable();
        dynamic[] listChannel;
        dynamic defaultSMSChannel = null;

        Thread autoSendThread = null;
        public SMS()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ManualStart();
        }

        public void ManualStart()
        {
            _data = new eWallet.Data.MongoHelper(
                System.Configuration.ConfigurationSettings.AppSettings["SMS_DB_SERVER"],
                System.Configuration.ConfigurationSettings.AppSettings["SMS_DB_DATABASE"]
                );
            listChannel = _data.List("sms_channel", null);// DataHelper.ListChannel();
            listRunningModule = new Hashtable();
            listTemplate = new Hashtable();
            eWallet.Data.DynamicObj[] runningModule = _data.List("sms_module", null);// DataHelper.ListRunningModule();
            foreach (dynamic m in runningModule)
                listRunningModule.Add(m.ModuleCode, m);

            eWallet.Data.DynamicObj[] runningTemplate = _data.List("sms_template", null);// DataHelper.ListTemplate();
            foreach (dynamic t in runningTemplate)
                listTemplate.Add(t.TemplateCode, t.TemplateContent);

            autoSendThread = new Thread(new ThreadStart(AutoSent));
            autoSendThread.Start();
            
        }

        public void AutoSent()
        {
            while (true)
            {
                try
                {
                    ManualStart();
                    long _total=0;
                    dynamic[] listUnSend = _data.ListPagging("sms_message", Query.EQ("status", "new"), null, 100, 1, out _total);// DataHelper.ListUnSendMessage(100);
                    if (listUnSend.Length == 0)
                        System.Threading.Thread.Sleep(1000);
                    else
                        foreach (dynamic msg in listUnSend)
                            try
                            {
                                Sent(msg);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                }
                catch { }
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void Sent(dynamic msg)
        {
            try
            {
                dynamic module = listRunningModule[msg.OwnerModule];
                string ReceiverTel = msg.SendTo;
                string result = "";

                dynamic template = listTemplate[msg.Template];
                msg.SendContent = String.Format(
                    template.TemplateContent,
                    msg.MessageContent);

                if (!module.Mode.Equals("PRODUCTION"))
                {
                    msg.MessageContent = ReceiverTel + "/" + msg.SendContent;
                    if (String.IsNullOrEmpty(module.TestMobile))
                    {
                        ReceiverTel = "909989986";
                    }
                    else
                        ReceiverTel = module.TestMobile;
                }
                ReceiverTel = ReceiverTel.Replace(" ", "").Replace("+", "");
                if (ReceiverTel.StartsWith("84")) ReceiverTel = ReceiverTel.Substring(2, ReceiverTel.Length - 2);
                if (ReceiverTel.StartsWith("0")) ReceiverTel = ReceiverTel.Substring(1, ReceiverTel.Length - 1);
                dynamic chnToSent = defaultSMSChannel;
               
                ReceiverTel = "84" + ReceiverTel;
                Console.WriteLine("Sent: " + ReceiverTel);
                if (ReceiverTel.Length < 11 || ReceiverTel.Length > 12
                    || (ReceiverTel.Length == 11 && !ReceiverTel.StartsWith("849"))
                    || (ReceiverTel.Length == 12 && !ReceiverTel.StartsWith("841"))
                    )
                {
                    result = "INVAILD MOBILE NUMBER (" + msg.SendTo + "/" + ReceiverTel + ")";
                }
                else
                {
                    string chnToSendCode = chnToSent.ChannelCode.ToString();
                    switch (chnToSendCode)
                    {
                        case "VIETTEL":
                            Console.WriteLine("Sent VIETTEL");
                            //result = ePayment.Notification.SMS.SendVietTel(ReceiverTel, msg.SendContent);
                            break;
                        case "MOBI":
                            Console.WriteLine("Sent MOBI ");
                            //result = ePayment.Notification.SMS.SendMobifone(ReceiverTel, msg.SendContent,
                            //    module.IsBrandName
                            //    );
                            break;
                        default:
                            Console.WriteLine("Sent INCOMM ");
                            //result = ePayment.Notification.SMS.SendIncom(ReceiverTel, msg.SendContent,
                            //    module.IsBrandName
                            //    );
                            break;
                    }
                }
                Console.WriteLine("Sent Result " + result.ToString());
                if (result.Equals("1"))
                {
                    Console.WriteLine("SUCCESS");
                    msg.SendTo = ReceiverTel;
                    SentMessage(msg, chnToSent.ChannelCode,ReceiverTel, "SENT", "SUCCESS");
                }
                else
                {
                    Console.WriteLine("FAIL");
                    SentMessage(msg, chnToSent.ChannelCode,ReceiverTel,  "ERROR", "SENT ERROR: " + result);
                }
            }
            catch { }
        }

        private void SentMessage(dynamic msg, string channel, string mobilePhone, string errorCode, string errorMessage)
        {
            //DataHelper.SendMessage(msg.Id, msg.SendContent, errorCode);
            dynamic message = _data.Get("sms_message", Query.EQ("_id", msg._id));
            message.Status = errorCode;
            message.SendContent = msg.SendContent;
            message.SendTime = DateTime.Now;
            _data.Save("sms_message", message);
            dynamic log = new eWallet.Data.DynamicObj();
            log.ErrorCode = errorCode;
            log.ErrorMessage = errorMessage;
            log.LogTime = DateTime.Now;
            log.MessageId = msg.Id;
            log.ReceiverPhone = mobilePhone;
            log.SendContent = msg.SendContent;

            _data.Save("sms_log", log);           
        }

        protected override void OnStop()
        {
            try
            {
                if (autoSendThread != null && autoSendThread.IsAlive)
                    autoSendThread.Abort();
                autoSendThread = null;
            }
            catch { }
        }
    }
}
