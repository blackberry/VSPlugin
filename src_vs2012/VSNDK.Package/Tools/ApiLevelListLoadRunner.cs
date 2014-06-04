using System;
using System.Collections.Generic;
using System.Diagnostics;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to list available NDKs or simulator versions.
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
        /// Gets or sets the type of list to load.
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
        /// Gets the list received from server.
        /// </summary>
        public ApiInfo[] APIs
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the grouped by Level list.
        /// </summary>
        public ApiInfoArray[] ApiLevels
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
                case ApiLevelListTypes.Runtimes:
                    args += " --list-all --runtime";
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
                Array.Sort(APIs);
            }

            // group the received list by level:
            ApiLevels = GroupList(APIs);
        }

        private static ApiInfoArray[] GroupList(ApiInfo[] list)
        {
            if (list == null || list.Length == 0)
                return new ApiInfoArray[0];

            var groups = new List<List<ApiInfo>>();

            // inject info about tablet NDK:
            Add(groups, ApiInfo.CreateTabletInfo());
            
            // group BlackBerry 10 items together:
            foreach (var item in list)
            {
                Add(groups, item);
            }

            // convert to pure arrays and assign the name for each group:
            var result = new ApiInfoArray[groups.Count];
            int i = 0;
            foreach (var item in groups)
            {
                result[i++] = new ApiInfoArray(SimplifyName(item[item.Count - 1]), item[0].Level, item.ToArray());
            }

            return result;
        }

        /// <summary>
        /// Get the name of specified item without full version.
        /// </summary>
        private static string SimplifyName(ApiInfo item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (item.Name.Contains(item.Version.ToString()))
                return item.Name.Replace(item.Version.ToString(), item.Level.ToString());

            return item.Name;
        }

        private static void Add(ICollection<List<ApiInfo>> groups, ApiInfo item)
        {
            var group = Find(groups, item.Level);
            if (group != null)
            {
                group.Add(item);
            }
            else
            {
                group = new List<ApiInfo>();
                group.Add(item);
                groups.Add(group);
            }
        }

        private static List<ApiInfo> Find(IEnumerable<List<ApiInfo>> groups, Version level)
        {
            foreach (var item in groups)
            {
                Debug.Assert(item.Count > 0, "Invalid number of items!");
                if (item[0].Level == level)
                    return item;
            }

            return null;
        }
    }
}
