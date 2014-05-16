using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class LoginForm : Form
    {
        private DeveloperDefinition _developer;

        public LoginForm(string title, DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            _developer = developer;
            
            // validate certificate by calling a function
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            InitializeComponent();

            if (title != null)
                Text = title;

            // navigate:
            webBrowser.Navigating += OnBrowserNavigating;
            webBrowser.Navigate(new Uri("https://developer.blackberry.com/codesigning/", UriKind.Absolute));
        }

        /// <summary>
        /// Method that handles the Navigating event.
        /// </summary>
        public void OnBrowserNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Segments[e.Url.Segments.Length - 1].EndsWith("csk.pg"))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Url);
                request.Referer = ((WebBrowser)sender).Url.ToString();
                request.CookieContainer = new CookieContainer();
                string cookie_txt = webBrowser != null && webBrowser.Document != null ? webBrowser.Document.Cookie : string.Empty;
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
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            // Save the CSK file.. 
                            _developer.SaveBlackBerryToken(reader.ReadToEnd());
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Error while downloading BlackBerry ID token");
                    MessageBoxHelper.Show(ex.Message, "An error occurred while downloading your signing key.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Close();
            }
        }

        /// <summary>
        /// Callback used to validate the certificate in an SSL conversation
        /// </summary>
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // accept all 'BlackBerry' domains:
            return cert.Subject.Contains(".blackberry.com");
        }

    }
}
