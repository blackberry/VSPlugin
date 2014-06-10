using System;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Wrapper class for array of API info that belong to the same API Level.
    /// </summary>
    internal sealed class ApiInfoArray : ApiInfo
    {
        public ApiInfoArray(string name, Version version, ApiInfo[] items)
            : base(name, version)
        {
            Items = items ?? new ApiInfo[0];
        }

        #region Properties

        public ApiInfo[] Items
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of stored items.
        /// </summary>
        public int Length
        {
            get { return Items.Length; }
        }

        #endregion

        public override bool IsInstalled
        {
            get
            {
                foreach (var item in Items)
                    if (!item.IsInstalled)
                        return false;

                return true;
            }
        }
    }
}
