using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Model.Wizards;
using BlackBerry.Package.ViewModels;
using EnvDTE;
using EnvDTE80;

namespace BlackBerry.Package.Wizards
{
    /// <summary>
    /// Advanced template wizard, that will do a real magic based on parameters specified from .vsz files of BlackBerry new items and new projects.
    /// </summary>
    [ComVisible(true)]
    [Guid("0cb934ba-06c8-4919-9498-bc6517940bcd")]
    [ProgId("BlackBerry.PuppetMasterWizardEngine")]
    public sealed class PuppetMasterWizardEngine : BaseWizardEngine
    {
        private static string _wizardDataFolder;

        /// <summary>
        /// Gets the path, where the additional data files of the wizard are located.
        /// </summary>
        public static string WizardDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_wizardDataFolder))
                {
                    string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Templates"));
                    _wizardDataFolder = Path.Combine(path, @"VCWizards\BlackBerry");
                }

                return _wizardDataFolder;
            }
        }

        /// <summary>
        /// Method that creates new project for existing or new solution.
        /// </summary>
        internal override wizardResult ExecuteNewProject(DTE2 dte, IntPtr owner, NewProjectParams context, KeyValuePair<string, string>[] customParams)
        {
            dte.Solution.AddFromFile(Path.Combine(WizardDataFolder, "opengl-default.vcxproj"));

            MessageBox.Show("I am a custom wizard engine!");
            return wizardResult.wizardResultSuccess;
        }

        /// <summary>
        /// Method that creates new project item for existing project.
        /// </summary>
        internal override wizardResult ExecuteNewProjectItem(DTE2 dte, IntPtr owner, NewProjectItemParams context, KeyValuePair<string, string>[] customParams)
        {
            var project = context.ProjectItems.ContainingProject;
            var tokenProcessor = CreateTokenProcessor(context, context.ItemName);

            foreach (var data in customParams)
            {
                if (string.Compare(data.Key, "file", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var templatePath = Path.Combine(WizardDataFolder, GetSourceName(data.Value));
                    var destinationPath = GetDestinationName(context.ItemName, data.Value);

                    // update tokens withing the file and copy to destination:
                    tokenProcessor.UntokenFile(templatePath, destinationPath);

                    // then add that file to the project items:
                    project.ProjectItems.AddFromFile(destinationPath);
                    TraceLog.WriteLine("Added file: \"{0}\"", destinationPath);
                }
            }

            return wizardResult.wizardResultSuccess;
        }

        private TokenProcessor CreateTokenProcessor(ContextParams context, string destinationName)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (string.IsNullOrEmpty(destinationName))
                throw new ArgumentNullException("destinationName");

            var tokenProcessor = new TokenProcessor();
            var name = Path.GetFileNameWithoutExtension(destinationName);
            var ext = Path.GetExtension(destinationName);
            var safeName = CreateSafeName(name);

            var author = PackageViewModel.Instance.Developer.Name ?? "Example Inc.";
            var authorID = "ABCD1234";
            var now = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            tokenProcessor.AddReplace(@"[!output PROJECT_NAME]", context.ProjectName);
            tokenProcessor.AddReplace("$projectname$", context.ProjectName);
            tokenProcessor.AddReplace("$ProjectName$", context.ProjectName);
            tokenProcessor.AddReplace("$safename$", safeName);
            tokenProcessor.AddReplace("$SafeName$", safeName);
            tokenProcessor.AddReplace("$filename$", name);
            tokenProcessor.AddReplace("$FileName$", name);
            tokenProcessor.AddReplace("$ext$", ext);
            tokenProcessor.AddReplace("$Ext$", ext);
            tokenProcessor.AddReplace("$extension$", ext);
            tokenProcessor.AddReplace("$Extension$", ext);
            tokenProcessor.AddReplace("$user$", Environment.UserName);
            tokenProcessor.AddReplace("$User$", Environment.UserName);
            tokenProcessor.AddReplace("$author$", author);
            tokenProcessor.AddReplace("$Author$", author);
            tokenProcessor.AddReplace("$authorid$", authorID);
            tokenProcessor.AddReplace("$AuthorID$", authorID);
            tokenProcessor.AddReplace("$now$", now);
            tokenProcessor.AddReplace("$Now$", now);

            return tokenProcessor;
        }
    }
}
