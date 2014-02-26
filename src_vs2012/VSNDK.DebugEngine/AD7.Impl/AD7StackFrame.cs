//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Microsoft.VisualStudio;
using VSNDK.Parser;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This class contains all information about a variable / expression.
    /// </summary>
    public class VariableInfo
    {

        /// <summary>
        /// Variable's name or expression.
        /// </summary>
        public string _name;

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
        /// <param name="GDBName"> The GDB Name of the variable. </param>
        /// <returns> A boolean value indicating if the expression evaluation was successful  or not. </returns>
        public static bool evaluateExpression(string name, ref string result, string GDBName)
        {
            if (name[name.Length - 1] == '.')
                name = name.Remove(name.Length - 1);

            // Waits for the parsed response for the GDB/MI command that evaluates "name" as an expression.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Data-Manipulation.html)
            result = GDBParser.parseCommand("-data-evaluate-expression \"" + name + "\"", 2);
            if (result.Substring(0, 2) == "61") // If result starts with 61, it means that there is an error.
            {
                if (GDBName != null) // Maybe that error was caused because GDB didn't accept the VS name. Use the GDBName if there is one.
                {
                    // Gets the parsed response for the GDB/MI command that evaluates "GDBName" as an expression.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Data-Manipulation.html)
                    string result2 = GDBParser.parseCommand("-data-evaluate-expression \"" + GDBName + "\"", 2);
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
        /// <param name="m_eventDispatcher"> The event dispatcher. </param>
        /// <param name="m_frame"> Current stack frame. </param>
        /// <returns> Return the VariableInfo object. </returns>
        public static VariableInfo get(string name, EventDispatcher m_eventDispatcher, AD7StackFrame m_frame)
        {
            VariableInfo vi = null;
            string search = "";
            string separator = "";
            bool isArray = false;
            bool isRoot = true;

            do
            {
                int dot = name.IndexOf(".");
                int squareBracket = name.IndexOf("[");
                int pos = name.IndexOf("->");
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
                    if (m_frame._locals != null)
                    {
                        foreach (VariableInfo var in m_frame._locals)
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
                        if (m_frame._arguments != null)
                        {
                            foreach (VariableInfo var in m_frame._arguments)
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

            string result = "";
            bool valid;
            if (vi == null)
                valid = evaluateExpression(search, ref result, null);
            else
                valid = evaluateExpression(search, ref result, vi._GDBName);
            
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
                        bool hasVsNdK_ = false;
                        m_eventDispatcher.createVar(vi._name, ref hasVsNdK_);
                        if (hasVsNdK_)
                        {
                            GDBNames.Add("VsNdK_" + vi._name);
                            VSNames.Add(vi._name);
                        }
                        vi._children = new ArrayList();
                        if (vi._type.Contains("struct"))
                            if (vi._type[vi._type.Length - 1] == '*')
                                vi.listChildren(m_eventDispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                            else if (vi._type.Contains("["))
                                vi.listChildren(m_eventDispatcher, "struct[]", GDBNames, VSNames, hasVsNdK_, null);
                            else
                                vi.listChildren(m_eventDispatcher, "struct", GDBNames, VSNames, hasVsNdK_, null);
                        else if (vi._type.Contains("["))
                            vi.listChildren(m_eventDispatcher, "[]", GDBNames, VSNames, hasVsNdK_, null);
                        else if (vi._type.Contains("*"))
                            vi.listChildren(m_eventDispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                        else
                            vi.listChildren(m_eventDispatcher, "", GDBNames, VSNames, hasVsNdK_, null);
                        m_eventDispatcher.deleteVar(vi._name, hasVsNdK_);
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
                    string firstDatatype = GDBParser.parseCommand("whatis " + aux_exp, 3);

                    // Gets the parsed response for the GDB command that returns a detailed description of the type.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/Symbols.html)
                    string baseDatatype = GDBParser.parseCommand("ptype " + aux_exp, 4);

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
                            vi = new VariableInfo(search, firstDatatype, baseDatatype, result, m_eventDispatcher, null, null);
                        }
                    else if ((datatype.Contains("struct")) || (datatype.Contains("[")))
                    {
                        vi = new VariableInfo(search, firstDatatype, baseDatatype, null, m_eventDispatcher, null, null);
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
        public static VariableInfo create(string name, string type, string value, EventDispatcher dispatcher)
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
        /// <param name="GDBName"> The GDB Name of the variable. </param>
        public VariableInfo(string name, string type, string value, string GDBName)
        {
            _name = name;
            _type = type;
            _value = value;
            _children = null;
            _GDBName = GDBName;
        }


        /// <summary>
        /// Constructor for Variable Info Object inquiring for the variable's children
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type"> The data type of the variable. </param>
        /// <param name="baseType"> The base type of the variable. </param>
        /// <param name="value"> The value of the variable. </param>
        /// <param name="dispatcher"> The event dispatcher. </param>
        /// <param name="GDBNames"> The names of the variables used by GDB. </param>
        /// <param name="VSNames"> The Names of the variables used by VS. </param>
        public VariableInfo(string name, string type, string baseType, string value, EventDispatcher dispatcher, ArrayList GDBNames, ArrayList VSNames)
        {
            /// numChildren - The result of the createvar returns ERROR or a number from 0 to "n" where "n" is the number of children 
            /// of the variable.
            string numChildren = ""; 
            
            _name = name;
            _type = type;
            _value = value;
            _children = null;
            _GDBName = null;

            if (baseType != "")
                type = baseType;

            if (GDBNames == null)
            {
                GDBNames = new ArrayList();
                VSNames = new ArrayList();
            }

            if (value == null || (type.Contains("*") && (value != "0x0")))
            {
                // This is an array, struct, union, or pointer.
                // Create a variable object so we can list this variable's children.

                /// Some VS variable names cannot be used by GDB. When that happens, it is added the prefix VsNdK_ to the GDB variable 
                /// name and it is stored in the GDBNames array. At the same time, the VS name is stored in the VSNames array using the 
                /// same index position. This bool variable just indicate if this prefix is used or not.
                bool hasVsNdK_ = false; 

                numChildren = dispatcher.createVar(_name, ref hasVsNdK_);

                if (hasVsNdK_)
                {
                    _GDBName = "VsNdK_" + _name;
                    GDBNames.Add("VsNdK_" + _name);
                    VSNames.Add(_name);
                }

                try // Catch non-numerical data
                {
                    if (Convert.ToInt32(numChildren) > 0) // If the variable has children, evaluate them.
                    {
                        _children = new ArrayList();
                        if (type.Contains("struct"))
                            if (type[type.Length - 1] == '*')
                                this.listChildren(dispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                            else if (type.Contains("["))
                                this.listChildren(dispatcher, "struct[]", GDBNames, VSNames, hasVsNdK_, null);
                            else
                                this.listChildren(dispatcher, "struct", GDBNames, VSNames, hasVsNdK_, null);
                        else if (type.Contains("["))
                            this.listChildren(dispatcher, "[]", GDBNames, VSNames, hasVsNdK_, null);
                        else if (type.Contains("*"))
                            this.listChildren(dispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                        else
                            this.listChildren(dispatcher, "", GDBNames, VSNames, hasVsNdK_, null);
                    }
                }
                catch (FormatException e)
                {

                }

                dispatcher.deleteVar(_name, hasVsNdK_);
            }

            if (value == null)
                evaluateExpression(name, ref _value, null);
        }


        /// <summary>
        /// Gets the list of children for a given variable.
        /// </summary>
        /// <param name="dispatcher"> The event dispatcher. </param>
        /// <param name="parentType"> The variable's parent data type. "*" means it is a pointer; "struct[]" means it is an array of
        /// structures; "struct" means it is a structure; and "[]" means it is an array. </param>
        /// <param name="GDBNames"> The names of the variables used by GDB. </param>
        /// <param name="VSNames"> The Names of the variables used by VS. </param>
        /// <param name="hasVsNdK_"> Indicate if the variable name uses or not the prefix "VsNdK_". </param>
        /// <param name="GDBName"> The GDB Name of the variable. </param>
        public void listChildren(EventDispatcher dispatcher, string parentType, ArrayList GDBNames, ArrayList VSNames, bool hasVsNdK_, string GDBName)
        {
            string childListResponse;
            if (GDBName == null)
            {
                if (hasVsNdK_)
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
                childListResponse = dispatcher.listChildren(GDBName);

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

                    GDBName = name;

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
                        int index = VSNames.IndexOf(_name);
                        if (index != -1)
                            GDBNames[index] = GDBName;
                        else
                        {
                            GDBNames.Add(GDBName);
                            VSNames.Add(_name);
                        }
                        this.listChildren(dispatcher, parentType, GDBNames, VSNames, hasVsNdK_, GDBName);
                        continue;
                    }
                    else
                    {
                        int dot = name.LastIndexOf(".");
                        if (dot != -1)
                        {
                            int index = GDBNames.IndexOf(name.Substring(0, dot));
                            if (index != -1)
                            {
                                name = VSNames[index].ToString() + name.Substring(dot);
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
                        GDBNames.Add(GDBName);
                        VSNames.Add(name);
                    }

                    if (childProperties[1] != "")
                        numChildren = Convert.ToInt32(childProperties[1]);

                    value = childProperties[2];
                    if ((value == "") || (value.Contains("{...}")) || 
                        ((value.Length >= 2) && (value[0] == '[') && (value[value.Length - 1] == ']')))
                        valid = evaluateExpression(name, ref value, GDBName);

                    type = childProperties[3];
                                        
                    VariableInfo child = new VariableInfo(name, type, value, GDBName);

                    if ((valid) && (numChildren > 0 && value != "0x0"))
                    {
                        child._children = new ArrayList();
                    }
                    if (VSNames.Contains(name))
                    {
                        int index = VSNames.IndexOf(name);
                        VSNames.RemoveAt(index);
                        GDBNames.RemoveAt(index);
                    }
                    _children.Add(child); // If VS knows that there are children, it will inquiries again if those children data 
                                          // were not filled.
                }
            }
        }
    }


    /// <summary>
    /// Represents a logical stack frame on the thread stack. 
    /// 
    /// It implements:
    /// 
    /// IDebugStackFrame2: Represents a single stack frame in a call stack in a particular thread.
    /// (http://msdn.microsoft.com/en-us/library/bb161683.aspx)
    /// 
    /// IDebugExpressionContext: Represents a context for expression evaluation, which allows expression evaluation and watch windows.
    /// (http://msdn.microsoft.com/en-ca/library/bb146178.aspx)
    /// </summary>
    public class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        /// <summary>
        /// The class that manages debug events for the debug engine.
        /// </summary>
        public static EventDispatcher m_dispatcher;

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        public readonly AD7Engine m_engine;
        
        /// <summary>
        /// Represents the thread for this stack frame.
        /// </summary>
        public readonly AD7Thread m_thread = null;

        /// <summary>
        ///  The short path file name that contains the source code of this stack frame. 
        /// </summary>
        private string m_documentName = "";
        
        /// <summary>
        /// The function name associated to this stack frame.
        /// </summary>
        public string m_functionName = "";
        
        /// <summary>
        /// Represents the current position (line number) in m_documentName.
        /// </summary>
        private uint m_lineNum = 0;
        
        /// <summary>
        /// Boolean value that indicates if this stack frame has an associated source code to present.
        /// </summary>
        private bool m_hasSource = false;
        
        /// <summary>
        /// The current context's address. 
        /// </summary>
        private uint m_address = 0;
        
        /// <summary>
        /// List of variables that we want to filter from the locals window.
        /// </summary>
        private string[] m_variableFilter = { "__func__" };

        /// <summary>
        /// Contains the locals variables to this stack frame.
        /// </summary>
        public ArrayList _locals;
        
        /// <summary>
        /// Contains the parameters used to call the method/function that originated this stack frame.
        /// </summary>
        public ArrayList _arguments;


        /// <summary>
        /// 
        /// </summary>
        public VariableInfo _lastEvaluatedExpression;


        /// <summary>
        /// Search the __stackframes cache for the internal representation of the stack frame associated to the GDB frameInfo 
        /// information. If successful, returns the stack frame; otherwise, creates a new one and return it.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="thread"> Represents the thread for this stack frame. </param>
        /// <param name="frameInfo">  Array of strings with the information provided by GDB about this stack frame. </param>
        /// <param name="created"> Boolean value that indicates if a new object for this stack frame was created or not. </param>
        /// <returns> Returns the created/found stack frame. </returns>
        public static AD7StackFrame create(AD7Engine engine, AD7Thread thread, string[] frameInfo, ref bool created)
        {
            created = false;
            if (thread.__stackFrames != null)
            {
                foreach (AD7StackFrame frame in thread.__stackFrames)
                {
                    if (frame.m_documentName != null && frame.m_functionName != null)
                    {
                        if (frame.m_documentName == frameInfo[3] && frame.m_functionName == frameInfo[2]) // frameInfo[2] = func, frameInfo[3] = file
                            return frame;
                    }
                }
            }
            else
                thread.__stackFrames = new ArrayList();

            AD7StackFrame newFrame = new AD7StackFrame(engine, thread, frameInfo);
            thread.__stackFrames.Add(newFrame);
            created = true;
            return newFrame;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="thread"> Represents the thread for this stack frame. </param>
        /// <param name="frameInfo"> Array of strings with the information provided by GDB about this stack frame. </param>
        public AD7StackFrame(AD7Engine engine, AD7Thread thread, string[] frameInfo)
        {
            m_engine = engine;
            m_thread = thread;
            m_dispatcher = m_engine.eDispatcher;

            uint level = Convert.ToUInt32(frameInfo[0]);
            string address = frameInfo[1];
            m_functionName = frameInfo[2];
            m_documentName = frameInfo[3];
            try
            {
                m_lineNum = Convert.ToUInt32(frameInfo[4]);
            }
            catch (Exception e)
            {
                m_lineNum = 0;
            }

            _locals = new ArrayList();
            _arguments = new ArrayList();
            m_hasSource = (m_lineNum == 0) ? false : true;
            ArrayList evaluatedVars = new ArrayList();
            
            // Add the variable filter list to the evaluatedVars list.
            // Causes named variables to be ignored.
            evaluatedVars.AddRange(m_variableFilter);

            if (address.StartsWith("0x"))
                address = address.Remove(0, 2);
            m_address = uint.Parse(address, System.Globalization.NumberStyles.AllowHexSpecifier);

            // Query GDB for parameters and locals.
            string variablesResponse = m_engine.eDispatcher.getVariablesForFrame(level, m_thread._id).Replace("#;;;", "");
            if (variablesResponse == null || variablesResponse == "ERROR" || variablesResponse == "")
                return;
            variablesResponse = variablesResponse.Substring(3);

            string[] variableStrings = variablesResponse.Split('#');

            foreach (string variableString in variableStrings)
            {
                string name = null;
                bool arg = false;
                string type = null;
                string value = null;

                string[] variableProperties = variableString.Split(';');

                if (variableProperties[0] != "")
                {
                    if (!evaluatedVars.Contains(variableProperties[0]))
                    {
                        name = variableProperties[0];
                        evaluatedVars.Add(variableProperties[0]);
                        if (variableProperties[1] != "")
                            arg = true;
                        if (variableProperties[2] != "")
                            type = variableProperties[2];
                        if (variableProperties[3] != "")
                            value = variableProperties[3];
                        if (arg)
                            _arguments.Add(VariableInfo.create(name, type, value, m_engine.eDispatcher));
                        else
                            _locals.Add(VariableInfo.create(name, type, value, m_engine.eDispatcher));
                    }
                }
            }
        }

        #region Non-interface methods


        /// <summary>
        /// Construct a FRAMEINFO for this stack frame with the requested information.
        /// </summary>
        /// <param name="dwFieldSpec"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the 
        /// frameInfo parameter are to be filled in. </param>
        /// <param name="frameInfo"> A FRAMEINFO structure that is filled in with the description of the stack frame. </param>
        public void SetFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, out FRAMEINFO frameInfo)
        {
            frameInfo = new FRAMEINFO();

            // The debugger is asking for the formatted name of the function which is displayed in the callstack window.
            // There are several optional parts to this name including the module, argument types and values, and line numbers.
            // The optional information is requested by setting flags in the dwFieldSpec parameter.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME) != 0)
            {
                // If there is source information, construct a string that contains the module name, function name, and optionally argument names and values.
                if (m_hasSource)
                {
                    frameInfo.m_bstrFuncName = "";
                    
                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
//                        frameInfo.m_bstrFuncName = System.IO.Path.GetFileName(module.Name) + "!";
                    }
                    

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_RETURNTYPE) != 0)
                    {
                        // Adds the return type to the m_bstrFuncName field.
                        //frameInfo.m_bstrFuncName += _returnType;
                    }

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LANGUAGE) != 0)
                    {
                        // Adds the language to the m_bstrFuncName field.
                        if (m_documentName.EndsWith(".c"))
                            frameInfo.m_bstrFuncName += "(C) ";
                        else if (m_documentName.EndsWith(".cpp") || m_documentName.EndsWith(".c++"))
                            frameInfo.m_bstrFuncName += "(C++) ";
                    }

                    frameInfo.m_bstrFuncName += m_functionName;

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS) != 0)
                    {
                        // Add the arguments to the m_bstrFuncName field.
                        frameInfo.m_bstrFuncName += "(";
                        bool all = (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_ALL) != 0;
                        int i = 0;
                        foreach (VariableInfo arg in _arguments)
                        {
                            if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES) != 0)
                            {
                                frameInfo.m_bstrFuncName += arg._type + " ";
                            }

                            if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES) != 0)
                            {
                                frameInfo.m_bstrFuncName += arg._name;
                            }

                            if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_VALUES) != 0)
                            {
                                frameInfo.m_bstrFuncName += "=" + arg._value;
                            }

                            if (i < _arguments.Count - 1)
                            {
                                frameInfo.m_bstrFuncName += ", ";
                            }
                            i++;
                        }
                        frameInfo.m_bstrFuncName += ")";
                    }

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES) != 0)
                    {
                        frameInfo.m_bstrFuncName += " Line: " + m_lineNum.ToString();
                    }

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_OFFSET) != 0)
                    {
                        // TODO:
                        // Adds to the m_bstrFuncName field the offset in bytes from the start of the line if FIF_FUNCNAME_LINES is specified.
                        // If FIF_FUNCNAME_LINES is not specified, or if line numbers are not available, adds the offset in bytes from the start of the function.
                    }

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_FORMAT) != 0)
                    {
                        // TODO:
                        // Formats the function name. The result is returned in the m_bstrFuncName field and no other fields are filled out.
                        // According to http://msdn.microsoft.com/en-us/library/bb145138.aspx, this flag "Specify the FIF_FUNCNAME_FORMAT 
                        // flag to format the function name into a single string". This method already works on this way.
                    }
                }
                else
                {
                    // No source information, so only return the module name and the instruction pointer.
                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
                        if ((this.m_functionName != "") && (this.m_functionName != "??"))
                            frameInfo.m_bstrFuncName = this.m_functionName;
                        else
                            frameInfo.m_bstrFuncName = "[External Code]";
                    }
                    else
                    {
                        frameInfo.m_bstrFuncName = "[External Code]";
                    }
                    
                }
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME;
            }

            // The debugger is requesting the name of the module for this stack frame.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_MODULE) != 0)
            {
                frameInfo.m_bstrModule = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_MODULE;
            }
            

            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE) != 0)
            {
                // TODO:
                // Initialize/use the m_bstrReturnType field.
                frameInfo.m_bstrReturnType = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE;
            }

            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS) != 0)
            {
                // Initialize/use the m_bstrArgs field.
                frameInfo.m_bstrArgs = "";
                bool all = (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_ALL) != 0;
                int i = 0;
                foreach (VariableInfo arg in _arguments)
                {
                    if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_TYPES) != 0)
                    {
                        frameInfo.m_bstrArgs += arg._type + " ";
                    }

                    if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_NAMES) != 0)
                    {
                        frameInfo.m_bstrArgs += arg._name;
                    }

                    if (all || (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_VALUES) != 0)
                    {
                        frameInfo.m_bstrArgs += "=" + arg._value;
                    }

                    if (i < _arguments.Count - 1)
                    {
                        frameInfo.m_bstrArgs += ", ";
                    }
                    i++;
                }

                if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_NO_TOSTRING) != 0)
                {
                    // TODO:
                    // Do not allow ToString() function evaluation or formatting when returning function arguments.
                }

                if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_NO_FUNC_EVAL) != 0)
                {
                    // TODO:
                    // Specifies that function (property) evaluation should not be used when retrieving argument values.
                }

                if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_ARGS_NOFORMAT) != 0)
                {
                    // TODO:
                    // Specifies that the arguments are not be formatted (for example, do not add opening and closing parentheses around 
                    // the argument list nor add a separator between arguments).
                }

                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_ARGS;
            }

            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0)
            {
                // Initialize/use the m_bstrLanguage field.
                if (m_documentName != null)
                {
                    if (m_documentName.EndsWith(".c"))
                        frameInfo.m_bstrLanguage = "C";
                    else if (m_documentName.EndsWith(".cpp") || m_documentName.EndsWith(".c++"))
                        frameInfo.m_bstrLanguage = "C++";
                    else
                        frameInfo.m_bstrLanguage = "";
                }
                else
                    frameInfo.m_bstrLanguage = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_LANGUAGE;
            }

            // The debugger would like a pointer to the IDebugModule2 that contains this stack frame.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP) != 0)
            {
                frameInfo.m_pModule = m_engine.m_module;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP;
            }

            // The debugger is requesting the range of memory addresses for this frame.
            // For the sample engine, this is the contents of the frame pointer.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_STACKRANGE) != 0)
            {
                // TODO:
                // Initialize/use the m_addrMin and m_addrMax (stack range) fields.

                frameInfo.m_addrMin = 0;
                frameInfo.m_addrMax = 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STACKRANGE;
            }

            // The debugger is requesting the IDebugStackFrame2 value for this frame info.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FRAME) != 0)
            {
                frameInfo.m_pFrame = this;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FRAME;
            }

            // Does this stack frame of symbols loaded?
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO) != 0)
            {
                frameInfo.m_fHasDebugInfo = m_hasSource ? 1 : 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
            }

            // Is this frame stale?
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_STALECODE) != 0)
            {
                frameInfo.m_fStaleCode = 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
            }

            // The debug engine is to filter non-user code frames so they are not included.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FILTER_NON_USER_CODE ) != 0)
            {
            }

            // Frame information should be gotten from the hosted app-domain rather than the hosting process.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DESIGN_TIME_EXPR_EVAL) != 0)
            {
            }

        }


        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the combined locals and parameters.
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        public void CreateLocalsPlusArgsProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = 0;
            int localsLength = 0;

            if (_locals != null)
            {
                localsLength = _locals.Count;
                elementsReturned += (uint)localsLength;
            }

            if (_arguments != null)
            {
                elementsReturned += (uint)_arguments.Count;
            }
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[elementsReturned];

            if (_locals != null)
            {
                int i = 0;
                foreach(VariableInfo var in _locals)
                {
                    AD7Property property = new AD7Property(var);
                    propInfo[i] = property.ConstructDebugPropertyInfo(dwFields);
                    i++;
                }
            }

            if (_arguments != null)
            {
                int i = 0;
                foreach (VariableInfo arg in _arguments)
                {
                    AD7Property property = new AD7Property(arg);
                    propInfo[localsLength + i] = property.ConstructDebugPropertyInfo(dwFields);
                    i++;
                }
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }


        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the locals collection only.
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        private void CreateLocalProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = (uint)_locals.Count;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[_locals.Count];

            int i = 0;
            foreach (VariableInfo var in _locals)
            {
                AD7Property property = new AD7Property(var);
                propInfo[i] = property.ConstructDebugPropertyInfo(dwFields);
                i++;
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }


        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the parameters collection only.
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        private void CreateParameterProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = (uint)_arguments.Count;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[_arguments.Count];

            int i = 0;
            foreach (VariableInfo arg in _arguments)
            {
                AD7Property property = new AD7Property(arg);
                propInfo[i] = property.ConstructDebugPropertyInfo(dwFields);
                i++;
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }

        #endregion

        #region IDebugStackFrame2 Members


        /// <summary>
        /// Creates an enumerator for properties associated with the stack frame, such as local variables.
        /// (http://msdn.microsoft.com/en-us/library/bb145607.aspx).
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumerated DEBUG_PROPERTY_INFO structures are to be filled in. </param>
        /// <param name="nRadix"> The radix to be used in formatting any numerical information. </param>
        /// <param name="guidFilter"> A GUID of a filter used to select which DEBUG_PROPERTY_INFO structures are to be enumerated, 
        /// such as guidFilterLocals. </param>
        /// <param name="dwTimeout"> Maximum time, in milliseconds, to wait before returning from this method. Use INFINITE to wait 
        /// indefinitely. </param>
        /// <param name="elementsReturned"> Returns the number of properties enumerated. This is the same as calling the 
        /// IEnumDebugPropertyInfo2::GetCount method. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = 0;
            enumObject = null;

            try
            {
                if (guidFilter == AD7Guids.guidFilterLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocals)
                {
                    CreateLocalsPlusArgsProperties(dwFields, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                else if (guidFilter == AD7Guids.guidFilterLocals)
                {
                    CreateLocalProperties(dwFields, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                else if (guidFilter == AD7Guids.guidFilterArgs)
                {
                    CreateParameterProperties(dwFields, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                else
                {
                    return VSConstants.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Gets the code context for this stack frame. The code context represents the current instruction pointer in this stack frame.
        /// (http://msdn.microsoft.com/en-us/library/bb147046.aspx)
        /// </summary>
        /// <param name="memoryAddress"> Returns an IDebugCodeContext2 object that represents the current instruction pointer in this stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 memoryAddress)
        {
            memoryAddress = null;
            try
            {
                memoryAddress = new AD7MemoryAddress(m_engine, m_address);
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Gets a description of the properties associated with a stack frame.
        /// (http://msdn.microsoft.com/en-us/library/bb144920.aspx)
        /// </summary>
        /// <param name="property"> Returns an IDebugProperty2 object that describes the properties of this stack frame. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 property)
        {
            property = new AD7Property(this);
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the document context for this stack frame. The debugger will call this when the current stack frame is changed and 
        /// will use it to open the correct source document for this stack frame. (http://msdn.microsoft.com/en-us/library/bb146338.aspx)
        /// </summary>
        /// <param name="docContext"> Returns an IDebugDocumentContext2 object that represents the current position in a source 
        /// document. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 docContext)
        {
            this.m_engine.cleanEvaluatedThreads();
            docContext = null;

            try
            {
                // Assume all lines begin and end at the beginning of the line.
                TEXT_POSITION begTp = new TEXT_POSITION();
                begTp.dwColumn = 0;
                if (m_lineNum != 0)
                    begTp.dwLine = m_lineNum - 1;
                else
                    begTp.dwLine = 0;
                TEXT_POSITION endTp = new TEXT_POSITION();
                endTp.dwColumn = 0;
                if (m_lineNum != 0)
                    endTp.dwLine = m_lineNum - 1;
                else
                    endTp.dwLine = 0;

                docContext = new AD7DocumentContext(m_documentName, begTp, endTp, null);
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return VSConstants.S_FALSE;
        }


        /// <summary>
        /// Gets an evaluation context for expression evaluation within the current context of a stack frame and thread.
        /// Generally, an expression evaluation context can be thought of as a scope for performing expression evaluation. 
        /// Call the IDebugExpressionContext2::ParseText method to parse an expression and then call the resulting 
        /// IDebugExpression2::EvaluateSync or IDebugExpression2::EvaluateAsync methods to evaluate the parsed expression.
        /// (http://msdn.microsoft.com/en-us/library/bb161269.aspx)
        /// </summary>
        /// <param name="ppExprCxt"> Returns an IDebugExpressionContext2 object that represents a context for expression evaluation. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
        {
            ppExprCxt = (IDebugExpressionContext2)this;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets a description of the stack frame. (http://msdn.microsoft.com/en-us/library/bb145146.aspx)
        /// </summary>
        /// <param name="dwFieldSpec"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the 
        /// pFrameInfo parameter are to be filled in. </param>
        /// <param name="nRadix"> The radix to be used in formatting any numerical information. </param>
        /// <param name="pFrameInfo"> A FRAMEINFO structure that is filled in with the description of the stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, FRAMEINFO[] pFrameInfo)
        {
            try
            {
                SetFrameInfo(dwFieldSpec, out pFrameInfo[0]);

                int frame = 0;
                if (this.m_thread.__stackFrames != null)
                {
                    foreach (AD7StackFrame sf in this.m_thread.__stackFrames)
                    {
                        if (sf.m_functionName == this.m_functionName)
                            break;
                        frame++;
                    }
                }

                if (this.m_thread._id != this.m_engine.currentThread()._id)
                    this.m_engine.eDispatcher.selectThread(this.m_thread._id);

                // Waits for the parsed response for the GDB/MI command that changes the selected frame.
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
                GDBParser.parseCommand("-stack-select-frame " + frame, 5);

                if (this.m_thread._id != this.m_engine.currentThread()._id)
                    this.m_engine.eDispatcher.selectThread(this.m_engine.currentThread()._id);

                this.m_engine.cleanEvaluatedThreads();
                
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Gets the language associated with this stack frame. (http://msdn.microsoft.com/en-us/library/bb145096.aspx)
        /// </summary>
        /// <param name="pbstrLanguage"> Returns the name of the language that implements the method associated with this stack frame. </param>
        /// <param name="pguidLanguage"> Returns the GUID of the language. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            if (m_documentName.EndsWith(".c"))
            {
                pbstrLanguage = "C";
                pguidLanguage = AD7Guids.guidLanguageCpp;
            }
            else if (m_documentName.EndsWith(".cpp") || m_documentName.EndsWith(".c++"))
            {
                pbstrLanguage = "C++";
                pguidLanguage = AD7Guids.guidLanguageCpp;
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the name of the stack frame. The name of a stack frame is typically the name of the method being executed.
        /// Not implemented. (http://msdn.microsoft.com/en-us/library/bb145002.aspx)
        /// </summary>
        /// <param name="name"> Returns the name of the stack frame. </param>
        /// <returns> Not implemented. </returns>
        int IDebugStackFrame2.GetName(out string name)
        {
            name = null;

            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets a machine-dependent representation of the range of physical addresses associated with a stack frame.
        /// Not implemented. (http://msdn.microsoft.com/en-us/library/bb145597.aspx)
        /// </summary>
        /// <param name="addrMin"> Returns the lowest physical address associated with this stack frame. </param>
        /// <param name="addrMax"> Returns the highest physical address associated with this stack frame. </param>
        /// <returns> Not implemented. </returns>
        int IDebugStackFrame2.GetPhysicalStackRange(out ulong addrMin, out ulong addrMax)
        {
            addrMin = 0;
            addrMax = 0;

            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets the thread associated with a stack frame. (http://msdn.microsoft.com/en-us/library/bb161776.aspx)
        /// </summary>
        /// <param name="thread"> Returns an IDebugThread2 object that represents the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetThread(out IDebugThread2 thread)
        {
            thread = m_thread;
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugExpressionContext2 Members


        /// <summary>
        /// Retrieves the name of the evaluation context. The name is the description of this evaluation context. It is typically 
        /// something that can be parsed by an expression evaluator that refers to this exact evaluation context. For example, in 
        /// C++ the name is as follows:  "{ function-name, source-file-name, module-file-name }"
        /// Not implemented. (http://msdn.microsoft.com/en-ca/library/bb161724.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the evaluation context. </param>
        /// <returns> Not implemented. </returns>
        int IDebugExpressionContext2.GetName(out string pbstrName)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Parses a text-based expression for evaluation. (http://msdn.microsoft.com/en-ca/library/bb162304.aspx).
        /// GDB will parse and evaluate the expression, returning the result or an error in the expression. So, the only task for this
        /// method is to create the IDebugExpression2 object that will be sent indirectly to the methods responsible for the evaluation.
        /// </summary>
        /// <param name="pszCode"> The expression to be parsed. </param>
        /// <param name="dwFlags"> A combination of flags from the PARSEFLAGS enumeration that controls parsing. </param>
        /// <param name="nRadix"> The radix to be used in parsing any numerical information in pszCode. </param>
        /// <param name="ppExpr"> Returns the IDebugExpression2 object that represents the parsed expression, which is ready for 
        /// binding and evaluation. </param>
        /// <param name="pbstrError"> Returns the error message if the expression contains an error. </param>
        /// <param name="pichError"> Returns the character index of the error in pszCode if the expression contains an error. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugExpressionContext2.ParseText(string pszCode,
                                                enum_PARSEFLAGS dwFlags, 
                                                uint nRadix, 
                                                out IDebugExpression2 ppExpr, 
                                                out string pbstrError, 
                                                out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = null;

            ppExpr = new AD7Expression(pszCode, this, m_engine.eDispatcher);
            return VSConstants.S_OK;
        }

        #endregion
    }
}

