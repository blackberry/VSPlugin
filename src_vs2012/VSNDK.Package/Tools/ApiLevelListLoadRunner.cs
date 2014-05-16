using System;
using System.Collections.Generic;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to list available NDKs.
    /// </summary>
    internal sealed class ApiLevelListLoadRunner : ToolRunner
    {
        private ApiLevelListTypes _type;

        public ApiLevelListLoadRunner(string workingDirectory, ApiLevelListTypes type)
            : base("cmd.exe", workingDirectory)
        {
            Type = type;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the type of NDKs list to load.
        /// </summary>
        public ApiLevelListTypes Type
        {
            get { return _type; }
            set
            {
                _type = value;
                UpdateArguments();
            }
        }

        /// <summary>
        /// Gets the installation descriptor location.
        /// </summary>
        public Uri DescriptorLocation
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of available NDKs.
        /// </summary>
        public ApiInfo[] APIs
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            string args = "/C eclipsec.exe";

            switch (Type)
            {
                case ApiLevelListTypes.Default:
                    args += " --list";
                    break;
                case ApiLevelListTypes.Full:
                    args += " --list-all";
                    break;
                case ApiLevelListTypes.Simulators:
                    args += " --list-all --simulator";
                    break;
                default:
                    throw new InvalidOperationException("Specified list type is unsupported");
            }

            Arguments = args;
        }

        protected override void ConsumeResults(string output, string error)
        {
            DescriptorLocation = null;
            APIs = new ApiInfo[0];

            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var result = new List<ApiInfo>();

                foreach (var line in lines)
                {
                    // runtime error:
                    if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LastError = line.Substring(6).Trim();
                        continue;
                    }

                    // gets the metadata location:
                    if (line.StartsWith("location:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DescriptorLocation = new Uri(line.Substring(9).Trim());
                        continue;
                    }

                    // ignore this line:
                    if (line.StartsWith("available sdk", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    // SDK entries:
                    int separatorIndex = line.LastIndexOf(" - ", StringComparison.Ordinal);
                    if (separatorIndex > 0)
                    {
                        string name = line.Substring(separatorIndex + 3);
                        string version = line.Substring(0, separatorIndex);

                        if (name.EndsWith("(SIMULATOR)", StringComparison.InvariantCultureIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 11);
                        }
                        if (name.EndsWith("(EXTERNAL_NDK)", StringComparison.InvariantCultureIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 14);
                        }

                        result.Add(new ApiInfo(name.Trim(), new Version(version)));
                    }
                }

                APIs = result.ToArray();
            }
        }
    }
}
