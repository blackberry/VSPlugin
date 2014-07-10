using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Description of locally installed GDB.
    /// </summary>
    public sealed class GdbInfo
    {
        public GdbInfo(NdkDefinition ndk, DeviceDefinition device, RuntimeDefinition runtime, IEnumerable<string> additionalLibraryPaths)
        {
            if (ndk == null)
                throw new ArgumentNullException("ndk");
            if (string.IsNullOrEmpty(ndk.HostPath))
                throw new ArgumentOutOfRangeException("ndk");
            if (string.IsNullOrEmpty(ndk.TargetPath))
                throw new ArgumentOutOfRangeException("ndk");
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(device.IP))
                throw new ArgumentOutOfRangeException("device");

            NDK = ndk;
            Device = device;
            Runtime = runtime;
            LibraryPaths = CreateLibraryPaths(ndk, runtime, device.Type, additionalLibraryPaths);
            Executable = Path.Combine(NDK.HostPath, "usr", "bin", GetName(device.Type));
            Arguments = "--interpreter=mi2";
        }

        public GdbInfo(NdkInfo ndk, DeviceDefinition device, RuntimeDefinition runtime, IEnumerable<string> additionalLibraryPaths)
            : this(ndk != null ? ndk.ToDefinition() : null, device, runtime, additionalLibraryPaths)
        {
        }

        #region Properties

        public NdkDefinition NDK
        {
            get;
            private set;
        }

        public DeviceDefinition Device
        {
            get;
            private set;
        }

        public RuntimeDefinition Runtime
        {
            get;
            private set;
        }

        public string Executable
        {
            get;
            private set;
        }

        public string Arguments
        {
            get;
            private set;
        }

        public string[] LibraryPaths
        {
            get;
            private set;
        }

        #endregion

        private string GetName(DeviceDefinitionType type)
        {
            switch (type)
            {
                case DeviceDefinitionType.Device:
                    return "ntoarm-gdb.exe";
                case DeviceDefinitionType.Simulator:
                    return "ntox86-gdb.exe";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private static string[] CreateLibraryPaths(NdkDefinition ndk, RuntimeDefinition runtime, DeviceDefinitionType deviceType, IEnumerable<string> additionalLibraryPaths)
        {
            if (ndk == null)
                throw new ArgumentNullException("ndk");

            // create list of folders with installed libraries:
            var libraries = new List<string>();
            string archFolder = GetArchitectureFolder(deviceType);

            // developer-specified paths go first:
            if (additionalLibraryPaths != null)
            {
                libraries.AddRange(additionalLibraryPaths);
            }

            // first the libraries from the runtime:
            if (runtime != null)
            {
                libraries.Add(Path.Combine(runtime.Folder, archFolder, "lib"));
                libraries.Add(Path.Combine(runtime.Folder, archFolder, "user", "lib"));
                libraries.Add(Path.Combine(runtime.Folder, archFolder, "user", "lib", "qt4"));
            }

            // then the default ones, that belong to current NDK:
            libraries.Add(Path.Combine(ndk.TargetPath, archFolder, "lib"));
            libraries.Add(Path.Combine(ndk.TargetPath, archFolder, "usr", "lib"));
            return libraries.ToArray();
        }

        private static string GetArchitectureFolder(DeviceDefinitionType type)
        {
            switch (type)
            {
                case DeviceDefinitionType.Device:
                    return "armle-v7";
                case DeviceDefinitionType.Simulator:
                    return "x86";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}
