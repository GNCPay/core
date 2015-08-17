using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eWallet.Business
{
    public class BusinessFactory
    {
        static Hashtable businessList = new Hashtable();
        public static void AddBusinessModule(string Module)
        {
            GetBusiness(Module);
        }
        public static BaseBusiness GetBusiness(string Module)
        {
            Module = Module.ToLower();
            if (businessList.Contains(Module)) return (BaseBusiness)businessList[Module];
            BaseBusiness business;
            switch (Module)
            {
                case "profile":
                    business =  new ProileServices();
                    break;
                case "finance":
                    business = new FinanceServices();
                    break;
                case "transaction":
                    business = new TransactionService();
                    break;
                case "security":
                    business = new SecurityServices();
                    break;
                case "sms":
                    business = new SMSServices();
                    break;
                case "billing":
                    business = new BillingServices();
                    break;
                default:    
                    business = null;
                    break;
            }
            business.ModuleCode = Module;
            business.Start();
            //business.Timeout = 15;
            businessList.Add(Module, business);
            return business;
        }
    }

    public class BaseBusiness
    {
        private  Queue in_progress_queue = new Queue();
        //private Hashtable in_progress_queue = new Hashtable();
        private  Queue rollback_progress_queue = new Queue();
        private  Hashtable done_queue = new Hashtable();
        public eWallet.Data.MongoHelper data;
        private System.Threading.Thread process_thread, load_thread;
        public string ModuleCode { get; set; }
        public string SystemCode { get; set; }
        public int Timeout { get { return 30; } }

        public dynamic GetResponse(string request_id)
        {
            DateTime dt = DateTime.Now;
            while (dt.AddSeconds(Timeout) > DateTime.Now)
            {
                if (done_queue.ContainsKey(request_id))
                {
                    dynamic response = done_queue[request_id];
                    response.status = "DONE";
                    data.Save("core_request", response);
                    done_queue.Remove(request_id);

                    return response;
                }
                System.Threading.Thread.Sleep(10);
            }
            return null;
        }

        public virtual void Start()
        {
            data = new Data.MongoHelper(
                   System.Configuration.ConfigurationSettings.AppSettings["CORE_DB_SERVER"],
                   System.Configuration.ConfigurationSettings.AppSettings["CORE_DB_DATABASE"]
                   );
            process_thread = new System.Threading.Thread(new System.Threading.ThreadStart(processRequest));
            load_thread = new System.Threading.Thread(new System.Threading.ThreadStart(loadQueue));

            load_thread.Start();
            process_thread.Start();
        }

        private void loadQueue()
        {
            while (true)
            {
                IMongoQuery query = Query.And
                    (
                        Query.EQ("module", ModuleCode),
                        Query.EQ("status", "NEW")
                    );
                long _total = 0;
                dynamic[] list = data.ListPagging("core_request", query, SortBy.Ascending("system_last_updated_time"), 100, 1, out _total);
                if (_total > 0)
                    for (int i = 0; i < list.Length; i++)
                        {
                            dynamic request = list[i];
                            DateTime dt = DateTime.ParseExact(request.system_created_time, "yyyyMMddHHmmss",null);
                            if (dt.AddSeconds(Timeout) > DateTime.Now)
                            {
                                in_progress_queue.Enqueue(request);
                                request.status = "WAITING";
                            }
                            else
                            {
                                request.status = "EXPIRED";
                            }
                            data.Save("core_request", request);
                        }
                //if (_total < 100)
                //{
                    System.Threading.Thread.Sleep(1000);
                //}
                //else { System.Threading.Thread.Sleep(1000); }
            }
        }

        private void processRequest()
        {
            while (true)
            {
                if (in_progress_queue.Count > 0)
                {
                    dynamic request = in_progress_queue.Dequeue();
                    //if (request.status == "NEW")
                    //{
                    try { 
                        dynamic response = Process(request);
                        if (request.type == "one_way")
                        {
                            response.status = "DONE";
                            data.Save("core_request", response);
                        }
                        else
                        done_queue.Add(request._id.ToString(), response);
                    }
                    catch { }
                    System.Threading.Thread.Sleep(10);
                }
                else System.Threading.Thread.Sleep(10);
                
            }
        }

        public virtual eWallet.Data.DynamicObj Process(eWallet.Data.DynamicObj request)
        {
            dynamic request_message = request;
            dynamic response = request;
            response.error_code = "00";
            response.error_message = "Success";

            request_message.status = "IN_PROGRESS";
            data.Save("core_request", request_message);

            return response;
        }
    }
}
