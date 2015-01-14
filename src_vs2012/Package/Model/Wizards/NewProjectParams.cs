using System;

namespace BlackBerry.Package.Model.Wizards
{
    /// <summary>
    /// Context parameters passed by Visual Studio, when creating new project.
    /// </summary>
    sealed class NewProjectParams : ContextParams
    {
        internal const string TypeGuid = "{0f90e1d0-4999-11d1-b6d1-00a0c90f2744}";

        /// <summary>
        /// Init constructor.
        /// </summary>
        public NewProjectParams(object[] contextParams)
            : base(TypeGuid, contextParams != null && contextParams.Length > 1 ? contextParams[1] : null)
        {
            if (contextParams == null)
                throw new ArgumentNullException("contextParams");
            if (contextParams.Length < 7)
                throw new ArgumentOutOfRangeException("contextParams");

            LocalDirectory = contextParams[2] != null ? contextParams[2].ToString() : null;
            InstallationDirectory = contextParams[3] != null ? contextParams[3].ToString() : null;
            IsExclusive = GetBoolValue(contextParams[4]);
            SolutionName = contextParams[5] != null ? contextParams[5].ToString() : null;
            IsSilent = GetBoolValue(contextParams[6]);
        }

        #region Properties

        public string LocalDirectory
        {
            get;
            private set;
        }

        public string InstallationDirectory
        {
            get;
            private set;
        }

        public bool IsExclusive
        {
            get;
            private set;
        }

        public string SolutionName
        {
            get;
            private set;
        }

        public bool IsSilent
        {
            get;
            private set;
        }

        #endregion
    }
}
