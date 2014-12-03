using System;
using System.Diagnostics;
using System.Windows.Forms;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.ViewModels.TargetNavigator
{
    internal sealed class ProcessViewItem : BaseViewItem
    {
        private readonly DeviceDefinition _device;
        private readonly SystemInfoProcess _process;
        private readonly TargetServiceControl _service;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProcessViewItem(TargetNavigatorViewModel viewModel, DeviceDefinition device, TargetServiceControl service, SystemInfoProcess process, string arguments, string[] environmentVariables)
            : base(viewModel)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (service == null)
                throw new ArgumentNullException("service");
            if (process == null)
                throw new ArgumentNullException("process");

            ContextMenuName = "ContextForProcess";
            _device = device;
            _service = service;
            _process = process;
            Arguments = arguments ?? string.Empty;
            ImageSource = ViewModel.GetIconForProcess();

            if (environmentVariables == null || environmentVariables.Length == 0)
            {
                EnvironmentVariables = string.Empty;
            }
            else
            {
                Array.Sort(environmentVariables);
                EnvironmentVariables = string.Join(Environment.NewLine, environmentVariables);
            }
        }

        #region Properties

        public override string Name
        {
            get { return _process.ShortExecutablePath; }
        }

        public string ID
        {
            get { return "0x" + _process.ID.ToString("X8"); }
        }

        public string ParentID
        {
            get { return "0x" + _process.ParentID.ToString("X8"); }
        }

        public string ExecutablePath
        {
            get { return _process.ExecutablePath; }
        }

        public string Arguments
        {
            get;
            private set;
        }

        public string EnvironmentVariables
        {
            get;
            private set;
        }

        public override bool IsEnumerable
        {
            get { return false; }
        }

        public bool CanTerminate
        {
            get { return _process.ExecutablePath != "/usr/sbin/qconn"; }
        }

        public bool CanCapture
        {
            get { return !Targets.TraceIs(_device, _process); }
        }

        public bool CanStopCapture
        {
            get { return Targets.TraceIs(_device, _process); }
        }

        #endregion

        protected override void ItemsCompleted(object state)
        {
            UpdateContent(this);
        }

        public bool Terminate()
        {
            if (CanTerminate)
            {
                if (MessageBoxHelper.Show(Name, "Do you really want to terminate this process?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        _service.Terminate(_process);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Failure, trying to terminate: {0} (0x{1:X8}", _process.Name, _process.ID);
                        MessageBoxHelper.Show(Name, "Unable to terminate the process.\r\n" + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // and force the parent node to reload the items, to make the currently executed one to disappear:
                    if (Parent != null)
                    {
                        Parent.ForceReload();
                    }
                }

                return true;
            }

            return false;
        }

        public void CaptureConsole()
        {
            Targets.Trace(_device, _process);

            NotifyPropertyChanged("CanCapture");
            NotifyPropertyChanged("CanStopCapture");
        }

        public void StopCaptureConsole()
        {
            Targets.TraceStop(_device, _process);

            NotifyPropertyChanged("CanCapture");
            NotifyPropertyChanged("CanStopCapture");
        }
    }
}
