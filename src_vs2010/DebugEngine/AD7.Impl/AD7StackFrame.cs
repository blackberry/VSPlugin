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
using System.Collections;
using Microsoft.VisualStudio;
using VSNDK.Parser;
using Microsoft.VisualStudio.Debugger.Interop;

namespace BlackBerry.DebugEngine
{
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
    public sealed class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        /// <summary>
        /// The class that manages debug events for the debug engine.
        /// </summary>
        public static EventDispatcher _dispatcher;

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        public readonly AD7Engine _engine;
        
        /// <summary>
        /// Represents the thread for this stack frame.
        /// </summary>
        public readonly AD7Thread _thread;

        /// <summary>
        ///  The short path file name that contains the source code of this stack frame. 
        /// </summary>
        private readonly string _documentName;
        
        /// <summary>
        /// The function name associated to this stack frame.
        /// </summary>
        public string _functionName = "";
        
        /// <summary>
        /// Represents the current position (line number) in m_documentName.
        /// </summary>
        private readonly uint _lineNum;
        
        /// <summary>
        /// Boolean value that indicates if this stack frame has an associated source code to present.
        /// </summary>
        private readonly bool _hasSource;
        
        /// <summary>
        /// The current context's address. 
        /// </summary>
        private readonly uint _address;
        
        /// <summary>
        /// List of variables that we want to filter from the locals window.
        /// </summary>
        private string[] _variableFilter = { "__func__" };

        /// <summary>
        /// Contains the locals variables to this stack frame.
        /// </summary>
        public ArrayList _locals;
        
        /// <summary>
        /// Contains the parameters used to call the method/function that originated this stack frame.
        /// </summary>
        public ArrayList _arguments;

