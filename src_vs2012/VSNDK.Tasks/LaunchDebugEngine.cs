using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Runtime.InteropServices;

namespace VSNDK.Tasks
{
    /*[ComImport]
    [Guid("23EB4AF8-BE9C-4b49-B3A4-24F4FF657B27")]
     * */

    public abstract class LaunchDebugEngine : Microsoft.Build.Utilities.Task
    {
        /*
        [DllImport("ole32.dll")]
        static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);
        */
        private DTE2 _applicationObject;
        private string _filePath;

        public override bool Execute()
        {
            System.Diagnostics.Debugger.Launch();          
            //CoInitializeEx(NULL, COINIT_MULTITHREADED);
            //enum comEnum = {COINIT_MULTITHREADED=0};
            //CoInitializeEx((IntPtr)null, (uint)0);

            _applicationObject = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.10.0");            

            Microsoft.VisualStudio.Shell.ServiceProvider sp =
                new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_applicationObject);

            IVsDebugger dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));

            VsDebugTargetInfo info = new VsDebugTargetInfo();
            info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.dlo = Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

            info.bstrExe = _filePath;
            info.bstrCurDir = System.IO.Path.GetDirectoryName(info.bstrExe);
            info.bstrArg = null; // no command line parameters
            info.bstrRemoteMachine = null; // debug locally
            info.fSendStdoutToOutputWindow = 0; // Let stdout stay with the application.
            info.clsidCustom = new Guid("{D951924A-4999-42a0-9217-1EB5233D1D5A}"); // Set the launching engine the sample engine guid
            info.grfLaunch = 0;

            IntPtr pInfo = System.Runtime.InteropServices.Marshal.AllocCoTaskMem((int)info.cbSize);
            System.Runtime.InteropServices.Marshal.StructureToPtr(info, pInfo, false);

            try
            {
                dbg.LaunchDebugTargets(1, pInfo);
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pInfo);
                }
            }

            return true;
        }

        [Required]
        public string FilePath
        {
            set
            {
                _filePath = value;
            }
        }
    }
}
