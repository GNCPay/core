namespace eWallet.CoreHosting
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.HostServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.HostServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // HostServiceProcessInstaller
            // 
            this.HostServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.HostServiceProcessInstaller.Password = null;
            this.HostServiceProcessInstaller.Username = null;
            // 
            // HostServiceInstaller
            // 
            this.HostServiceInstaller.DisplayName = "eWallet Core";
            this.HostServiceInstaller.ServiceName = "eWallet";
            this.HostServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.HostServiceInstaller,
            this.HostServiceProcessInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller HostServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller HostServiceInstaller;
    }
}