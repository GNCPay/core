using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.CoreHosting
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[] 
            //{ 
            //    new CoreHosting() 
            //};
            //ServiceBase.Run(ServicesToRun);
            //try
            //{
            //    eWallet.Notification.SMS sms = new Notification.SMS();
            //    sms.ManualStart();
            //}
            //catch {}
            CoreHosting core = new CoreHosting();
            core.ManualStart();

        }
    }
}
