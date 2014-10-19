using System;
using System.Runtime.InteropServices;
using BlackBerry.Package.Helpers;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Registration
{
    /// <summary>
    /// Registration attribute that creates entries to publish specified type as a custom wizard engine within current version of Visual Studio only.
    /// 
    /// More about VC++ wizards:
    ///  * http://msdn.microsoft.com/en-us/library/7k3w6w59.aspx
    ///  * http://go.microsoft.com/fwlink/?LinkId=229844
    ///  * http://msdn.microsoft.com/en-us/library/vstudio/96xz4cw2(v=vs.100).aspx
    /// </summary>
    public sealed class ProvideWizardEngineAttribute : RegistrationAttribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProvideWizardEngineAttribute()
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProvideWizardEngineAttribute(Type engineType)
        {
            if (engineType == null)
                throw new ArgumentNullException("engineType");
            if (engineType.GUID == Guid.Empty)
                throw new ArgumentOutOfRangeException("engineType");

            ClassGUID = engineType.GUID;
            ClassName = engineType.FullName;
            AssemblyName = engineType.Assembly.Location;

            var progIdAttribute = (ProgIdAttribute)GetCustomAttribute(engineType, typeof(ProgIdAttribute));
            ProgId = progIdAttribute != null ? progIdAttribute.Value : null;
        }

        #region Properties

        public object ClassGUID
        {
            get;
            set;
        }

        public string ClassName
        {
            get;
            set;
        }

        public string ProgId
        {
            get;
            set;
        }

        public string AssemblyName
        {
            get;
            set;
        }

        #endregion

        private string GetWizardClassGuidString()
        {
            return AttributeHelper.GetGuidFrom(ClassGUID).ToString("B").ToUpperInvariant();
        }

        public override void Register(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            // class registration:
            var wizardClassGuid = GetWizardClassGuidString();
            using (var key = context.CreateKey(@"CLSID\" + wizardClassGuid))
            {
                key.SetValue(null, ClassName);
                key.SetValue("Class", ClassName);

                // check if full-path specified or relative to the package location:
                if ((AssemblyName.Length > 2 && AssemblyName[1] == ':') || AssemblyName.StartsWith("file://", StringComparison.InvariantCultureIgnoreCase))
                {
                    key.SetValue("CodeBase", AssemblyName);
                }
                else
                {
                    key.SetValue("CodeBase", @"$PackageFolder$\" + AssemblyName);
                }

                key.SetValue("InprocServer32", @"$WinDir$\System32\mscoree.dll");
                key.SetValue("ThreadingModel", "Both");

                if (!string.IsNullOrEmpty(ProgId))
                {
                    using (var p = key.CreateSubkey("ProgId"))
                    {
                        p.SetValue(null, ProgId);
                    }
                }
            }

            // ProgId registration, if specified:
            if (!string.IsNullOrEmpty(ProgId))
            {
                using (var key = context.CreateKey(@"ProgId\" + ProgId))
                {
                    key.SetValue(null, ClassName);
                    using (var clsid = key.CreateSubkey("CLSID"))
                    {
                        clsid.SetValue(null, wizardClassGuid);
                    }
                }
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            // remove class registration:
            context.RemoveKey(@"CLSID\" + GetWizardClassGuidString());

            // remove ProgId registration:
            if (!string.IsNullOrEmpty(ProgId))
            {
                context.RemoveKey(@"ProgId\" + ProgId);
            }
        }
    }
}
