using System;
using BlackBerry.Package.Helpers;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Registration
{
    /// <summary>
    /// Registration attribute providing info about custom DebugEndine implemented withing this package.
    /// </summary>
    public sealed class DebugEngineRegistrationAttribute : RegistrationAttribute
    {
        private const string DefaultInprocServerPath = "$WinDir$\\System32\\mscoree.dll";

        /// <summary>
        /// Init constructor.
        /// </summary>
        public DebugEngineRegistrationAttribute(string name, object debugEngineGUID)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (debugEngineGUID == null)
                throw new ArgumentNullException("debugEngineGUID");

            Name = name;
            DebugEngineGUID = debugEngineGUID;
        }

        #region Properties

        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the indication of support for attachment to existing programs.
        /// </summary>
        public bool Attach
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an indication of support for address breakpoints.
        /// </summary>
        public bool AddressBreakpoints
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set an indication of support for callstack breakpoints.
        /// </summary>
        public bool CallstackBreakpoints
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an indication of support for suspending thread execution.
        /// </summary>
        public bool SuspendThread
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets indication if always load the debug engine locally.
        /// </summary>
        public bool AlwaysLoadLocal
        {
            get;
            set;
        }

        public uint AutoSelectPriority
        {
            get;
            set;
        }

        public object DebugEngineGUID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the GUID for a class implementing IDebugEngine interface.
        /// </summary>
        public object DebugEngineClassGUID
        {
            get;
            set;
        }

        public string DebugEngineClassName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the GUID of the program provider class.
        /// </summary>
        public object ProgramProviderClassGUID
        {
            get;
            set;
        }

        public string ProgramProviderClassName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set the comma-separated list of the port supplier(s)
        /// </summary>
        public object PortSupplierClassGUID
        {
            get;
            set;
        }

        public string PortSupplierClassName
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

        public override void Register(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var key = context.CreateKey(@"AD7Metrics\Engine\" + AttributeHelper.Format(DebugEngineGUID));

            key.SetValue("Attach", Format(Attach));
            key.SetValue("AddressBP", Format(AddressBreakpoints));
            key.SetValue("CallstackBP", Format(CallstackBreakpoints));
            key.SetValue("SuspendThread", Format(SuspendThread));
            key.SetValue("AlwaysLoadLocal", Format(AlwaysLoadLocal));
            key.SetValue("AutoSelectPriority", AutoSelectPriority);
            if (DebugEngineClassGUID != null)
                key.SetValue("CLSID", AttributeHelper.Format(DebugEngineClassGUID));
            if (ProgramProviderClassGUID != null)
                key.SetValue("ProgramProvider", AttributeHelper.Format(ProgramProviderClassGUID));

            // Port suppliers
            if (PortSupplierClassGUID != null)
            {
                var portSupplierFormattedGuid = AttributeHelper.Format(PortSupplierClassGUID);

                key.SetValue("PortSupplier", portSupplierFormattedGuid);
                var supplierDetailsKey = context.CreateKey(@"AD7Metrics\PortSupplier\" + portSupplierFormattedGuid);
                supplierDetailsKey.SetValue("DisallowUserEnteredPorts", 0u);
                supplierDetailsKey.SetValue("CLSID", portSupplierFormattedGuid);
                supplierDetailsKey.SetValue("PortPickerCLSID", portSupplierFormattedGuid);
                supplierDetailsKey.Close();
            }
            key.Close();

            // describe classes:
            RegisterClass(context, DebugEngineClassGUID, DebugEngineClassName);
            RegisterClass(context, ProgramProviderClassGUID, ProgramProviderClassName);
            RegisterClass(context, PortSupplierClassGUID, PortSupplierClassName);
        }

        /// <summary>
        /// Registers reference to specified class.
        /// </summary>
        private void RegisterClass(RegistrationContext context, object classGuid, string className)
        {
            if (classGuid != null)
            {
                var key = context.CreateKey(@"CLSID\" + AttributeHelper.Format(classGuid));
                if (!string.IsNullOrEmpty(className))
                    key.SetValue("Class", className);
                if (!string.IsNullOrEmpty(AssemblyName))
                {
                    key.SetValue("InprocServer32", DefaultInprocServerPath);

                    // check if full-path specified or relative to the package location:
                    if ((AssemblyName.Length > 2 && AssemblyName[1] == ':') || AssemblyName.StartsWith("file://", StringComparison.InvariantCultureIgnoreCase))
                    {
                        key.SetValue("CodeBase", AssemblyName);
                    }
                    else
                    {
                        key.SetValue("CodeBase", "$PackageFolder$\\" + AssemblyName);
                    }
                }
                key.Close();
            }
        }

        private static void UnregisterClass(RegistrationContext context, object classGuid)
        {
            if (classGuid != null)
            {
                context.RemoveKey(@"CLSID\" + AttributeHelper.Format(classGuid));
            }
        }

        private static uint Format(bool value)
        {
            return value ? 1u : 0u;
        }

        public override void Unregister(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.RemoveKey(@"AD7Metrics\Engine\" + AttributeHelper.Format(DebugEngineGUID));
            UnregisterClass(context, DebugEngineClassGUID);
            UnregisterClass(context, ProgramProviderClassGUID);
            UnregisterClass(context, PortSupplierClassGUID);
        }
    }
}
