﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace eWallet.Topup.vn.xpay {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="PartnerServiceSoap", Namespace="http://partner.logich.vn/")]
    public partial class PartnerService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback AboutOperationCompleted;
        
        private System.Threading.SendOrPostCallback EchoOperationCompleted;
        
        private System.Threading.SendOrPostCallback UserRequestOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public PartnerService() {
            this.Url = global::eWallet.Topup.Properties.Settings.Default.eWallet_Topup_vn_xpay_PartnerService;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event AboutCompletedEventHandler AboutCompleted;
        
        /// <remarks/>
        public event EchoCompletedEventHandler EchoCompleted;
        
        /// <remarks/>
        public event UserRequestCompletedEventHandler UserRequestCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://partner.logich.vn/About", RequestNamespace="http://partner.logich.vn/", ResponseNamespace="http://partner.logich.vn/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string About() {
            object[] results = this.Invoke("About", new object[0]);
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void AboutAsync() {
            this.AboutAsync(null);
        }
        
        /// <remarks/>
        public void AboutAsync(object userState) {
            if ((this.AboutOperationCompleted == null)) {
                this.AboutOperationCompleted = new System.Threading.SendOrPostCallback(this.OnAboutOperationCompleted);
            }
            this.InvokeAsync("About", new object[0], this.AboutOperationCompleted, userState);
        }
        
        private void OnAboutOperationCompleted(object arg) {
            if ((this.AboutCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.AboutCompleted(this, new AboutCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://partner.logich.vn/Echo", RequestNamespace="http://partner.logich.vn/", ResponseNamespace="http://partner.logich.vn/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string Echo() {
            object[] results = this.Invoke("Echo", new object[0]);
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void EchoAsync() {
            this.EchoAsync(null);
        }
        
        /// <remarks/>
        public void EchoAsync(object userState) {
            if ((this.EchoOperationCompleted == null)) {
                this.EchoOperationCompleted = new System.Threading.SendOrPostCallback(this.OnEchoOperationCompleted);
            }
            this.InvokeAsync("Echo", new object[0], this.EchoOperationCompleted, userState);
        }
        
        private void OnEchoOperationCompleted(object arg) {
            if ((this.EchoCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.EchoCompleted(this, new EchoCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://partner.logich.vn/UserRequest", RequestNamespace="http://partner.logich.vn/", ResponseNamespace="http://partner.logich.vn/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string UserRequest(string request) {
            object[] results = this.Invoke("UserRequest", new object[] {
                        request});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void UserRequestAsync(string request) {
            this.UserRequestAsync(request, null);
        }
        
        /// <remarks/>
        public void UserRequestAsync(string request, object userState) {
            if ((this.UserRequestOperationCompleted == null)) {
                this.UserRequestOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUserRequestOperationCompleted);
            }
            this.InvokeAsync("UserRequest", new object[] {
                        request}, this.UserRequestOperationCompleted, userState);
        }
        
        private void OnUserRequestOperationCompleted(object arg) {
            if ((this.UserRequestCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UserRequestCompleted(this, new UserRequestCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    public delegate void AboutCompletedEventHandler(object sender, AboutCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class AboutCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal AboutCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    public delegate void EchoCompletedEventHandler(object sender, EchoCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class EchoCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal EchoCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    public delegate void UserRequestCompletedEventHandler(object sender, UserRequestCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.79.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class UserRequestCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal UserRequestCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591