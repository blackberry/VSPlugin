using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BlackBerry.NativeCore.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace BlackBerry.Package.Diagnostics
{
    /// <summary>
    /// Pane panel, that listens for BlackBerry-only trace log messages and displays on UI.
    /// </summary>
    internal sealed class BlackBerryPaneTraceListener : TraceListener
    {
        public const string SName = "BlackBerry Pane Trace Listener";

        private readonly IVsOutputWindowPane _outputPane;
        private readonly TimeTracker _time;
        private readonly string _category;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BlackBerryPaneTraceListener(string title, string category, bool printTime, IVsOutputWindow outputWindow, Guid paneGuid)
            : base(SName)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentNullException("category");
            if (outputWindow == null)
                throw new ArgumentNullException("outputWindow");

            _category = category;
            _outputPane = FindOrCreatePane(title, outputWindow, paneGuid);
            if (printTime)
                _time = new TimeTracker();
        }

        private static IVsOutputWindowPane FindOrCreatePane(string title, IVsOutputWindow outputWindow, Guid paneGuid)
        {
            IVsOutputWindowPane pane;

            // try to retrieve existing pane:
            if (outputWindow.GetPane(ref paneGuid, out pane) == VSConstants.S_OK)
            {
                return pane;
            }

            // if there is no particular pane, create new instance:
            Marshal.ThrowExceptionForHR(outputWindow.CreatePane(ref paneGuid, title, 1, 0));
            return outputWindow.GetPane(ref paneGuid, out pane) == VSConstants.S_OK ? pane : null;
        }

        /// <summary>
        /// Brings this pane to front.
        /// </summary>
        public void Activate()
        {
            _outputPane.Activate();
        }

        #region TraceListener Overrides

        public override void Write(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void WriteLine(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void Write(string message, string category)
        {
            // print only messages of designated category:
            if (string.CompareOrdinal(category, _category) != 0)
                return;

            var timeString = _time != null ? _time.GetCurrent() : null;
            if (!string.IsNullOrEmpty(timeString))
                ErrorHandler.ThrowOnFailure(_outputPane.OutputStringThreadSafe(timeString));

            ErrorHandler.ThrowOnFailure(_outputPane.OutputStringThreadSafe(message));
        }

        public override void WriteLine(string message, string category)
        {
            // print only messages of designated category:
            if (string.CompareOrdinal(category, _category) != 0)
                return;

            try
            {
                var timeString = _time != null ? _time.GetCurrentAndReset() : null;
                if (!string.IsNullOrEmpty(timeString))
                    ErrorHandler.ThrowOnFailure(_outputPane.OutputStringThreadSafe(timeString));

                ErrorHandler.ThrowOnFailure(_outputPane.OutputStringThreadSafe(message));
                ErrorHandler.ThrowOnFailure(_outputPane.OutputStringThreadSafe(Environment.NewLine));
            }
            catch
            {
            }
        }

        #endregion
    }
}
