using System;
using System.Collections;
using BlackBerry.NativeCore.Debugger;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class contains all information about a variable / expression.
    /// </summary>
    public sealed class VariableInfo
    {
        /// <summary>
        /// Variable's name or expression.
        /// </summary>
        public readonly string _name;

        /// <summary>
        /// Variable's data type.
        /// </summary>
        public string _type;

        /// <summary>
        /// Variable's value or the result of an expression.
        /// </summary>
        public string _value;

        /// <summary>
        /// Variable's name to be used by GDB.
        /// </summary>
        public string _GDBName;

        /// <summary>
        /// List of variable's children.
        /// </summary>
        public ArrayList _children;


        /// <summary>
        /// Evaluate an expression / Get the value of a variable. This method basically send a "-data-evaluate-expression" command to GDB
        /// and evaluate the result.
        /// </summary>
        /// <param name="name"> The expression/variable to be evaluated. </param>
        /// <param name="result"> The result of the expression/ value of variable. </param>
        /// <param name="gdbName"> The GDB Name of the variable. </param>
        /// <returns> A boolean value indicating if the expression evaluation was successful  or not. </returns>
        public static bool EvaluateExpression(string name, out string result, string gdbName)
        {
            if (string.IsNullOrEmpty(name))
            {
                result = string.Empty;
                return false;
            }

            if (name[name.Length - 1] == '.')
                name = name.Remove(name.Length - 1);

            // Waits for the parsed response for the GDB/MI command that evaluates "name" as an expression.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Data-Manipulation.html)
            result = GdbWrapper.SendCommand("-data-evaluate-expression \"" + name + "\"", 2);
            if (result.Substring(0, 2) == "61") // If result starts with 61, it means that there is an error.
            {
                if (gdbName != null) // Maybe that error was caused because GDB didn't accept the VS name. Use the GDBName if there is one.
                {
                    // Gets the parsed response for the GDB/MI command that evaluates "GDBName" as an expression.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Data-Manipulation.html)
                    string result2 = GdbWrapper.SendCommand("-data-evaluate-expression \"" + gdbName + "\"", 2);
                    if (result2.Substring(0, 2) == "60")
                        result = result2;
                }
            }

            bool valid = true;

            if (result.Substring(0, 2) == "61") // If result starts with 61, it means that there is an error. So, expression is not valid.
                valid = false;

            result = result.Substring(3);
            result = result.Substring(1, result.Length - 2); // remove the quotes located in the beginning and at the end
            result = result.Replace("\\\"", "\"");

            if (valid)
            {
                int endString = result.IndexOf("\\\\000");
                int ini, end = 0;
                while (endString != -1)  // remove garbage from strings
                {
                    ini = result.IndexOf("\"", end);
                    while ((ini > 0) && (result[ini - 1] == '\\'))
                        ini = result.IndexOf("\"", ini + 1);
                    if (ini == -1)
                        break;
                    end = result.IndexOf("\"", ini + 1);
                    while ((end > 0) && (result[end - 1] == '\\'))
                        end = result.IndexOf("\"", end + 1);
                    if (end == -1)
                        break;
                    while ((endString != -1) && (endString < ini))
                        endString = result.IndexOf("\\\\000", endString + 1);
                    if ((endString > ini) && (endString < end))
                    {
                        result = result.Substring(0, endString) + result.Substring(end, (result.Length - end));
                        end = endString;
                        endString = result.IndexOf("\\\\000", end);
                    }
                    end++;
                };
            }
            return valid;
        }

        /// <summary>
        /// Gets the information about a variable/expression.
        /// </summary>
        /// <param name="name"> Variable name / expression. </param>
        /// <param name="eventDispatcher"> The event dispatcher. </param>
        /// <param name="frame"> Current stack frame. </param>
        /// <returns> Return the VariableInfo object. </returns>
        public static VariableInfo Get(string name, EventDispatcher eventDispatcher, AD7StackFrame frame)
        {
            VariableInfo vi = null;
            string search = "";
            string separator = "";
            bool isArray = false;
            bool isRoot = true;

            do
            {
                int dot = name.IndexOf('.');
                int squareBracket = name.IndexOf('[');
                int pos = name.IndexOf("->", StringComparison.Ordinal);
                if (dot == -1)
                    dot = name.Length;
                if (squareBracket == -1)
                    squareBracket = name.Length;
                if (pos == -1)
                    pos = name.Length;
                int stop = dot < squareBracket ? dot : squareBracket;
                stop = stop < pos ? stop : pos;

                search = search + separator + name.Substring(0, stop);
                separator = "";

                if (stop < name.Length)
                {
                    separator = name.Substring(stop, 1);
                    if (separator == "-")
                    {
                        separator = "->";
                        name = name.Substring(stop + 2, name.Length - (stop + 2));
                    }
                    else if (separator == "[")
                    {
                        int aux = name.IndexOf("]");
                        isArray = true;
                        separator = name.Substring(stop, (aux - stop) + 1);
                        name = name.Substring(aux + 1, name.Length - (aux + 1));
                    }
                    else
                        name = name.Substring(stop + 1, name.Length - (stop + 1));
                }
                else
                    name = "";

                if (vi == null)
                {
                    if (frame._locals != null)
                    {
                        foreach (VariableInfo var in frame._locals)
                        {
                            if (var._name == search)
                            { // if the "search" expression is a local variable, it doesn't need to create a VariableInfo object for that.
                                vi = var;
                                break;
                            }
                        }
                    }

                    if (vi == null)
                    {
                        if (frame._arguments != null)
                        {
                            foreach (VariableInfo var in frame._arguments)
                            { // if the "search" expression is an argument, it doesn't need to create a VariableInfo object for that.
                                if (var._name == search)
                                {
                                    vi = var;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    isRoot = false;
                    VariableInfo vi_child = null;
                    if (vi._children != null)
                    {
                        foreach (VariableInfo var in vi._children)
                        {
                            if (var._name == search)
                            {
                                vi_child = var;
                                break;
                            }
                        }
                    }
                    vi = vi_child;
                }
            } while ((vi != null) && ((isArray) || (name != "")));


            if (name != "") // variable not found probably because it is in an expression, so return to the original name to evaluate it.
                search = search + separator + name;

            string result;
            bool valid;
            if (vi == null)
                valid = EvaluateExpression(search, out result, null);
            else
                valid = EvaluateExpression(search, out result, vi._GDBName);
            
            if (vi != null)
            {
                if ((vi._value != result) || (!isRoot))  // if is not root means that it can be expanded...
                {
                    vi._value = result;
                    if (vi._value == null || (vi._type.Contains("*") && (vi._value != "0x0")))
                    {
                        // This is an array, struct, union, or pointer.
                        // Create a variable object so we can list this variable's children.
                        ArrayList GDBNames = new ArrayList();
                        ArrayList VSNames = new ArrayList();
                        bool hasVsNdK;
                        eventDispatcher.CreateVar(vi._name, out hasVsNdK);
                        if (hasVsNdK)
                        {
                            GDBNames.Add("VsNdK_" + vi._name);
                            VSNames.Add(vi._name);
                        }
                        vi._children = new ArrayList();
                        if (vi._type.Contains("struct"))
                            if (vi._type[vi._type.Length - 1] == '*')
                                vi.ListChildren(eventDispatcher, "*", GDBNames, VSNames, hasVsNdK, null);
                            else if (vi._type.Contains("["))
                                vi.ListChildren(eventDispatcher, "struct[]", GDBNames, VSNames, hasVsNdK, null);
                            else
                                vi.ListChildren(eventDispatcher, "struct", GDBNames, VSNames, hasVsNdK, null);
                        else if (vi._type.Contains("["))
                            vi.ListChildren(eventDispatcher, "[]", GDBNames, VSNames, hasVsNdK, null);
                        else if (vi._type.Contains("*"))
                            vi.ListChildren(eventDispatcher, "*", GDBNames, VSNames, hasVsNdK, null);
                        else
                            vi.ListChildren(eventDispatcher, "", GDBNames, VSNames, hasVsNdK, null);
                        eventDispatcher.DeleteVar(vi._name, hasVsNdK);
                    }
                }
            }
            else
            {
                if (!valid)
                    vi = new VariableInfo(search, "", result, null);
                else
                {
                    string aux_exp = search.Replace(" ", "");
                    string datatype;

                    // Sending 2 GDB commands to get the data type of "aux_exp" because it is not known, at this moment, if "aux_exp"
                    // is an expression, a primitive or a compound variable.

                    // Gets the parsed response for the GDB command that returns the data type of "aux_exp".
                    // (http://sourceware.org/gdb/onlinedocs/gdb/Symbols.html)
                    string firstDatatype = GdbWrapper.SendCommand("whatis " + aux_exp, 3);

                    // Gets the parsed response for the GDB command that returns a detailed description of the type.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/Symbols.html)
                    string baseDatatype = GdbWrapper.SendCommand("ptype " + aux_exp, 4);

                    if ((baseDatatype[baseDatatype.Length - 1] == '{') && (baseDatatype[baseDatatype.Length - 2] == ' '))
                        baseDatatype = baseDatatype.Remove(baseDatatype.Length - 2);
                    if (baseDatatype.Length < firstDatatype.Length)
                    {
                        if (firstDatatype.Contains(baseDatatype))
                        {
                            baseDatatype = firstDatatype;
                        }
                    }
                    if ((baseDatatype == firstDatatype) || ((baseDatatype.Contains("::")) && (!baseDatatype.Contains("union"))))
                    {
                        baseDatatype = "";
                        datatype = firstDatatype;
                    }
                    else
                    {
                        datatype = baseDatatype;
                    }
                    if (datatype[datatype.Length - 1] == '*')
                        if (result == "0x0")
                        {
                            vi = new VariableInfo(search, firstDatatype, result, null);
                        }
                        else
                        {
                            vi = new VariableInfo(search, firstDatatype, baseDatatype, result, eventDispatcher, null, null);
                        }
                    else if ((datatype.Contains("struct")) || (datatype.Contains("[")))
                    {
                        vi = new VariableInfo(search, firstDatatype, baseDatatype, null, eventDispatcher, null, null);
                    }
                    else
                    {
                        vi = new VariableInfo(search, firstDatatype, result, null);
                    }
                }
            }
            return vi;
        }

        /// <summary>
        /// Call the right VariableInfo constructor for locals and arguments.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type"> The data type of the variable. </param>
        /// <param name="value"> The value of the variable. </param>
        /// <param name="dispatcher"> The event dispatcher. </param>
        /// <returns> Return the VariableInfo created object. </returns>
        public static VariableInfo Create(string name, string type, string value, EventDispatcher dispatcher)
        {
            VariableInfo newVar = new VariableInfo(name, type, "", value, dispatcher, null, null);
            return newVar;
        }

        /// <summary>
        /// Constructor for Variable Info Object without inquiring for the variable's children.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type"> The data type of the variable. </param>
        /// <param name="value"> The value of the variable. </param>
        /// <param name="gdbName"> The GDB Name of the variable. </param>
        public VariableInfo(string name, string type, string value, string gdbName)
        {
            _name = name;
            _type = type;
            _value = value;
            _children = null;
            _GDBName = gdbName;
        }

        /// <summary>
        /// Constructor for Variable Info Object inquiring for the variable's children
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type"> The data type of the variable. </param>
        /// <param name="baseType"> The base type of the variable. </param>
        /// <param name="value"> The value of the variable. </param>
        /// <param name="dispatcher"> The event dispatcher. </param>
        /// <param name="gdbNames"> The names of the variables used by GDB. </param>
        /// <param name="vsNames"> The Names of the variables used by VS. </param>
        public VariableInfo(string name, string type, string baseType, string value, EventDispatcher dispatcher, ArrayList gdbNames, ArrayList vsNames)
        {
            // numChildren - The result of the createvar returns ERROR or a number from 0 to "n" where "n" is the number of children of the variable.

            _name = name;
            _type = type;
            _value = value;
            _children = null;
            _GDBName = null;

            if (baseType != "")
                type = baseType;

            if (gdbNames == null)
            {
                gdbNames = new ArrayList();
                vsNames = new ArrayList();
            }

            if (value == null || (type.Contains("*") && (value != "0x0")))
            {
                // This is an array, struct, union, or pointer.
                // Create a variable object so we can list this variable's children.

                // Some VS variable names cannot be used by GDB. When that happens, it is added the prefix VsNdK_ to the GDB variable 
                // name and it is stored in the GDBNames array. At the same time, the VS name is stored in the VSNames array using the 
                // same index position. This bool variable just indicate if this prefix is used or not.
                bool hasVsNdK; 
                string numChildren = dispatcher.CreateVar(_name, out hasVsNdK);

                if (hasVsNdK)
                {
                    _GDBName = "VsNdK_" + _name;
                    gdbNames.Add("VsNdK_" + _name);
                    vsNames.Add(_name);
                }

                try // Catch non-numerical data
                {
                    if (Convert.ToInt32(numChildren) > 0) // If the variable has children, evaluate them.
                    {
                        _children = new ArrayList();
                        if (type.Contains("struct"))
                            if (type[type.Length - 1] == '*')
                                ListChildren(dispatcher, "*", gdbNames, vsNames, hasVsNdK, null);
                            else if (type.Contains("["))
                                ListChildren(dispatcher, "struct[]", gdbNames, vsNames, hasVsNdK, null);
                            else
                                ListChildren(dispatcher, "struct", gdbNames, vsNames, hasVsNdK, null);
                        else if (type.Contains("["))
                            ListChildren(dispatcher, "[]", gdbNames, vsNames, hasVsNdK, null);
                        else if (type.Contains("*"))
                            ListChildren(dispatcher, "*", gdbNames, vsNames, hasVsNdK, null);
                        else
                            ListChildren(dispatcher, "", gdbNames, vsNames, hasVsNdK, null);
                    }
                }
                catch (FormatException)
                {

                }

                dispatcher.DeleteVar(_name, hasVsNdK);
            }

            if (value == null)
                EvaluateExpression(name, out _value, null);
        }

        /// <summary>
        /// Gets the list of children for a given variable.
        /// </summary>
        /// <param name="dispatcher"> The event dispatcher. </param>
        /// <param name="parentType"> The variable's parent data type. "*" means it is a pointer; "struct[]" means it is an array of
        /// structures; "struct" means it is a structure; and "[]" means it is an array. </param>
        /// <param name="gdbNames"> The names of the variables used by GDB. </param>
        /// <param name="vsNames"> The Names of the variables used by VS. </param>
        /// <param name="hasVsNdK"> Indicate if the variable name uses or not the prefix "VsNdK_". </param>
        /// <param name="gdbName"> The GDB Name of the variable. </param>
        public void ListChildren(EventDispatcher dispatcher, string parentType, ArrayList gdbNames, ArrayList vsNames, bool hasVsNdK, string gdbName)
        {
            string childListResponse;
            if (gdbName == null)
            {
                if (hasVsNdK)
                {
                    childListResponse = dispatcher.listChildren(_GDBName);
                }
                else
                {
                    childListResponse = dispatcher.listChildren(_name);
                    if ((childListResponse == "ERROR") && (_GDBName != null))
                    {
                        childListResponse = dispatcher.listChildren(_GDBName);
                    }
                }
            }
            else
                childListResponse = dispatcher.listChildren(gdbName);

            if (childListResponse != "ERROR")
            {
                childListResponse = (childListResponse.Substring(3)).Replace("#;;;", "");
                
                string[] childList = childListResponse.Split('#');
                foreach (string childString in childList)
                {
                    string name = null;
                    string type = null;
                    string value = null;
                    int numChildren = 0;
                    bool valid = true;

                    string[] childProperties = childString.Split(';');

                    if (childProperties[0] == "")
                        continue;

                    name = childProperties[0];

                    if (name.Contains("::")) // discard this GDB expression.
                    {
                        continue;
                    }

                    gdbName = name;

                    int end = name.Length;
                    if (name[end - 1] == '"')
                        end--;
                    if (((name.Length > 8) && (name.Substring(end - 8, 8) == ".private")) || 
                        ((name.Length > 7) && (name.Substring(end - 7, 7) == ".public")) || 
                        ((name.Length > 10) && (name.Substring(end - 10, 10) == ".protected")) || 
                        ((name.Length > 9) && (name.Substring(end - 9, 9) == ".private.")) || 
                        ((name.Length > 8) && (name.Substring(end - 8, 8) == ".public.")) || 
                        ((name.Length > 11) && (name.Substring(end - 11, 11) == ".protected.")))
                    {
                        // GDB is using an intermediate representation for the variable. Inquire GDB again using this intermediate name.
                        int index = vsNames.IndexOf(_name);
                        if (index != -1)
                            gdbNames[index] = gdbName;
                        else
                        {
                            gdbNames.Add(gdbName);
                            vsNames.Add(_name);
                        }
                        ListChildren(dispatcher, parentType, gdbNames, vsNames, hasVsNdK, gdbName);
                        continue;
                    }
                    else
                    {
                        int dot = name.LastIndexOf(".");
                        if (dot != -1)
                        {
                            int index = gdbNames.IndexOf(name.Substring(0, dot));
                            if (index != -1)
                            {
                                name = vsNames[index].ToString() + name.Substring(dot);
                            }
                        }

                        name = name.Replace(".private", "");
                        name = name.Replace(".public", "");
                        name = name.Replace(".protected", "");
                        name = name.Replace("..", ".");

                        dot = name.LastIndexOf(".*");
                        if (dot != -1)
                        {
                            name = "*(" + name.Remove(dot) + ")";
                        }

                        if (parentType == "*")
                        {
                            dot = name.LastIndexOf('.');
                            if (dot != -1)
                            {
                                name = name.Substring(0, dot) + "->" + name.Substring(dot + 1, name.Length - (dot + 1));
                            }
                        }
                        else if (parentType == "[]")
                        {
                            dot = name.LastIndexOf('.');
                            name = name.Substring(0, dot) + "[" + name.Substring(dot + 1, name.Length - (dot + 1));
                            name = name + "]";
                        }
                        gdbNames.Add(gdbName);
                        vsNames.Add(name);
                    }

                    if (childProperties[1] != "")
                        numChildren = Convert.ToInt32(childProperties[1]);

                    value = childProperties[2];
                    if ((value == "") || (value.Contains("{...}")) || 
                        ((value.Length >= 2) && (value[0] == '[') && (value[value.Length - 1] == ']')))
                        valid = EvaluateExpression(name, out value, gdbName);

                    type = childProperties[3];
                                        
                    VariableInfo child = new VariableInfo(name, type, value, gdbName);

                    if ((valid) && (numChildren > 0 && value != "0x0"))
                    {
                        child._children = new ArrayList();
                    }
                    if (vsNames.Contains(name))
                    {
                        int index = vsNames.IndexOf(name);
                        vsNames.RemoveAt(index);
                        gdbNames.RemoveAt(index);
                    }
                    _children.Add(child); // If VS knows that there are children, it will inquiries again if those children data 
                    // were not filled.
                }
            }
        }
    }
}