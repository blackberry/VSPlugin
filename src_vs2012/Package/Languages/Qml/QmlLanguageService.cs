using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BlackBerry.Package.Languages.Qml
{
    [ComVisible(true)]
    [Guid(LanguageGuid)]
    internal sealed class QmlLanguageService : IVsLanguageInfo, IVsProvideColorableItems
    {
        public const string LanguageGuid = "90408888-8ce2-41af-8b01-73ec883e5b7c";
        public const string LanguageName = "QML";

        private readonly ColorableItem[] _colorableItems;

        public QmlLanguageService()
        {
            _colorableItems = new[]
            {
                new ColorableItem("QML - Text", "QML - Text", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
                new ColorableItem("QML - Identifier", "QML - Identifier", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_BOLD),
                new ColorableItem("QML - Comment", "QML - Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
                new ColorableItem("QML - Keyword", "QML - Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
                new ColorableItem("QML - Number", "QML - Number", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.DodgerBlue, Color.Empty, FONTFLAGS.FF_BOLD),
                new ColorableItem("QML - String", "QML - String", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.DarkRed, Color.Empty, FONTFLAGS.FF_DEFAULT),
                new ColorableItem("QML - Operator", "QML - Operator", COLORINDEX.CI_DARKGRAY, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT),
                new ColorableItem("QML - Types", "QML - Types", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.LightSeaGreen, Color.Empty, FONTFLAGS.FF_BOLD),
                new ColorableItem("QML - Signals", "QML - Signals", COLORINDEX.CI_USERTEXT_FG, COLORINDEX.CI_USERTEXT_BK, Color.SeaGreen, Color.Empty, FONTFLAGS.FF_DEFAULT)
            };
        }

        #region Implementation of IVsLanguageInfo

        /// <summary>
        /// Returns the name of the programming language.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="name">[out] Returns a BSTR that contains the language name.</param>
        int IVsLanguageInfo.GetLanguageName(out string name)
        {
            name = LanguageName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the file extensions belonging to this language.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="extensions">[out] Returns a BSTR that contains the requested file extensions.</param>
        int IVsLanguageInfo.GetFileExtensions(out string extensions)
        {
            extensions = ".qml;.jsqml;";
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the colorizer.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="buffer">[in] The <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsTextLines"/> interface for the requested colorizer.</param>
        /// <param name="colorizer">[out] Returns an <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsColorizer"/> object.</param>
        int IVsLanguageInfo.GetColorizer(IVsTextLines buffer, out IVsColorizer colorizer)
        {
            if (buffer == null)
            {
                colorizer = null;
                return VSConstants.E_INVALIDARG;
            }

            colorizer = new QmlColorizer(buffer);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Allows a language to add adornments to a code editor.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="window">[in] The <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsCodeWindow"/> interface for the requested code editor manager.</param>
        /// <param name="windowManager">[out] Returns an <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsCodeWindowManager"/> object.</param>
        int IVsLanguageInfo.GetCodeWindowManager(IVsCodeWindow window, out IVsCodeWindowManager windowManager)
        {
            if (window == null)
            {
                windowManager = null;
                return VSConstants.E_INVALIDARG;
            }

            windowManager = new QmlWindowManager(window);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines the number of custom colorable items proffered by the language service.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="count">[out] The number of custom colorable items provided by the language service.</param>
        int IVsProvideColorableItems.GetItemCount(out int count)
        {
            count = _colorableItems.Length;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines the item information for each custom colorable item proffered by the language service.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="index">[in] Integer containing the index value for the custom colorable item. This value is never zero.</param>
        /// <param name="item">[out] Custom colorable item object. For more information, see <see cref="T:Microsoft.VisualStudio.TextManager.Interop.IVsColorableItem"/>.</param>
        int IVsProvideColorableItems.GetColorableItem(int index, out IVsColorableItem item)
        {
            if (index < 1 || index > _colorableItems.Length)
            {
                item = null;
                return VSConstants.E_INVALIDARG;
            }

            item = _colorableItems[index - 1];
            return VSConstants.S_OK;
        }

        #endregion
    }
}
