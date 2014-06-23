using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlackBerry.Package.Model.Integration
{
    /// <summary>
    /// Extended version of WebBrowser to return more data, when navigating over URLs and notify about errors.
    /// Based on sample implementation from:
    ///   http://msdn.microsoft.com/en-us/library/system.windows.forms.webbrowser.createsink%28VS.80%29.aspx
    /// </summary>
    public sealed class WebBrowserTurbo : WebBrowser
    {
        public event WebBrowserNavigateErrorEventHandler NavigateError;
        public event WebBrowserBeforeNavigatingEventHandler BeforeNavigating;

        private AxHost.ConnectionPointCookie _cookie;
        private EventSink _sink;

        #region COM Interfaces

        // Imports the NavigateError method from the OLE DWebBrowserEvents2 
        // interface. 
        [ComImport, Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch), TypeLibType(TypeLibTypeFlags.FHidden)]
        public interface DWebBrowserEvents2
        {
            [DispId(250)]
            void BeforeNavigate2(
                [MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                [In] ref object url,
                [In] ref object flags,
                [In] ref object targetFrameName,
                [In] ref object postData,
                [In] ref object headers,
                [In, Out, MarshalAs(UnmanagedType.VariantBool)] ref bool cancel);

            [DispId(271)]
            void NavigateError(
                [In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                [In] ref object url,
                [In] ref object targetFrame,
                [In] ref object statusCode,
                [In, Out] ref bool cancel);
        }

        /// <summary>
        /// Helper class that will forward COM events received from the COM-object-browser to the control.
        /// </summary>
        private sealed class EventSink : StandardOleMarshalObject, DWebBrowserEvents2
        {
            private WebBrowserTurbo _browser;

            public EventSink(WebBrowserTurbo browser)
            {
                if (browser == null)
                    throw new ArgumentNullException("browser");

                _browser = browser;
            }

            public void BeforeNavigate2(object pDisp, ref object url, ref object flags, ref object targetFrameName, ref object postData, ref object headers, ref bool cancel)
            {
                var e = new WebBrowserBeforeNavigatingEventArgs((string) url, (string) targetFrameName, (byte[]) postData, (string) headers, cancel);

                // notify:
                _browser.OnBeforeNavigating(e);

                // update out params:
                cancel = e.Cancel;
            }

            public void NavigateError(object pDisp, ref object url, ref object targetFrame, ref object statusCode, ref bool cancel)
            {
                var e = new WebBrowserNavigateErrorEventArgs((string) url, (string) targetFrame, (int) statusCode, cancel);

                // notify
                _browser.OnNavigateError(e);

                // update out params:
                cancel = e.Cancel;
            }
        }

        #endregion

        #region Notifications

        private void OnNavigateError(WebBrowserNavigateErrorEventArgs e)
        {
            if (NavigateError != null)
            {
                NavigateError(this, e);
            }
        }

        private void OnBeforeNavigating(WebBrowserBeforeNavigatingEventArgs e)
        {
            if (BeforeNavigating != null)
            {
                BeforeNavigating(this, e);
            }
        }

        #endregion

        protected override void CreateSink()
        {
            base.CreateSink();

            // initialize object, that will forward events from associated ActiveX control:
            _sink = new EventSink(this);
            _cookie = new AxHost.ConnectionPointCookie(ActiveXInstance, _sink, typeof(DWebBrowserEvents2));
        }

        protected override void DetachSink()
        {
            // remove even forwarder:
            if (_cookie != null)
            {
                _cookie.Disconnect();
                _cookie = null;
            }
            base.DetachSink();
        }
    }

    public delegate void WebBrowserNavigateErrorEventHandler(object sender, WebBrowserNavigateErrorEventArgs e);

    public delegate void WebBrowserBeforeNavigatingEventHandler(object sender, WebBrowserBeforeNavigatingEventArgs e);

    /// <summary>
    /// Arguments passed along with browser navigation error events.
    /// </summary>
    public sealed class WebBrowserNavigateErrorEventArgs : EventArgs
    {
        public WebBrowserNavigateErrorEventArgs(string url, string targetFrame, int statusCode, bool cancel)
        {
            Url = url;
            TargetFrame = targetFrame;
            StatusCode = statusCode;
            Cancel = cancel;
        }

        #region Properties

        public string Url
        {
            get;
            private set;
        }

        public string TargetFrame
        {
            get;
            private set;
        }

        public int StatusCode
        {
            get;
            private set;
        }

        public bool Cancel
        {
            get;
            set;
        }

        #endregion
    }

    /// <summary>
    /// Arguments passed along with browser navigation events.
    /// </summary>
    public sealed class WebBrowserBeforeNavigatingEventArgs : EventArgs
    {
        public WebBrowserBeforeNavigatingEventArgs(string url, string targetFrame, byte[] postData, string headers, bool cancel)
        {
            Url = url;
            TargetFrame = targetFrame;
            PostData = postData;
            Headers = headers;
            Cancel = cancel;
        }

        #region Properties

        public string Url
        {
            get;
            private set;
        }

        public string TargetFrame
        {
            get;
            private set;
        }

        public byte[] PostData
        {
            get;
            private set;
        }

        public string Headers
        {
            get;
            private set;
        }

        public bool Cancel
        {
            get;
            set;
        }

        #endregion
    }
}
