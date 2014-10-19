using System;
using System.IO;
using BlackBerry.Package.Helpers;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Registration
{
    /// <summary>
    /// Custom registration attribute, that will let to define new custom VC++ projects within the VSIP package.
    /// </summary>
    public sealed class ProvideProjects : RegistrationAttribute
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProvideProjects(object projectFactoryType, string categoryName, string templatesDir, int priority)
        {
            if (projectFactoryType == null)
                throw new ArgumentNullException("projectFactoryType");
            if (string.IsNullOrEmpty(templatesDir))
                throw new ArgumentNullException("templatesDir");
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");

            ProjectFactoryType = projectFactoryType;
            Name = categoryName;
            TemplatesDir = templatesDir;
            Priority = priority;

            Activity = "VC++";
        }

        #region Properties

        public object ProjectFactoryType
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string TemplatesDir
        {
            get;
            private set;
        }

        public int Priority
        {
            get;
            private set;
        }

        public string Activity
        {
            get;
            set;
        }

        #endregion

        private string GetRegistryPath()
        {
            var factoryGuid = AttributeHelper.GetGuidFrom(ProjectFactoryType);

            return string.Concat(@"NewProjectTemplates\TemplateDirs\", factoryGuid.ToString("B"), @"\/1");
        }

        public override void Register(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            // register templates folder:
            using (var key = context.CreateKey(GetRegistryPath()))
            {
                key.SetValue("", Name);
                key.SetValue("SortPriority", Priority);

                if (!string.IsNullOrEmpty(Activity))
                {
                    // put the info in a node of active development activities or inside 'Other Languages':
                    // more info: http://blogs.msdn.com/b/vsdocs/archive/2006/10/30/new-project-generation-under-the-hood.aspx
                    key.SetValue("DeveloperActivity", Activity);
                }

                string path = Path.Combine(Path.GetDirectoryName(new Uri(context.ComponentType.Assembly.CodeBase).LocalPath), TemplatesDir);
                path = context.EscapePath(Path.GetFullPath(path));
                key.SetValue("TemplatesDir", path);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            // remove registered templates folder:
            context.RemoveKey(GetRegistryPath());
        }
    }
}
