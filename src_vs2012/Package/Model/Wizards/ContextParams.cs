using System;

namespace BlackBerry.Package.Model.Wizards
{
    /// <summary>
    /// Base class of context parameters passed from Visual Studio to the wizard engine.
    /// </summary>
    class ContextParams
    {
        /// <summary>
        /// Hidden constructor.
        /// </summary>
        protected ContextParams(Guid type, string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentNullException("projectName");

            Type = type;
            ProjectName = projectName;
        }

        /// <summary>
        /// Hidden constructor.
        /// </summary>
        protected ContextParams(string typeGuid, object projectName)
        {
            if (string.IsNullOrEmpty(typeGuid))
                throw new ArgumentNullException("typeGuid");
            if (projectName == null)
                throw new ArgumentNullException("projectName");

            Type = new Guid(typeGuid);
            ProjectName = projectName.ToString();
        }

        #region Properties

        /// <summary>
        /// Predefined type of the wizard (new-project, new-project-item, new-sub-project).
        /// </summary>
        public Guid Type
        {
            get;
            private set;
        }

        public string ProjectName
        {
            get;
            private set;
        }

        #endregion

        protected static bool GetBoolValue(object value)
        {
            if (value == null)
                return false;

            var textValue = value.ToString();

            if (string.Compare(textValue, "true", StringComparison.OrdinalIgnoreCase) == 0)
                return true;
            if (string.Compare(textValue, "1", StringComparison.CurrentCulture) == 0)
                return true;

            return false;
        }
    }
}
