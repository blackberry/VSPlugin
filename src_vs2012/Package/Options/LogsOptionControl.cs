using System;
using System.Windows.Forms;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Options
{
    public partial class LogsOptionControl : UserControl
    {
        private static readonly uint[] LogsIntervals = { 100, 250, 500, 750, 1000, 2000, 5000};

        public LogsOptionControl()
        {
            InitializeComponent();

            // add intervals on UI:
            foreach (var interval in LogsIntervals)
            {
                cmbLogsInterval.Items.Add(interval >= 1000 ? (interval / 1000) + " s" : interval + " ms");
            }

            OnReset();
        }

        #region Properties

        public bool LimitLogs
        {
            get { return chbLimitLogs.Checked; }
            set { chbLimitLogs.Checked = value; }
        }

        public int LimitCount
        {
            get { return (int) numLogLimit.Value; }
            set { numLogLimit.Value = value; }
        }

        public string Path
        {
            get { return txtLogPath.Text; }
            set { txtLogPath.Text = value; }
        }

        public bool InjectLogs
        {
            get { return chbInjectLogs.Checked; }
            set { chbInjectLogs.Checked = value; }
        }

        public bool DebuggedOnly
        {
            get { return chbDebuggedOnly.Checked; }
            set { chbDebuggedOnly.Checked = value; }
        }

        public uint LogsInterval
        {
            get { return LogsIntervals[cmbLogsInterval.SelectedIndex]; }
            set { cmbLogsInterval.SelectedIndex = GetNearest(LogsIntervals, value); }
        }

        public int SLog2Level
        {
            get { return cmbSlog2Level.SelectedIndex; }
            set { cmbSlog2Level.SelectedIndex = value; }
        }

        public int SLog2Formatter
        {
            get { return cmbSlog2Formatter.SelectedIndex; }
            set { cmbSlog2Formatter.SelectedIndex = value; }
        }

        public string SLog2BufferSets
        {
            get { return txtSlog2BufferSets.Text; }
            set { txtSlog2BufferSets.Text = value; }
        }

        #endregion

        private static int GetNearest(uint[] values, uint value)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            int result = -1;
            uint delta = uint.MaxValue;

            for (int i = 0; i < values.Length; i++)
            {
                uint diff = (uint) Math.Abs(values[i] - (long) value);
                if (diff < delta)
                {
                    delta = diff;
                    result = i;
                }
            }

            return result;
        }

        private void bttBrowse_Click(object sender, System.EventArgs e)
        {
            txtLogPath.Text = DialogHelper.BrowseForFolder(txtLogPath.Text, "Browse for Logs folder");
        }

        public void OnReset()
        {
            txtLogPath.Text = string.Empty;
            chbLimitLogs.Checked = true;
            numLogLimit.Value = 25;
            chbInjectLogs.Checked = true;
            chbDebuggedOnly.Checked = false;
            cmbLogsInterval.SelectedIndex = GetNearest(LogsIntervals, TargetServiceConsoleLog.DefaultInterval);
            cmbSlog2Level.SelectedIndex = 1;
            cmbSlog2Formatter.SelectedIndex = 0;
            txtSlog2BufferSets.Text = "default";
        }
    }
}
