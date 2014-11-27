using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// Manager class to add dynamically BlackBerry plugin-owned errors and warnings.
    /// </summary>
    internal sealed class ErrorManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ErrorListProvider _provider;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ErrorManager(IServiceProvider serviceProvider, Guid guid, string name)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            _serviceProvider = serviceProvider;
            _provider = new ErrorListProvider(serviceProvider);
            _provider.ProviderGuid = guid;
            _provider.ProviderName = name;
        }

        #region Properties

        public int Count
        {
            get { return _provider.Tasks.Count; }
        }

        #endregion

        /// <summary>
        /// Add item to the error-list.
        /// </summary>
        public void Add(TaskErrorCategory category, string message, EventHandler navigateHandler)
        {
            Add(category, message, null, null, navigateHandler);
        }

        /// <summary>
        /// Add item to the error-list.
        /// </summary>
        public void Add(TaskErrorCategory category, string message, Project project, ProjectItem projectItem, EventHandler navigateHandler)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            var task = new ErrorTask();
            task.ErrorCategory = category;
            task.Text = message;
            task.IsTextEditable = false;
            task.HierarchyItem = GetHierarchyItem(project);

            if (projectItem != null)
            {
                task.Document = projectItem.FileNames[0];
            }

            if (navigateHandler != null)
            {
                task.Navigate += navigateHandler;
            }

            _provider.Tasks.Add(task);
        }

        /// <summary>
        /// Gets hierarchy item matching specified project. Accepts null values.
        /// </summary>
        private IVsHierarchy GetHierarchyItem(Project project)
        {
            if (project == null)
                return null;

            var solution = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            if (solution == null)
                return null;

            IVsHierarchy result;
            ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, out result));
            return result;
        }

        /// <summary>
        /// Remove all errors.
        /// </summary>
        public void Clear()
        {
            for (int i = _provider.Tasks.Count - 1; i >= 0; i--)
            {
                _provider.Tasks.RemoveAt(i);
            }
        }

        /// <summary>
        /// Brings the Errors window to front.
        /// </summary>
        public void Show()
        {
            _provider.Show();
        }
    }
}
