using System;
using System.Collections.Generic;
using System.Diagnostics;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to list available NDKs or simulator versions.
    /// </summary>
    public sealed class ApiLevelListLoadRunner : BBToolRunner
    {
        private ApiLevelListTypes _type;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ApiLevelListLoadRunner(string workingDirectory, ApiLevelListTypes type)
            : base(workingDirectory)
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
                    throw new InvalidOperationException("Specified list type is unsupported (" + Type + ")");
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
                            name = name.Substring(0, name.Length - 11).Replace("10 Native SDK", "Simulator").Replace("Native SDK", "Simulator");
                        }
                        if (name.EndsWith("(EXTERNAL_NDK)", StringComparison.InvariantCultureIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 14);
                        }
                        if (name.EndsWith("(RUNTIME)", StringComparison.InvariantCultureIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 9).Replace("10 Native SDK", "Runtime Libraries").Replace("Native SDK", "Runtime Libraries");
                        }

                        // PH: TODO: there is no way to load list of tablet NDKs from server, so mark all of them as 'phone'...
                        result.Add(new ApiInfo(name.Trim(), new Version(version), DeviceFamilyType.Phone));
                    }
                }

                APIs = result.ToArray();
                Array.Sort(APIs);
            }

            // group the received list by level:
            ApiLevels = GroupList(APIs, Type != ApiLevelListTypes.Simulators && Type != ApiLevelListTypes.Runtimes);
        }

        /// <summary>
        /// Sorts the list of given API levels and injects some special ones (to download PlayBook NDK and 'Add custom local NDK')
        /// </summary>
        public static ApiInfoArray[] GroupList(ApiInfo[] list, bool injectExtraActions)
        {
            if ((list == null || list.Length == 0) && !injectExtraActions)
                return new ApiInfoArray[0];

            var groups = new List<List<ApiInfo>>();

            if (injectExtraActions)
            {
                // inject info about tablet NDK:
                Add(groups, ApiInfo.CreateTabletInfo());

                // inject info about 'custom NDK':
                Add(groups, ApiInfo.CreateAddCustomInfo());
            }

            // group BlackBerry 10 items together:
            if (list != null && list.Length > 0)
            {
                foreach (var item in list)
                {
                    Add(groups, item);
                }
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
