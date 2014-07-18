using System;
using System.Diagnostics;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class representing single GDB instruction.
    /// </summary>
    [DebuggerDisplay("{ID}: {Command}")]
    public sealed class Instruction
    {
        public Instruction(int id, string command, bool expectsParameter, string parsing)
        {
            ID = id;
            Command = command;
            ExpectsParameter = expectsParameter;
            Parsing = parsing;
        }

        #region Properties

        /// <summary>
        /// Gets the ID of the instruction.
        /// </summary>
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command text expected to be send from GDB.
        /// </summary>
        public string Command
        {
            get;
            private set;
        }

        public bool ExpectsParameter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the response parsing instruction.
        /// </summary>
        public string Parsing
        {
            get;
            private set;
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Loads data using following format:
        ///     {$}gdb-command:->:parsing-instruction;
        /// It must have the $ sign in front of the GDB command when it is needed to store the command parameters to be used later.
        /// This usually happens whenever the GDB response for a given command does not contains enough information, like 
        /// only "^done" for example, when the parser needs something else.
        /// The ":->:" separates the GDB command from the parsing Instruction.
        /// </summary>
        public static Instruction Load(int id, string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            int separatorIndex = text.IndexOf(":->:", StringComparison.Ordinal);
            if (separatorIndex < 0)
                return new Instruction(id, string.Empty, false, text);

            if (text[0] != '$')
                return new Instruction(id, text.Substring(0, separatorIndex), false, text.Substring(separatorIndex + 4));
            return new Instruction(id, text.Substring(1, separatorIndex - 1), true, text.Substring(separatorIndex + 4));
        }

        #endregion

        #region Parsing - PH: just copied from GDBParserAPI.cpp, gdb-connect.cpp and converted to C# - it's not MY CODE!

        /// <summary>
        /// Function used to call the parser that will process a GDB response according to the stored parsing instruction, returning this parsed response.
        /// </summary>
        /// <param name="response">Unprocessed GDB response</param>
        public string Parse(string response)
        {
            return Parse(response, Parsing, 0, false, new string[10], "#");
        }

        /// <summary>
        /// Responsible for parsing each GDB response. Depending on the parsing instruction, the parser can get the first occurrence  
        /// from GDB response or get all of them that satisfies this parsing instruction. 
        /// </summary>
        public string Parse(string response, int respBegin, bool repeat, string[] variables, string separator)
        {
            return Parse(response, Parsing, respBegin, repeat, variables, separator);
        }

        /// <summary>
        /// Responsible for parsing each GDB response. Depending on the parsing instruction, the parser can get the first occurrence  
        /// from GDB response or get all of them that satisfies this parsing instruction. 
        /// </summary>
        /// <param name="response">GDB response</param>
        /// <param name="respBegin">Current character to be read in GDB response</param>
        /// <param name="parsingInstruction">Instruction used to parse the respective GDB response.</param>
        /// <param name="repeat">If the parsing instruction specifies that it has to get all occurrences from a GDB response, this value must be true. </param>
        /// <param name="variables"> A variable stores a string parsed from GDB response and can be used as many times as needed to create the parsed response. </param>
        /// <param name="separator"> If the parsing instruction allows the parser to get all occurrences from GDB response, this separator will 
        /// be used at the end of each occurrence. The default one is '#' but it can be specified in the parsing instruction.</param>
        /// <returns>Returns the parsed GDB response.</returns>
        private static string Parse(string response, string parsingInstruction, int respBegin, bool repeat, string[] variables, string separator)
        {
            if (variables == null)
                throw new ArgumentNullException("variables");
            if (variables.Length < 10)
                throw new ArgumentOutOfRangeException("variables");
            if (string.IsNullOrEmpty(response))
                throw new ArgumentNullException("response");

            string result = "";         // This variable will be the parsed GDB response.
            int parsePos = 0;           // Current character to be read in parsing instruction.
            int home = respBegin;       // Save the current position in GDB response.
            int respEnd;
            int limit = response.Length;
            bool found = false;
            string originalParsingInstruction = parsingInstruction;

            bool removeSelection = false;
            int removeFromHere = -2;    // -2 - not set; -1 - indicates that it will be set in the first '?' instruction; >=0 - already set.

            if (separator == "$EOL$")
                separator = "\r\n";

            while (parsePos < parsingInstruction.Length || repeat)
            {
                if (repeat && parsePos >= parsingInstruction.Length)  // repeat instructions till the end of the response
                {
                    if (found)
                    {
                        if (result != "")
                            result += separator;
                        parsePos = 0;
                        parsingInstruction = originalParsingInstruction;
                        limit = response.Length;
                        found = false;
                    }
                    else
                        repeat = false;
                    continue;
                }

                if (parsingInstruction[parsePos] == '(')  // repeat instructions till the end of the response
                {
                    int end = parsePos;
                    string repeatInstruction;

                    end = FindClosing(parsingInstruction, '(', ')', parsePos);

                    repeatInstruction = parsingInstruction.Substring(parsePos + 1, end - (parsePos + 1));

                    end++;
                    if (parsingInstruction[end] == ':')
                    {
                        parsePos = end + 1;
                        end = GetNextChar(parsingInstruction, ';', parsePos);
                        separator = parsingInstruction.Substring(parsePos, end - parsePos);
                        int aux = separator.IndexOf("\\;");
                        if (aux != -1)
                        {
                            separator = separator.Remove(aux, 1);
                        }
                    }
                    parsePos = end + 1;

                    result += Parse(response, repeatInstruction, respBegin, true, variables, separator);
                    continue;
                }

                if (parsingInstruction[parsePos] == '?') // search and set the beginning of the searched string.
                {
                    int times = 1;
                    bool forward = true;
                    int end;
                    int aux;
                    string txt;

                    parsePos++;
                    if (parsingInstruction[parsePos] == '%') // consider this position to extract and use the data and them remove this one from the GDB response string
                    {
                        removeFromHere = -1;
                        parsePos++;
                    }

                    if (parsingInstruction[parsePos] == '<') // means to search backwards.
                    {
                        forward = false;
                        parsePos++;
                    }

                    if (parsingInstruction[parsePos] != '?') // the number of times to search for a given string.
                    {
                        end = GetNextChar(parsingInstruction, '?', parsePos);
                        times = int.Parse(parsingInstruction.Substring(parsePos, end - parsePos));
                        parsePos = end;
                    }
                    parsePos++;

                    end = GetNextChar(parsingInstruction, ';', parsePos);
                    txt = SubstituteVariables(parsingInstruction.Substring(parsePos, end - parsePos), variables);

                    aux = txt.IndexOf('\\');
                    while (aux != -1)
                    {
                        // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
                        txt = txt.Substring(0, aux) + txt.Substring(aux + 1, txt.Length - (aux + 1));
                        aux = txt.IndexOf('\\', aux + 1);
                    };

                    int previousRespBegin = respBegin;
                    respBegin = SearchResponse(response, txt, respBegin, times, forward, '?');

                    // check if it was defined a set of instructions to be executed if the search is NOT valid, i.e., there is a '{' after the ';'.
                    // If the search is not valid, go to the '}' and keep going. If there is no '{}' and the search is not valid, just end evaluating
                    // these instructions. Normally, there is '{}' to allow handling gdb errors.
                    if (parsingInstruction[end + 1] == '{')
                    {
                        parsePos = end + 1;

                        end = FindClosing(parsingInstruction, '{', '}', parsePos);
                        if ((respBegin != -1) && (respBegin <= limit))
                        {
                            found = true;
                            // the following instructions to be evaluated are between '{' and '}'.
                            if (end + 1 < parsingInstruction.Length && parsingInstruction[end + 1] != '{')
                            {
                                parsingInstruction = parsingInstruction.Substring(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.Substring(end + 2, parsingInstruction.Length - (end + 2));
                            }
                            else
                            {
                                // if there is '{' after a '}', it means that there is an 'else' sentence.
                                int else_end;
                                else_end = FindClosing(parsingInstruction, '{', '}', end + 1);
                                parsingInstruction = parsingInstruction.Substring(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.Substring(else_end + 2, parsingInstruction.Length - (else_end + 2));
                            }

                            parsePos = 0;
                            if (removeFromHere == -1)
                            {
                                removeFromHere = respBegin - txt.Length;
                            }
                        }
                        else
                        {
                            respBegin = previousRespBegin;
                            removeFromHere = -2;
                            if (end + 1 < parsingInstruction.Length && parsingInstruction[end + 1] != '{')
                            {
                                parsePos = end + 2; // move it to the next instruction, after the "};"
                            }
                            else
                            {
                                parsePos = end + 1;
                                end = FindClosing(parsingInstruction, '{', '}', parsePos);
                                parsingInstruction = parsingInstruction.Substring(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.Substring(end + 2, parsingInstruction.Length - (end + 2));
                                parsePos = 0;
                            }
                        }
                    }
                    else
                    {
                        if ((respBegin == -1) || (respBegin > limit))
                        {
                            if (respBegin > limit)
                                respBegin = previousRespBegin;

                            parsePos = parsingInstruction.Length;
                            removeFromHere = -2;
                            removeSelection = false;
                        }
                        else
                        {
                            found = true;
                            parsePos = end + 1;
                            if (removeFromHere == -1)
                            {
                                removeFromHere = respBegin - txt.Length;
                            }
                        }
                    }

                    continue;
                }

                if (parsingInstruction[parsePos] == '@') // till here: search and set the end of the searched string. Return the string between the last ? and this "till here" position
                {
                    int times = 1;
                    int end;
                    string txt;
                    int aux;

                    parsePos++;

                    if (parsingInstruction[parsePos] != '@') // get the number of times to search for a given string.
                    {
                        end = GetNextChar(parsingInstruction, '@', parsePos);
                        times = int.Parse(parsingInstruction.Substring(parsePos, end - parsePos));
                        parsePos = end;
                    }
                    parsePos++;

                    end = GetNextChar(parsingInstruction, ';', parsePos);
                    txt = SubstituteVariables(parsingInstruction.Substring(parsePos, end - parsePos), variables);

                    aux = txt.IndexOf('\\');
                    while (aux != -1)
                    {   // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
                        txt = txt.Substring(0, aux) + txt.Substring(aux + 1, txt.Length - (aux + 1));
                        aux = txt.IndexOf('\\', aux + 1);
                    };

                    respEnd = SearchResponse(response, txt, respBegin, times, true, '@');
                    if ((respEnd == -1) || (respEnd > limit))
                    {
                        result = "";
                        break;
                    }
                    if (!removeSelection)
                        result += response.Substring(respBegin, respEnd - respBegin);

                    respBegin = respEnd;

                    if (removeFromHere >= 0)
                    {
                        response = response.Remove(removeFromHere, respEnd + txt.Length - removeFromHere);
                        respBegin = removeFromHere;
                        removeFromHere = -2;
                        removeSelection = false;
                    }

                    parsePos = end + 1;
                    continue;
                }

                if (parsingInstruction[parsePos] == '#') // insert the following string into the result
                {
                    int end = GetNextChar(parsingInstruction, ';', parsePos);
                    parsePos++;

                    string txt = parsingInstruction.Substring(parsePos, end - parsePos);
                    int aux = txt.IndexOf('\\');
                    while (aux != -1)
                    {
                        // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
                        txt = txt.Substring(0, aux) + txt.Substring(aux + 1, txt.Length - (aux + 1));
                        aux = txt.IndexOf('\\', aux + 1);
                    }

                    result += SubstituteVariables(txt, variables);
                    parsePos = end + 1;
                    continue;
                }

                if (parsingInstruction[parsePos] == '~') // specify a limit, so the search instructions will search till this position.
                {
                    int end = GetNextChar(parsingInstruction, ';', parsePos);
                    parsePos++;

                    string txt = parsingInstruction.Substring(parsePos, end - parsePos);
                    int aux = txt.IndexOf('\\');
                    while (aux != -1)
                    {
                        // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
                        txt = txt.Substring(0, aux) + txt.Substring(aux + 1, txt.Length - (aux + 1));
                        aux = txt.IndexOf('\\', aux + 1);
                    }

                    limit = SearchResponse(response, txt, respBegin, 1, true, '~');
                    if (limit == -1)
                    {
                        limit = response.Length;
                    }

                    parsePos = end + 1;
                    continue;
                }

                if (parsingInstruction[parsePos] == '0') // set the response cursor position to home_pos (normally 0, but '(' can set a different 
                // value for home_pos)
                {
                    respBegin = home;
                    parsePos += 2; // jump the '0' and ';' characters
                    continue;
                }

                if (parsingInstruction[parsePos] == '%') // delete the string (between ? and @) from response
                {
                    removeSelection = true;
                    removeFromHere = -1;
                    parsePos++;
                    continue;
                }

                if (parsingInstruction[parsePos] == '$') // create a variable to store a value from response string. This value is defined by the instructions between the '=' and the @ symbol (till here)
                {
                    int end, varNumber, aux;

                    parsePos++;
                    end = GetNextChar(parsingInstruction, '=', parsePos);
                    aux = GetNextChar(parsingInstruction, '$', parsePos);
                    if ((aux > 0) && (aux < end))
                        end = parsingInstruction.Length;
                    if (end < parsingInstruction.Length) // variable assignment
                    {
                        varNumber = int.Parse(parsingInstruction.Substring(parsePos, end - parsePos));

                        parsePos = end + 1;
                        end = parsingInstruction.IndexOf("$$", parsePos);
                        string r = Parse(response, parsingInstruction.Substring(parsePos, end - parsePos), respBegin, false, variables, "#");
                        if (r == "")
                            break;
                        variables[varNumber] = r;

                        parsePos = end + 3; // jump $$;
                    }
                    else // pre-defined variable: Finish the parsing task or move to the end of the GDB response.
                    {
                        end = parsingInstruction.IndexOf('$', parsePos);
                        if (parsingInstruction.Substring(parsePos, end - parsePos) == "END")
                            break;
                        if (parsingInstruction.Substring(parsePos, end - parsePos) == "EOR") // move respBegin to the end of the response, probably
                        {
                            // to start looking from the end to the begin.
                            respBegin = response.Length - 1;
                        }
                        parsePos = end + 2;
                    }
                    continue;
                }

            }
            return (result);
        }

        /// <summary> 
        /// Find the position of the associated closing bracket/parenthesis. 
        /// </summary>
        /// <param name="opening"> The associated opening character ("(", "{" or "["). </param>
        /// <param name="closing"> The associated closing character (")", "}" or "]"). </param>
        /// <param name="text"> The string where the search will be made. </param>
        /// <param name="startAt"> Start position in the parsingInstruction string. This position normally corresponds to the position of the opening
        /// character, but it could be smaller than that (never bigger!). However, if it is smaller, it cannot have the same character between
        /// the 'startAt' position and the corresponding one for the opening one. If need to have another one, precede it by the character '\'. </param>
        /// <returns> Returns the position of the associated closing bracket/parenthesis. If it is not found, returns the length of the string. </returns>
        public static int FindClosing(string text, char opening, char closing, int startAt)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            int end = GetNextChar(text, closing, startAt);
            int other = startAt;
            bool found = true;
            do
            {
                other = GetNextChar(text, opening, other + 1);
                if (other < end)
                {
                    end = GetNextChar(text, closing, end + 1);
                }
                else
                    found = false;
            } while (found);
            return end;
        }

        /// <summary> 
        /// Get the next position of a given character (token) in a string message (txt), starting from a given position (pos). 
        /// </summary>
        /// <param name="text"> String message in which it will search for the given character. </param>
        /// <param name="token"> Character to search for. </param>
        /// <param name="startAt"> Starting position in the string message. </param>
        /// <returns> An integer that corresponds to the next position of the character in the string. If the character is not found, returns the length of the string. </returns>
        public static int GetNextChar(string text, char token, int startAt)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            if (startAt > text.Length)
                return text.Length;

            int end = startAt;
            int slash;

            do
            {
                slash = 0;
                end = text.IndexOf(token, startAt);
                if (end == -1)
                    end = text.Length;

                int aux = end - 1;
                while (aux >= 0 && text[aux] == '\\')
                {
                    slash++;
                    aux--;
                }
                startAt = end + 1;
            } while ((slash % 2) != 0); // PH: FIXME: what if there are 4 slashes before ;)

            return end;
        }

        /// <summary> 
        /// Substitute the existing variables in the string "txt" by their values, stored in the "variables" array. Each variable name 
        /// has this format: $9$, where $ characters are used to identify the variable while the number corresponds to the variable ID, that also
        /// corresponds to the array index. There is a special variable "$EOL$" that is substituted by "\r\n".
        /// </summary>
        /// <param name="text">String to search for variables.</param>
        /// <param name="variables">Array with the variable values.</param>
        /// <returns> Returns the new modified string. </returns>
        public static string SubstituteVariables(string text, string[] variables)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            if (variables == null || variables.Length == 0)
                return text;

            do
            {
                int aux = GetNextChar(text, '$', 0);
                if (aux >= 0 && aux < text.Length)
                {
                    aux++;
                    int aux2 = GetNextChar(text, '$', aux);
                    if (aux2 < 0)
                        throw new FormatException("Missing closing $ for a variable definition");

                    string varNumber = text.Substring(aux, aux2 - aux);
                    if (varNumber == "EOL")
                    {
                        text = string.Concat(text.Substring(0, aux - 1), "\r\n", text.Substring(aux2 + 1, text.Length - (aux2 + 1)));
                    }
                    else
                    {
                        int index = int.Parse(varNumber);
                        if (index < 0 || index >= variables.Length)
                            throw new IndexOutOfRangeException("Unable to ask for a parameter value as " + index + " is out of range");

                        text = string.Concat(text.Substring(0, aux - 1), variables[index], text.Substring(aux2 + 1, text.Length - (aux2 + 1)));
                    }
                }
                else
                {
                    break;
                }
            }
            while (true);

            return text;
        }

        /// <summary> 
        /// Get the position of a given string (txt) in the string GDB response (response), starting from a given position (begin). 
        /// </summary>
        /// <param name="response"> GDB string response where the search will be performed. </param>
        /// <param name="txt"> String to search for in the GDB response. </param>
        /// <param name="begin"> Starting position in the GDB string response. </param>
        /// <param name="times"> Search for that string "times" times. Ex: I want to find the third occurrence of word "qaqa" in the GDB response. </param>
        /// <param name="forward"> Direction: if true, search forwards; if not, search backwards. Only '?' instruction can search backwards. </param>
        /// <param name="instruction"> The kind of parsing instruction that called this method. '?', '@', or '~'. Only '?' instruction can 
        /// search backwards. </param>
        /// <returns> An integer that corresponds to the next position of the string in the GDB response. -1 in case of an error. </returns>
        public static int SearchResponse(string response, string txt, int begin, int times, bool forward, char instruction)
        {
            if (string.IsNullOrEmpty(response))
                throw new ArgumentNullException("response");
            if (string.IsNullOrEmpty(txt))
                throw new ArgumentNullException("txt");

            for (; times > 0; times--)
            {
                if (begin >= response.Length || begin == -1)
                    break;
                if (forward)
                {
                    if (begin == 0)
                    {
                        if (txt[0] == '\r' && txt[1] == '\n')
                        {
                            string check = txt.Substring(2, txt.Length - 2);
                            if (response.Substring(0, check.Length) == check)
                                continue;
                        }
                    }
                    begin = response.IndexOf(txt, begin);
                    if (begin != -1 && (times != 1 || instruction == '?'))
                        begin += txt.Length;
                }
                else
                {
                    begin = response.LastIndexOf(txt, begin);
                    if (begin != -1)
                    {
                        if (times == 1)
                            begin += txt.Length;
                        else
                            begin--;
                    }
                    if (begin == -1 && txt[0] == '\r' && txt[1] == '\n')
                        begin = 2; // 2 is the size of "\r\n".
                }
            }
            if (times == 0)
                return begin;
            return -1;
        }

        #endregion
    }
}
