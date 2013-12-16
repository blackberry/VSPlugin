using System.Windows.Forms;
using System.ComponentModel;
using System.Net;
using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using RIM.VSNDK_Package.Signing.Models;

namespace RIM.VSNDK_Package.Signing
{
    partial class Browser
    {

        private SigningData signingData = null;

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
            signingData = new SigningData();

            // validate certificate by calling a function
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.MaximumSize = new System.Drawing.Size(1280, 768);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(1280, 768);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1280, 768);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.Url = new System.Uri("https://developer.blackberry.com/codesigning/", System.UriKind.Absolute);
            this.webBrowser1.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser1_Navigating);
            // 
            // Browser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 768);
            this.Controls.Add(this.webBrowser1);
            this.Name = "Browser";
            this.Text = "Sign in to create and download your BlackBerry ID token";
            this.ResumeLayout(false);
            this.MinimizeBox = false;

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;

        /// <summary>
        /// Method that handles the Navigating event.
        /// </summary>
        /// <param name="sender"> Contains the WebBrowser1 data. </param>
        /// <param name="e"></param>
        public void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Segments[e.Url.Segments.Length - 1].EndsWith("csk.pg"))
            {
                this.Cursor = Cursors.WaitCursor;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Url);
                request.Referer = ((WebBrowser)sender).Url.ToString();
                request.CookieContainer = new CookieContainer();
                string cookie_txt = webBrowser1.Document.Cookie;
                string[] cookies = cookie_txt.Split(';');

                try
                {
                    // Copying cookies from WebBrowser to the HttpWebRequest.
                    foreach (string cookie in cookies)
                    {
                        string[] details = cookie.Split('=');
                        if (details.Length == 2)
                        {
                            details[1] = details[1].Replace(",", "%2C");
                            request.CookieContainer.Add(e.Url, new Cookie(details[0].Trim(), details[1].Trim()));
                        }
                    }

                    request.KeepAlive = true;
                    
                    // Getting the response
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    // Creating a StreamReader
                    System.IO.Stream stream = response.GetResponseStream();
                    System.Text.Encoding ec = System.Text.Encoding.GetEncoding("utf-8");
                    System.IO.StreamReader reader = new System.IO.StreamReader(stream, ec);

                    // Save the CSK file.. create directory if necessary.
                    if (!Directory.Exists(signingData.bbidtokenPath))
                    {
                        Directory.CreateDirectory(signingData.bbidtokenPath);
                    }
                    File.WriteAllText(signingData.bbidtokenPath, reader.ReadToEnd());

                    reader.Close();
                    response.Close();
                }
                catch (Exception e1)
                {
                    MessageBox.Show("An error occurred while downloading your signing key. " + e1.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.Cursor = Cursors.Arrow;
                this.Close();
            }
        }

        /// <summary>
        /// Callback used to validate the certificate in an SSL conversation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cert"></param>
        /// <param name="chain"></param>
        /// <param name="policyErrors"></param>
        /// <returns></returns>
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // This code must be included in InitializeComponent() method:
            // validate certificate by calling a function
            // ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            bool result = false;
            if (cert.Subject.Contains(".blackberry.com"))
            {
                result = true;
            }

            return result;
        }

    }
}