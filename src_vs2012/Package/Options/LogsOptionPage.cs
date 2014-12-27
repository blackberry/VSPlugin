using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackBerry.NativeCore.Components;
using BlackBerry.Package.Components;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
{
    /// <summary>
    /// Option page to manage log settings.
    /// </summary>
    [Guid("fa49d8e4-61b9-4d8f-9872-26cf73a67687")]
    public sealed class LogsOptionPage : DialogPage
    {
        #region Control

        private LogsOptionControl _control;

        private LogsOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new LogsOptionControl();
                    _control.Location = new Point(0, 0);
                }

                return _control;
            }
        }

        [Browsable(false)]
        protected override IWin32Window Window
        {
            get
            {
                return Control;
            }
        }

        #endregion

        #region Properties

        public bool LimitLogs
        {
            get { return Control.LimitLogs; }
            set { Control.LimitLogs = value; }
        }

        public int LimitCount
        {
            get { return Control.LimitCount; }
            set { Control.LimitCount = value; }
        }

        public string Path
        {
            get { return Control.Path; }
            set { Control.Path = value; }
        }

        public bool InjectLogs
        {
            get { return Control.InjectLogs; }
            set { Control.InjectLogs = value; }
        }

        public bool DebuggedOnly
        {
            get { return Control.DebuggedOnly; }
            set { Control.DebuggedOnly = value; }
        }

        public uint LogsInterval
        {
            get { return Control.LogsInterval; }
            set { Control.LogsInterval = value; }
        }

        public int SLog2Level
        {
            get { return Control.SLog2Level; }
            set { Control.SLog2Level = value; }
        }

        public int SLog2Formatter
        {
            get { return Control.SLog2Formatter; }
            set { Control.SLog2Formatter = value; }
        }

        public string SLog2BufferSets
        {
            get { return Control.SLog2BufferSets; }
            set { Control.SLog2BufferSets = value; }
        }

        #endregion

        internal string[] GetSLog2BufferSets()
        {
            return SLog2BufferSets.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                LogManager.Update(Path, LimitLogs ? LimitCount : -1);
                Targets.TraceOptions(DebuggedOnly, LogsInterval, SLog2Level, SLog2Formatter, GetSLog2BufferSets(), InjectLogs, null);
            }
            else
            {
                if (e.ApplyBehavior == ApplyKind.Cancel || e.ApplyBehavior == ApplyKind.CancelNoNavigate)
                {
                    Control.OnReset();
                }
            }
        }
    }
}