        /// <summary>
        /// Search the __stackframes cache for the internal representation of the stack frame associated to the GDB frameInfo 
        /// information. If successful, returns the stack frame; otherwise, creates a new one and return it.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="thread"> Represents the thread for this stack frame. </param>
        /// <param name="frameInfo">  Array of strings with the information provided by GDB about this stack frame. </param>
        /// <param name="created"> Boolean value that indicates if a new object for this stack frame was created or not. </param>
        /// <returns> Returns the created/found stack frame. </returns>
        public static AD7StackFrame Create(AD7Engine engine, AD7Thread thread, string[] frameInfo, ref bool created)
        {
            created = false;
            if (thread.__stackFrames != null)
            {
                foreach (AD7StackFrame frame in thread.__stackFrames)
                {
                    if (frame._documentName != null && frame._functionName != null)
                    {
                        if (frame._documentName == frameInfo[3] && frame._functionName == frameInfo[2]) // frameInfo[2] = func, frameInfo[3] = file
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
            _lineNum = 0;
            _engine = engine;
            _thread = thread;
            _dispatcher = _engine.EventDispatcher;

            uint level = Convert.ToUInt32(frameInfo[0]);
            string address = frameInfo[1];
            _functionName = frameInfo[2];
            _documentName = frameInfo[3];

            if (!uint.TryParse(frameInfo[4], out _lineNum))
            {
                _lineNum = 0;
            }

            _locals = new ArrayList();
            _arguments = new ArrayList();
            _hasSource = _lineNum != 0;
            ArrayList evaluatedVars = new ArrayList();
            
            // Add the variable filter list to the evaluatedVars list.
            // Causes named variables to be ignored.
            evaluatedVars.AddRange(_variableFilter);

            if (address.StartsWith("0x"))
                address = address.Remove(0, 2);
            _address = uint.Parse(address, System.Globalization.NumberStyles.AllowHexSpecifier);

            // Query GDB for parameters and locals.
            string variablesResponse = _engine.EventDispatcher.GetVariablesForFrame(level, _thread._id).Replace("#;;;", "");
            if (variablesResponse == null || variablesResponse == "ERROR" || variablesResponse == "")
                return;
            variablesResponse = variablesResponse.Substring(3);

            string[] variableStrings = variablesResponse.Split('#');

            foreach (string variableString in variableStrings)
            {
                bool arg = false;
                string type = null;
                string value = null;

                string[] variableProperties = variableString.Split(';');

                if (variableProperties[0] != "")
                {
                    if (!evaluatedVars.Contains(variableProperties[0]))
                    {
                        string name = variableProperties[0];
                        evaluatedVars.Add(variableProperties[0]);
                        if (variableProperties[1] != "")
                            arg = true;
                        if (variableProperties[2] != "")
                            type = variableProperties[2];
                        if (variableProperties[3] != "")
                            value = variableProperties[3];
                        if (arg)
                            _arguments.Add(VariableInfo.Create(name, type, value, _engine.EventDispatcher));
                        else
                            _locals.Add(VariableInfo.Create(name, type, value, _engine.EventDispatcher));
                    }
                }
            }
        }

        #region Non-interface methods

        /// <summary>
        /// Construct a FRAMEINFO for this stack frame with the requested information.
        /// </summary>
        /// <param name="flags"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the 
        /// frameInfo parameter are to be filled in. </param>
        /// <param name="frameInfo"> A FRAMEINFO structure that is filled in with the description of the stack frame. </param>
        public void SetFrameInfo(enum_FRAMEINFO_FLAGS flags, out FRAMEINFO frameInfo)
        {
            frameInfo = new FRAMEINFO();

            // The debugger is asking for the formatted name of the function which is displayed in the callstack window.
            // There are several optional parts to this name including the module, argument types and values, and line numbers.
            // The optional information is requested by setting flags in the dwFieldSpec parameter.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME) != 0)
            {
                // If there is source information, construct a string that contains the module name, function name, and optionally argument names and values.
                if (_hasSource)
                {
                    frameInfo.m_bstrFuncName = "";
                    
                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
//                        frameInfo.m_bstrFuncName = System.IO.Path.GetFileName(module.Name) + "!";
                    }
                    

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_RETURNTYPE) != 0)
                    {
                        // Adds the return type to the m_bstrFuncName field.
                        //frameInfo.m_bstrFuncName += _returnType;
                    }

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LANGUAGE) != 0)
                    {
                        // Adds the language to the m_bstrFuncName field.
                        if (_documentName.EndsWith(".c"))
                            frameInfo.m_bstrFuncName += "(C) ";
                        else if (_documentName.EndsWith(".cpp") || _documentName.EndsWith(".c++"))
                            frameInfo.m_bstrFuncName += "(C++) ";
                    }

                    frameInfo.m_bstrFuncName += _functionName;

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS) != 0)
                    {
                        // Add the arguments to the m_bstrFuncName field.
                        frameInfo.m_bstrFuncName += "(";
                        bool all = (flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_ALL) != 0;
                        int i = 0;
                        foreach (VariableInfo arg in _arguments)
                        {
                            if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES) != 0)
                            {
                                frameInfo.m_bstrFuncName += arg._type + " ";
                            }

                            if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES) != 0)
                            {
                                frameInfo.m_bstrFuncName += arg._name;
                            }

                            if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_VALUES) != 0)
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

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES) != 0)
                    {
                        frameInfo.m_bstrFuncName += " Line: " + _lineNum.ToString();
                    }

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_OFFSET) != 0)
                    {
                        // TODO:
                        // Adds to the m_bstrFuncName field the offset in bytes from the start of the line if FIF_FUNCNAME_LINES is specified.
                        // If FIF_FUNCNAME_LINES is not specified, or if line numbers are not available, adds the offset in bytes from the start of the function.
                    }

                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_FORMAT) != 0)
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
                    if ((flags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
                        if ((_functionName != "") && (_functionName != "??"))
                            frameInfo.m_bstrFuncName = _functionName;
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
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_MODULE) != 0)
            {
                frameInfo.m_bstrModule = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_MODULE;
            }
            

            if ((flags & enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE) != 0)
            {
                // TODO:
                // Initialize/use the m_bstrReturnType field.
                frameInfo.m_bstrReturnType = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE;
            }

            if ((flags & enum_FRAMEINFO_FLAGS.FIF_ARGS) != 0)
            {
                // Initialize/use the m_bstrArgs field.
                frameInfo.m_bstrArgs = "";
                bool all = (flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_ALL) != 0;
                int i = 0;
                foreach (VariableInfo arg in _arguments)
                {
                    if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_TYPES) != 0)
                    {
                        frameInfo.m_bstrArgs += arg._type + " ";
                    }

                    if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_NAMES) != 0)
                    {
                        frameInfo.m_bstrArgs += arg._name;
                    }

                    if (all || (flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_VALUES) != 0)
                    {
                        frameInfo.m_bstrArgs += "=" + arg._value;
                    }

                    if (i < _arguments.Count - 1)
                    {
                        frameInfo.m_bstrArgs += ", ";
                    }
                    i++;
                }

                if ((flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_NO_TOSTRING) != 0)
                {
                    // TODO:
                    // Do not allow ToString() function evaluation or formatting when returning function arguments.
                }

                if ((flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_NO_FUNC_EVAL) != 0)
                {
                    // TODO:
                    // Specifies that function (property) evaluation should not be used when retrieving argument values.
                }

                if ((flags & enum_FRAMEINFO_FLAGS.FIF_ARGS_NOFORMAT) != 0)
                {
                    // TODO:
                    // Specifies that the arguments are not be formatted (for example, do not add opening and closing parentheses around 
                    // the argument list nor add a separator between arguments).
                }

                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_ARGS;
            }

            if ((flags & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0)
            {
                // Initialize/use the m_bstrLanguage field.
                if (_documentName != null)
                {
                    if (_documentName.EndsWith(".c"))
                        frameInfo.m_bstrLanguage = "C";
                    else if (_documentName.EndsWith(".cpp") || _documentName.EndsWith(".c++"))
                        frameInfo.m_bstrLanguage = "C++";
                    else
                        frameInfo.m_bstrLanguage = "";
                }
                else
                    frameInfo.m_bstrLanguage = "";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_LANGUAGE;
            }

            // The debugger would like a pointer to the IDebugModule2 that contains this stack frame.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP) != 0)
            {
                frameInfo.m_pModule = _engine._module;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP;
            }

            // The debugger is requesting the range of memory addresses for this frame.
            // For the sample engine, this is the contents of the frame pointer.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_STACKRANGE) != 0)
            {
                // TODO:
                // Initialize/use the m_addrMin and m_addrMax (stack range) fields.

                frameInfo.m_addrMin = 0;
                frameInfo.m_addrMax = 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STACKRANGE;
            }

            // The debugger is requesting the IDebugStackFrame2 value for this frame info.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_FRAME) != 0)
            {
                frameInfo.m_pFrame = this;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FRAME;
            }

            // Does this stack frame of symbols loaded?
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO) != 0)
            {
                frameInfo.m_fHasDebugInfo = _hasSource ? 1 : 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
            }

            // Is this frame stale?
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_STALECODE) != 0)
            {
                frameInfo.m_fStaleCode = 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
            }

            // The debug engine is to filter non-user code frames so they are not included.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_FILTER_NON_USER_CODE ) != 0)
            {
            }

            // Frame information should be gotten from the hosted app-domain rather than the hosting process.
            if ((flags & enum_FRAMEINFO_FLAGS.FIF_DESIGN_TIME_EXPR_EVAL) != 0)
            {
            }
        }

        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the combined locals and parameters.
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        public void CreateLocalsPlusArgsProperties(enum_DEBUGPROP_INFO_FLAGS flags, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
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
                    propInfo[i] = property.ConstructDebugPropertyInfo(flags);
                    i++;
                }
            }

            if (_arguments != null)
            {
                int i = 0;
                foreach (VariableInfo arg in _arguments)
                {
                    AD7Property property = new AD7Property(arg);
                    propInfo[localsLength + i] = property.ConstructDebugPropertyInfo(flags);
                    i++;
                }
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }

        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the locals collection only.
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        private void CreateLocalProperties(enum_DEBUGPROP_INFO_FLAGS flags, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = (uint)_locals.Count;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[_locals.Count];

            int i = 0;
            foreach (VariableInfo var in _locals)
            {
                AD7Property property = new AD7Property(var);
                propInfo[i] = property.ConstructDebugPropertyInfo(flags);
                i++;
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }

        /// <summary>
        /// Construct an instance of IEnumDebugPropertyInfo2 for the parameters collection only.
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumObject are to be filled in.</param>
        /// <param name="elementsReturned"> Returns the number of elements in the enumeration. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        private void CreateParameterProperties(enum_DEBUGPROP_INFO_FLAGS flags, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = (uint)_arguments.Count;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[_arguments.Count];

            int i = 0;
            foreach (VariableInfo arg in _arguments)
            {
                AD7Property property = new AD7Property(arg);
                propInfo[i] = property.ConstructDebugPropertyInfo(flags);
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
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumerated DEBUG_PROPERTY_INFO structures are to be filled in. </param>
        /// <param name="radix"> The radix to be used in formatting any numerical information. </param>
        /// <param name="guidFilter"> A GUID of a filter used to select which DEBUG_PROPERTY_INFO structures are to be enumerated, such as guidFilterLocals. </param>
        /// <param name="timeout"> Maximum time, in milliseconds, to wait before returning from this method. Use INFINITE to wait indefinitely. </param>
        /// <param name="elementsReturned"> Returns the number of properties enumerated. This is the same as calling the IEnumDebugPropertyInfo2::GetCount method. </param>
        /// <param name="enumObject"> Returns an IEnumDebugPropertyInfo2 object containing a list of the desired properties. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.EnumProperties(enum_DEBUGPROP_INFO_FLAGS flags, uint radix, ref Guid guidFilter, uint timeout, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            elementsReturned = 0;
            enumObject = null;

            try
            {
                if (guidFilter == AD7Guids.guidFilterLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocals)
                {
                    CreateLocalsPlusArgsProperties(flags, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                if (guidFilter == AD7Guids.guidFilterLocals)
                {
                    CreateLocalProperties(flags, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                if (guidFilter == AD7Guids.guidFilterArgs)
                {
                    CreateParameterProperties(flags, out elementsReturned, out enumObject);
                    return VSConstants.S_OK;
                }
                return EngineUtils.NotImplemented();
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
            }
        }

        /// <summary>
        /// Gets the code context for this stack frame. The code context represents the current instruction pointer in this stack frame.
        /// (http://msdn.microsoft.com/en-us/library/bb147046.aspx)
        /// </summary>
        /// <param name="pMemoryAddress"> Returns an IDebugCodeContext2 object that represents the current instruction pointer in this stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 pMemoryAddress)
        {
            pMemoryAddress = null;
            try
            {
                pMemoryAddress = new AD7MemoryAddress(_engine, _address);
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
        /// <param name="pProperty"> Returns an IDebugProperty2 object that describes the properties of this stack frame. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 pProperty)
        {
            pProperty = new AD7Property(this);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the document context for this stack frame. The debugger will call this when the current stack frame is changed and 
        /// will use it to open the correct source document for this stack frame. (http://msdn.microsoft.com/en-us/library/bb146338.aspx)
        /// </summary>
        /// <param name="pDocContext"> Returns an IDebugDocumentContext2 object that represents the current position in a source 
        /// document. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 pDocContext)
        {
            _engine.CleanEvaluatedThreads();
            pDocContext = null;

            try
            {
                // Assume all lines begin and end at the beginning of the line.
                TEXT_POSITION begTp = new TEXT_POSITION();
                begTp.dwColumn = 0;
                if (_lineNum != 0)
                    begTp.dwLine = _lineNum - 1;
                else
                    begTp.dwLine = 0;
                TEXT_POSITION endTp = new TEXT_POSITION();
                endTp.dwColumn = 0;
                if (_lineNum != 0)
                    endTp.dwLine = _lineNum - 1;
                else
                    endTp.dwLine = 0;

                pDocContext = new AD7DocumentContext(_documentName, begTp, endTp, null);
                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
            }
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
            ppExprCxt = this;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets a description of the stack frame. (http://msdn.microsoft.com/en-us/library/bb145146.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the 
        /// pFrameInfo parameter are to be filled in. </param>
        /// <param name="radix"> The radix to be used in formatting any numerical information. </param>
        /// <param name="pFrameInfo"> A FRAMEINFO structure that is filled in with the description of the stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugStackFrame2.GetInfo(enum_FRAMEINFO_FLAGS flags, uint radix, FRAMEINFO[] pFrameInfo)
        {
            try
            {
                SetFrameInfo(flags, out pFrameInfo[0]);

                int frame = 0;
                if (_thread.__stackFrames != null)
                {
                    foreach (AD7StackFrame sf in _thread.__stackFrames)
                    {
                        if (sf._functionName == _functionName)
                            break;
                        frame++;
                    }
                }

                if (_thread._id != _engine.CurrentThread()._id)
                    _engine.EventDispatcher.SelectThread(_thread._id);

                // Waits for the parsed response for the GDB/MI command that changes the selected frame.
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
                GDBParser.parseCommand("-stack-select-frame " + frame, 5);

                if (_thread._id != _engine.CurrentThread()._id)
                    _engine.EventDispatcher.SelectThread(_engine.CurrentThread()._id);

                _engine.CleanEvaluatedThreads();
                
                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
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
            if (_documentName.EndsWith(".c"))
            {
                pbstrLanguage = "C";
                pguidLanguage = AD7Guids.guidLanguageCpp;
            }
            else if (_documentName.EndsWith(".cpp") || _documentName.EndsWith(".c++"))
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
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Gets a machine-dependent representation of the range of physical addresses associated with a stack frame.
        /// Not implemented. (http://msdn.microsoft.com/en-us/library/bb145597.aspx)
        /// </summary>
        /// <param name="addressMin"> Returns the lowest physical address associated with this stack frame. </param>
        /// <param name="addressMax"> Returns the highest physical address associated with this stack frame. </param>
        /// <returns> Not implemented. </returns>
        int IDebugStackFrame2.GetPhysicalStackRange(out ulong addressMin, out ulong addressMax)
        {
            addressMin = 0;
            addressMax = 0;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Gets the thread associated with a stack frame. (http://msdn.microsoft.com/en-us/library/bb161776.aspx)
        /// </summary>
        /// <param name="pThread"> Returns an IDebugThread2 object that represents the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugStackFrame2.GetThread(out IDebugThread2 pThread)
        {
            pThread = _thread;
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
            pbstrName = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Parses a text-based expression for evaluation. (http://msdn.microsoft.com/en-ca/library/bb162304.aspx).
        /// GDB will parse and evaluate the expression, returning the result or an error in the expression. So, the only task for this
        /// method is to create the IDebugExpression2 object that will be sent indirectly to the methods responsible for the evaluation.
        /// </summary>
        /// <param name="pszCode"> The expression to be parsed. </param>
        /// <param name="dwFlags"> A combination of flags from the PARSEFLAGS enumeration that controls parsing. </param>
        /// <param name="nRadix"> The radix to be used in parsing any numerical information in pszCode. </param>
        /// <param name="ppExpr"> Returns the IDebugExpression2 object that represents the parsed expression, which is ready for binding and evaluation. </param>
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
            ppExpr = new AD7Expression(pszCode, this, _engine.EventDispatcher);
            return VSConstants.S_OK;
        }

        #endregion
    }
}
