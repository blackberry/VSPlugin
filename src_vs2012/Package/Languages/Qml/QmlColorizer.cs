using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BlackBerry.Package.Languages.Qml
{
    sealed class QmlColorizer : IVsColorizer, IVsColorizer2
    {
        private readonly IVsTextLines _buffer;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public QmlColorizer(IVsTextLines buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            _buffer = buffer;
        }

        #region Implementation of IVsColorizer

        /// <summary>
        /// Returns the state maintenance requirement for the colorizer.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="flag">[out] true if this colorizer requires per-line state maintenance, otherwise it should be set to false.</param>
        int IVsColorizer.GetStateMaintenanceFlag(out int flag)
        {
            flag = 1;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines the state in which colorization of the first line of the buffer should begin.
        /// </summary>
        /// <returns>
        /// If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK"/>. If it fails, it returns an error code.
        /// </returns>
        /// <param name="startState">[out] Pointer to a long integer that represents the start state of the colorizer.</param>
        int IVsColorizer.GetStartState(out int startState)
        {
            startState = 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Colorizes the given text.
        /// </summary>
        /// <returns>
        /// Returns the colorizer's state at the end of the line.
        /// </returns>
        /// <param name="line">[in] Line to be colorized.</param>
        /// <param name="length">[in] Length of the line minus the end-of-line marker (CR, LF, CRLF pair, or 0 (EOF)).</param>
        /// <param name="pszText">[in] The line's text (examine up to <paramref name="length"/> characters).</param>
        /// <param name="state">[in] The colorizer's state at the beginning of the line.</param>
        /// <param name="attributes">[out] An array of color attributes to be filled in for the text. The array contains one member for each character in the line colorized, and an additional element which represents the background color of the space to the right of the last character. This array is <paramref name="length"/> + 1 characters long. Members of the pAttributes array may contain bits that can be masked with the various values provided in the <see cref="T:Microsoft.VisualStudio.TextManager.Interop.COLORIZER_ATTRIBUTE"/> enumeration to get the information required. For more information, see <see cref="T:Microsoft.VisualStudio.TextManager.Interop.COLORIZER_ATTRIBUTE"/>.</param>
        int IVsColorizer.ColorizeLine(int line, int length, IntPtr pszText, int state, uint[] attributes)
        {
            string text = Marshal.PtrToStringUni(pszText, length);

            // perform some dummy colorizing, simply marking numbers out of text:
            for (int i = 0; i < length; i++)
            {
                attributes[i] = char.IsDigit(text[i]) ? 5u : 1u; // must match index of "colorable-item" + 1
            }
            attributes[length] = 1u;

            return 0; // new-state
        }

        /// <summary>
        /// Determines the end-of-line state for a given line.
        /// </summary>
        /// <returns>
        /// Returns the state at the end of the line.
        /// </returns>
        /// <param name="line">[in] Line whose state is to be queried.</param>
        /// <param name="length">[in] Length of the line minus the end-of-line marker (CR, LF, CRLF pair, or 0 (EOF)).</param>
        /// <param name="pszText">[in] The line's text (examine only up to <paramref name="length"/> characters).</param>
        /// <param name="state">[in] The colorizer's state at the beginning of the line.</param>
        int IVsColorizer.GetStateAtEndOfLine(int line, int length, IntPtr pszText, int state)
        {
            return 0; // new-state
        }

        /// <summary>
        /// Releases any references held on a <see cref="T:Microsoft.VisualStudio.TextManager.Interop.VsTextBuffer"/> object.
        /// </summary>
        void IVsColorizer.CloseColorizer()
        {
        }

        #endregion

        #region Implementation of IVsColorizer2

        /// <summary>
        /// Starts or resume colorization operations.
        /// </summary>
        int IVsColorizer2.BeginColorization()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Suspends or ends colorization operations.
        /// </summary>
        int IVsColorizer2.EndColorization()
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
