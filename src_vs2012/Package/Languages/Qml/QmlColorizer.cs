using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BlackBerry.Package.Languages.Qml
{
    /// <summary>
    /// Colorizer class responsible for correctly assigning colors to particular chars inside QML file.
    /// </summary>
    internal sealed class QmlColorizer : IVsColorizer, IVsColorizer2
    {
        #region Internal Classes

        private const string Operators = "+-~=/\\*%&.,:;@|!?#$^(){}[]<>";

        private static readonly string[] Keywords = new string[]
        {
            "alias", "and", "break", "case", "continue", "do", "decimal", "else", "if", "false", "for", "function", "import", "not", "or", "property", "return", "signal", "switch", "true", "var", "while", "ListItemData",
            "ListItem"
        };

        private static readonly string[] BasicTypes = new string[] { "bool", "double", "int", "string", "variant", "QTimer" };

        private static readonly string[] CascadeBasicTypes = new string[]
        {
            "ActionBar", "ActionBarPlacement", "Color", "ExpandMode", "FocusPolicy", "FontSize", "FontWeight", "InputType", "PickerKind", "ScalingMethod", "ScalingMode", "ScrollIndicatorMode",
            "ScrollMode", "ScrollPosition", "SnapMode", "SupportedDisplayOrientation", "SystemDefaults", "UIOrientation", "TitleBarAppearance", "VisualStyle",
            "AbsoluteLayout", "AbsoluteLayoutProperties", "DockLayout", "FlowListLayout", "FlowListLayoutProperties", "FreeFormTitleBarKindProperties", "GridLayout", "GridListLayout",
            "HorizontalAlignment", "Layout", "LayoutOrientation", "LayoutProperties",
            "ListHeaderMode", "ListLayout", "StackLayout", "StackLayoutProperties", "StackListLayout", "StackLayoutProperties", "TitleBarExpandableAreaIndicatorVisibility", "TitleBarKind", "TouchType",
            "VerticalAlignment"
        };

        private static readonly string[] CascadesBasicSignals = new string[]
        {
            "actions", "animations", "attachedObjects", "eventHandlers", "gestureHandlers", "onClicked", "onCreationCompleted", "onEnded", "onHeaderDateChanged", "onSelectedValueChanged", "onStarted", "onTextChanged", "onTextChanging", "onTapped",
            "onTimeout", "onTriggered", "onValueChanged"
        };

        private static readonly string[] CascadesControlTypes = new string[]
        {
            "ActionItem", "ActionSet", "ActivityIndicator", "Button", "CheckBox", "ComponentDefinition", "Container", "ControlDelegate", "CustomControl", "CustomListItem",
            "DateTimePicker", "Divider", "DropDown", "ExpandableView", "ForeignWindowControl", "GroupDataModel", "Header", "ImageButton", "ImageToggleButton", "ImageView", "ImplicitAnimationController", "InvokeActionItem",
            "Label", "ListItemComponent", "ListView", "NavigationPane", "Option", "OrientationSupport", "Page", "Picker", "PickerItemComponent", "ProgressIndicator", "RadioGroup",
            "ScrollView", "SegmentedControl", "Sheet", "Slider", "StandardsListItem", "StandardPickerItem", "Tab", "TabbedPane", "TapHandler", "TextArea", "TextField", "ToggleButton", "TitleBar", "WebView"
        };

        private static readonly string[] SpecialWords = new string[] { "bb", "cascades", "codetitans", "qsTr", "system", "ui", "Retranslate", "onLocaleOrLanguageChanged", "onLanguageChanged" };

        private enum Colors : uint
        {
            Unused = 0,
            Text = 1,
            Identifier = 2,
            Comment = 3,
            Keyword = 4,
            Number = 5,
            String = 6,
            Operator = 7,
            Type = 8,
            Signals = 9
        }

        private enum States
        {
            Text = 0,
            SingleLineComment = 1,
            MultiLineComment = 2,
            StringSingleQuoted = 3,
            StringDoubleQuoted = 4,
            Number = 5
        }

        /// <summary>
        /// Structure representing a status of the scanner.
        /// </summary>
        private struct ScannerStatus
        {
            private States _state;
            private int _level;

            /// <summary>
            /// Init constructor.
            /// </summary>
            public ScannerStatus(int state)
            {
                _state = (States) ((state >> 16) & 0x0F);
                _level = state & 0xFF;
            }

            /// <summary>
            /// Init constructor.
            /// </summary>
            public ScannerStatus(States state, int level)
            {
                _state = state;
                _level = level;
            }

            /// <summary>
            /// Gets simple representation of the state.
            /// </summary>
            public int ToInt32()
            {
                return (((int) _state) << 16) | _level;
            }

            public static implicit operator int(ScannerStatus s)
            {
                return s.ToInt32();
            }

            #region Properties

            public States State
            {
                get { return _state; }
                set { _state = value; }
            }

            public int Level
            {
                get { return _level; }
                set { _level = value; }
            }

            #endregion
        }

        #endregion

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
            startState = new ScannerStatus(States.Text, 0);
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

            return ScanLine(new ScannerStatus(state), text, attributes);
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
            string text = Marshal.PtrToStringUni(pszText, length);

            return ScanLine(new ScannerStatus(state), text, null);
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

        #region Scanning

        private static ScannerStatus ScanLine(ScannerStatus status, string text, uint[] colors)
        {
            var lastWord = new StringBuilder();
            int i = 0;

            while (i < text.Length)
            {
                switch (status.State)
                {
                    case States.Text:
                        // go through all the text and search for particular:
                        while (i < text.Length)
                        {
                            if (!char.IsLetterOrDigit(text[i]) && lastWord.Length > 0)
                            {
                                MarkKeyword(colors, i, lastWord.ToString());
                                lastWord.Remove(0, lastWord.Length);
                            }

                            if (IsSingleLineComment(text, i))
                            {
                                status.State = States.SingleLineComment;
                                Advance(colors, ref i, 2, Colors.Comment);
                                break;
                            }

                            if (IsMultiLineCommentStart(text, i))
                            {
                                status.State = States.MultiLineComment;
                                Advance(colors, ref i, 2, Colors.Comment);
                                break;
                            }

                            if (IsStringSingleQuoteStartChar(text, i))
                            {
                                status.State = States.StringSingleQuoted;
                                Advance(colors, ref i, 1, Colors.String);
                                break;
                            }

                            if (IsStringDoubleQuoteStartChar(text, i))
                            {
                                status.State = States.StringDoubleQuoted;
                                Advance(colors, ref i, 1, Colors.String);
                                break;
                            }

                            if (IsNumberStart(text, i))
                            {
                                status.State = States.Number;
                                break;
                            }

                            if (IsOperator(text, i))
                            {
                                SetColor(colors, i, Colors.Operator);
                                i++;
                                continue;
                            }

                            if (char.IsLetterOrDigit(text[i]) && colors != null)
                            {
                                lastWord.Append(text[i]);
                            }

                            SetColor(colors, i, Colors.Text);
                            i++;
                        }

                        break;
                    case States.SingleLineComment:
                        // scan single-line comment, by marking till the end of the line:
                        Advance(colors, ref i, text.Length - i, Colors.Comment);
                        break;
                    case States.MultiLineComment:
                        // scan multi-line comment, by searching the end of the comment:
                        while (i < text.Length)
                        {
                            if (IsMultiLineCommentEnd(text, i))
                            {
                                status.State = States.Text;
                                Advance(colors, ref i, 2, Colors.Comment);
                                break;
                            }

                            SetColor(colors, i, Colors.Comment);
                            i++;
                        }
                        break;
                    case States.StringSingleQuoted:
                        // look for the end of single-quoted string:
                        while (i < text.Length)
                        {
                            if (IsStringSingleQuoteEndChar(text, i))
                            {
                                status.State = States.Text;
                                Advance(colors, ref i, 1, Colors.String);
                                break;
                            }

                            SetColor(colors, i, Colors.String);
                            i++;
                        }
                        break;
                    case States.StringDoubleQuoted:
                        // look for the end of double-quoted string:
                        while (i < text.Length)
                        {
                            if (IsStringDoubleQuoteEndChar(text, i))
                            {
                                status.State = States.Text;
                                Advance(colors, ref i, 1, Colors.String);
                                break;
                            }

                            SetColor(colors, i, Colors.String);
                            i++;
                        }
                        break;
                    case States.Number:
                        // scan text to find end of the number:
                        while (i < text.Length)
                        {
                            if (IsNumber(text, i))
                            {
                                SetColor(colors, i, Colors.Number);
                                i++;
                                continue;
                            }

                            status.State = States.Text;
                            break;
                        }
                        break;
                    default:
                        throw new InvalidDataException("Invalid state, while colorizing QML file");
                }
            }

            switch (status.State)
            {
                case States.SingleLineComment:
                    SetColor(colors, text.Length, Colors.Comment);
                    status.State = States.Text;
                    break;

                case States.MultiLineComment:
                    SetColor(colors, text.Length, Colors.Comment);
                    break;
                default:
                    if (lastWord.Length > 0)
                    {
                        MarkKeyword(colors, text.Length, lastWord.ToString());
                    }

                    SetColor(colors, text.Length, Colors.Text);
                    break;
            }

            return status;
        }

        private static void SetColor(uint[] colors, int index, Colors color)
        {
            if (colors != null)
                colors[index] = (uint) color;
        }

        private static void Advance(uint[] colors, ref int index, int length, Colors color)
        {
            if (colors != null)
            {
                for (int i = 0; i < length; i++, index++)
                {
                    colors[index] = (uint) color;
                }
            }
            else
            {
                index += length;
            }
        }

        private static void MarkKeyword(uint[] colors, int index, string word)
        {
            Colors color;

            if (colors != null && IsKeyword(word, out color))
            {
                for (int i = index - word.Length; i < index; i++)
                {
                    colors[i] = (uint) color;
                }
            }
        }

        private static bool IsSingleLineComment(string text, int index)
        {
            return index + 1 < text.Length && text[index] == '/' && text[index + 1] == '/';
        }

        private static bool IsMultiLineCommentStart(string text, int index)
        {
            return index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*';
        }

        private static bool IsMultiLineCommentEnd(string text, int index)
        {
            return index + 1 < text.Length && text[index] == '*' && text[index + 1] == '/';
        }

        private static bool IsStringSingleQuoteStartChar(string text, int index)
        {
            return index < text.Length && text[index] == '\'';
        }

        private static bool IsStringDoubleQuoteStartChar(string text, int index)
        {
            return index < text.Length && text[index] == '"';
        }

        private static bool IsStringSingleQuoteEndChar(string text, int index)
        {
            return index > 0 && index < text.Length && text[index - 1] != '\\' && text[index] == '\'';
        }

        private static bool IsStringDoubleQuoteEndChar(string text, int index)
        {
            return index > 0 && index < text.Length && text[index - 1] != '\\' && text[index] == '"';
        }

        private static bool IsNumberStart(string text, int index)
        {
            return index < text.Length && IsHexDigit(text[index]) && (index == 0 || (index > 0 && !char.IsLetter(text[index - 1]) && text[index - 1] != '_'));
        }

        private static bool IsNumber(string text, int index)
        {
            return index < text.Length && (IsHexDigit(text[index]) || text[index] == '.');
        }

        private static bool IsHexDigit(char c)
        {
            return char.IsDigit(c); //(c >= '0' && c <= '9') || c == 'x' || c == 'X' || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == 'h' || c == 'H';
        }

        private static bool IsOperator(string text, int index)
        {
            return index < text.Length && Operators.IndexOf(text[index]) >= 0;
        }

        private static bool IsKeyword(string text, out Colors color)
        {
            if (string.IsNullOrEmpty(text))
            {
                color = Colors.Unused;
                return false;
            }

            if (Array.IndexOf(Keywords, text) >= 0)
            {
                color = Colors.Keyword;
                return true;
            }

            if (Array.IndexOf(BasicTypes, text) >= 0)
            {
                color = Colors.Type;
                return true;
            }

            if (Array.IndexOf(CascadeBasicTypes, text) >= 0)
            {
                color = Colors.Signals;
                return true;
            }

            if (Array.IndexOf(CascadesControlTypes, text) >= 0)
            {
                color = Colors.Type;
                return true;
            }

            if (Array.IndexOf(CascadesBasicSignals, text) >= 0)
            {
                color = Colors.Signals;
                return true;
            }

            if (Array.IndexOf(SpecialWords, text) >= 0)
            {
                color = Colors.Identifier;
                return true;
            }

            color = Colors.Unused;
            return false;
        }

        #endregion
    }
}
