using System;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// Manager class to add dynamically BlackBerry plugin-owned errors and warnings.
    /// </summary>
    internal sealed class ErrorManager
    {
        private readonly ErrorListProvider _provider;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ErrorManager(IServiceProvider serviceProvider, Guid guid, string name)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            _provider = new ErrorListProvider(serviceProvider);
            _provider.ProviderGuid = guid;
            _provider.ProviderName = name;
        }

        /// <summary>
        /// Add item to the error-list.
        /// </summary>
        public void Add(TaskErrorCategory category, string message, EventHandler navigateHandler)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            var task = new ErrorTask();
            task.ErrorCategory = category;
            task.Text = message;

            if (navigateHandler != null)
            {
                task.Navigate += navigateHandler;
            }

            _provider.Tasks.Add(task);
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
    }
}
