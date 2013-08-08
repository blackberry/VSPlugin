using System.Windows.Forms;
using System.ComponentModel;
using System.Net;
using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
namespace RIM.VSNDK_Package.Signing
{
    partial class Browser
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            //Trust all certificates
//            System.Net.ServicePointManager.ServerCertificateValidationCallback =
//                ((sender, certificate, chain, sslPolicyErrors) => true);

            // trust sender
//            System.Net.ServicePointManager.ServerCertificateValidationCallback
//                            = ((sender, cert, chain, errors) => cert.Subject.Contains("https://bdsc01cnc.rim.net:8443/bdsc/Developer/csk.html"));

            // validate cert by calling a function
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1024, 768);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.TabStop = false;
            this.webBrowser1.Url = new System.Uri("https://bdsc01cnc.rim.net:8443/bdsc/Developer/csk.html", System.UriKind.Absolute);
            this.webBrowser1.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser1_Navigating);
            // 
            // Browser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 768);
            this.Controls.Add(this.webBrowser1);
            this.Name = "Browser";
            this.Text = "Sign in to create and download your BlackBerry ID token";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;

        public void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Segments[e.Url.Segments.Length - 1].EndsWith(".pg"))
            {
                e.Cancel = true;

                WebClient client = new WebClient();

                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(e.Url, signingDialog.bbidtokenPath);
            }
        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Close();

            if (File.Exists(signingDialog.bbidtokenPath))
            {
                RegistrationWindow win = new RegistrationWindow();
                bool? res = win.ShowDialog();
            }
            signingDialog.UpdateUI(File.Exists(signingDialog.certPath));
        }

        // callback used to validate the certificate in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            bool result = false;
            if ((cert.Subject.Contains("=RIM,")) || (cert.Subject.Contains("=Research in Motion Limited,")))
            {
                result = true;
            }

            return result;
        }

    }
}