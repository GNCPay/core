using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace eWallet.CoreHosting
{
    public class ServiceHostEnhanced
    {
        ServiceHost host = null;
        public dynamic channel;
        string urlservice;
        string urlmeta;
        CommunicationState _host_state;

        public CommunicationState Status
        { get { return _host_state; } }
        public ServiceHostEnhanced(dynamic _channel)
        {
            channel = _channel;
        }
        public void Open()
        {
            urlservice = string.Format("http://{0}:{1}/{2}", channel.listener_host, channel.service_port - 1, channel._id);
            urlmeta = string.Format("http://{0}:{1}/{2}", channel.listener_host, channel.service_port, channel._id);
            host = new ServiceHost(new ChannelAPI(channel));
            host.Opening += new EventHandler(host_Opening);
            host.Opened += new EventHandler(host_Opened);
            host.Closing += new EventHandler(host_Closing);
            host.Closed += new EventHandler(host_Closed);

            BasicHttpBinding httpbinding = new BasicHttpBinding();
            httpbinding.Security.Mode = BasicHttpSecurityMode.None;
            host.AddServiceEndpoint(typeof(IChannelAPI), httpbinding, urlservice);
            //host.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            ServiceMetadataBehavior metaBehavior = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (metaBehavior == null)
            {
                metaBehavior = new ServiceMetadataBehavior();
                metaBehavior.HttpGetUrl = new Uri(urlmeta);
                metaBehavior.HttpGetEnabled = true;
                host.Description.Behaviors.Add(metaBehavior);
            }
            Append(string.Format("{0} channel starting .....", channel._id));
            host.Open();
        }
        public void Close()
        { host.Close(); }

        void host_Closed(object sender, EventArgs e)
        {
            _host_state = CommunicationState.Closed;
            Append(string.Format("Host channel {0} closed", channel._id));
        }
        void host_Closing(object sender, EventArgs e)
        {
            _host_state = CommunicationState.Closing;
            Append(string.Format("Host channel {0} closing ... stand by", channel._id));
        }
        void host_Opened(object sender, EventArgs e)
        {
            _host_state = CommunicationState.Opened;
            Append(string.Format("Host channel {0} opened.", channel._id));
            Append(string.Format("Host channel {0} URL:\t{1}\t(Security is {2})", channel._id, urlservice, channel.Security));
            Append(string.Format("Host channel {0} meta URL:\t{1}\t(Not that relevant)", channel._id, urlmeta));
            Append(string.Format("Host channel {0} Waiting for clients...", channel._id));
        }
        void host_Opening(object sender, EventArgs e)
        {
            _host_state = CommunicationState.Opening;
            Append(string.Format("Host channel {0} opening ... Stand by", channel._id));
        }
        private void Append(string str)
        {
        }
    }
}
