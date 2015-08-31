using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eWallet.CoreHosting
{
    public partial class CoreHosting : ServiceBase
    {

        private ServiceHostEnhanced[] host;
        private Data.MongoHelper _data;
        private dynamic[] list;
        public CoreHosting()
        {
            InitializeComponent();
        }

        public void ManualStart()
        {
            try
            {
                int i = 0;
                _data = new Data.MongoHelper(
                    System.Configuration.ConfigurationManager.AppSettings["CORE_DB_SERVER"],
                    System.Configuration.ConfigurationManager.AppSettings["CORE_DB_DATABASE"]
                    );
                list = _data.List("channels", null);

                Partner.Bank.BankNet.config = _data.Get("config", Query.EQ("_id", "partner_bank_banknet"));
                host = new ServiceHostEnhanced[list.Length];
                foreach (dynamic channel in list)
                {
                    host[i] = new ServiceHostEnhanced(channel);
                    host[i].Open();
                    i++;
                    System.Threading.Thread.Sleep(50);
                }
                while (true)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
            }
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(ManualStart));
            thread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                _data = null;
                list = null;
                host = null;
            }
            catch (Exception ex)
            {
            }
        }
    }
}
