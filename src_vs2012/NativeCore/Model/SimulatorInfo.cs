using System;
using System.Collections.Generic;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Description of locally installed simulator.
    /// </summary>
    public sealed class SimulatorInfo : ApiInfo
    {
        public SimulatorInfo(string folder, string name, Version version)
            : base(name, version, DeviceFamilyType.Phone)
        {
            Folder = folder;
            Details = Version.ToString();
        }

        #region Properties

        public string Folder
        {
            get;
            private set;
        }

        public override bool IsInstalled
        {
            get { return !string.IsNullOrEmpty(Folder) && Directory.Exists(Folder); }
        }

        #endregion

        /// <summary>
        /// Loads descriptions of installed simulators from specified folders.
        /// </summary>
        public static SimulatorInfo[] Load(params string[] globalSimulatorConfigFolders)
        {
            if (globalSimulatorConfigFolders == null)
                throw new ArgumentNullException("globalSimulatorConfigFolders");

            var result = new List<SimulatorInfo>();

            foreach (var folder in globalSimulatorConfigFolders)
            {
                if (Directory.Exists(folder))
                {
                    try
                    {
                        string[] simulatorDirectories = Directory.GetFiles(folder, "*.vmxf", SearchOption.AllDirectories);

                        foreach (string simDirectory in simulatorDirectories)
                        {
                            if (simDirectory.Contains("simulator_"))
                            {
                                var name = Path.GetDirectoryName(simDirectory); // 'simulator_xx_yy_zz'...
                                var version = GetVersionFromFolderName(name);

                                if (version != null)
                                {
                                    var simulatorInfo = new SimulatorInfo(name, string.Concat("BlackBerry Simulator ", version.Major, ".", version.Minor), version);

                                    result.Add(simulatorInfo);
                                }
                                else
                                {
                                    TraceLog.WarnLine("Unable to find simulator version number in folder: \"{0}\"", simDirectory);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to load info about simulators from folder: \"{0}\"", folder);
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }
    }
}
