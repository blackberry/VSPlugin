using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BlackBerry.Package.Languages.Qml
{
    sealed class QmlWindowManager : IVsCodeWindowManager
    {
        private readonly IVsCodeWindow _window;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public QmlWindowManager(IVsCodeWindow window)
        {
            if (window == null)
                throw new ArgumentNullException("window");

            _window = window;
        }

        #region Implementation of IVsCodeWindowManager

        /// <summary>
        /// Adds adornments, such as drop-down bars, to a code window.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        int IVsCodeWindowManager.AddAdornments()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Removes adornments, such as drop-down bars, from a code window.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        int IVsCodeWindowManager.RemoveAdornments()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called by the core editor to notify a language that a new view was created.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="view">[in] The <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsTextView"/> object for the new view.</param>
        int IVsCodeWindowManager.OnNewView(IVsTextView view)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
