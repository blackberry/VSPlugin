using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;

namespace BlackBerry.Package.Wizards
{
    [ComVisible(true)]
    [Guid("0cb934ba-06c8-4919-9498-bc6517940bcd")]
    [ProgId("BlackBerry.PuppetMasterWizardEngine")]
    public sealed class PuppetMasterWizardEngine : IDTWizard
    {
        public PuppetMasterWizardEngine()
        {
            string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Templates"));
            WizardDataFolder = Path.Combine(path, @"VCWizards\BlackBerry");
        }

        private string WizardDataFolder
        {
            get;
            set;
        }

        void IDTWizard.Execute(object application, int hwndOwner, ref object[] contextParams, ref object[] customParams, ref wizardResult returnValue)
        {
            // parameters passed to the wizard are described here:
            // http://msdn.microsoft.com/en-us/library/bb164728.aspx && http://msdn.microsoft.com/en-us/library/tz690efs.aspx

            returnValue = wizardResult.wizardResultSuccess;

            var dte = application as DTE2;
            var wizardType = contextParams[0].ToString();

            // add new project:
            if (string.Compare(wizardType, "{0f90e1d0-4999-11d1-b6d1-00a0c90f2744}", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (dte != null)
                {
                    dte.Solution.AddFromFile(Path.Combine(WizardDataFolder, "opengl-default.vcxproj"));
                }
            }

            MessageBox.Show("I am a custom wizard engine!");
        }
    }
}
