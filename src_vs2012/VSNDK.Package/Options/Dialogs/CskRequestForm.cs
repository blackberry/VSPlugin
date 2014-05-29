using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Model.Integration;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class CskRequestForm : Form
    {
        private const string CallbackURL = "http://127.0.0.1:12345/vs-plugin";

        public CskRequestForm(string title)
        {
            // validate certificate by calling a function
            InitializeComponent();

            if (title != null)
                Text = title;

            // navigate:
            webBrowser.BeforeNavigating += OnBeforeNavigating;
            webBrowser.NavigateError += OnNavigatingError;
        }

        #region Properties

        public int StatusCode
        {
            get;
            private set;
        }

        public CskTokenInfo Token
        {
            get;
            private set;
        }

        #endregion

        public void StartRequest(string password)
        {
            string headers = "Content-Type: application/x-www-form-urlencoded";
            string data = "callbackURL=" + HttpUtility.UrlEncode(CallbackURL) + "&cskPassword=" + HttpUtility.UrlEncode(password);

            webBrowser.Navigate(new Uri("https://developer.blackberry.com/bdsc/ndk.pg", UriKind.Absolute), null, Encoding.UTF8.GetBytes(data), headers);
        }

        /// <summary>
        /// Method that handles all errors during spot on web-pages.
        /// </summary>
        private void OnNavigatingError(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            StatusCode = e.StatusCode;
            e.Cancel = true;

            Invoke(new Action(RequestFailed));

            TraceLog.WarnLine("Error {0}, while loading URL: \"{1}\"", e.StatusCode, e.Url);
        }

        /// <summary>
        /// Method that handles the BeforeNavigating event of the browser.
        /// </summary>
        private void OnBeforeNavigating(object sender, WebBrowserBeforeNavigatingEventArgs e)
        {
            if (e.Url == CallbackURL)
            {
                if (e.PostData == null || e.PostData.Length == 0)
                {
                    StatusCode = 404;
                    Token = new CskTokenInfo(null);
                }
                else
                {
                    // this seems to be NUL-terminated string...
                    string postData = Encoding.UTF8.GetString(e.PostData, 0, e.PostData[e.PostData.Length - 1] == 0 ? e.PostData.Length - 1 : e.PostData.Length);
                    string[] data = postData.Split('&');

                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = HttpUtility.UrlDecode(data[i]);

                        // and remove some unwanted chars:
                        if (data[i] != null)
                            data[i] = data[i].TrimEnd('\t', ' ');
                    }

                    StatusCode = 200;
                    Token = new CskTokenInfo(FindContentFor(data, "cskData="));
                }

                e.Cancel = true;
                // And close the form
                Invoke(new Action(RequestCompleted));
            }
        }

        private void RequestFailed()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void RequestCompleted()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Get the rest of the line from specified collection that starts with specified string key.
        /// </summary>
        private static string FindContentFor(IEnumerable<string> collection, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            if (collection != null)
            {
                foreach (var item in collection)
                {
                    if (item != null && item.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.Substring(key.Length);
                    }
                }
            }

            return null;
        }
    }
}
