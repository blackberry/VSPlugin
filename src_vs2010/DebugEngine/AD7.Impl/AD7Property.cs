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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// An implementation of IDebugProperty2. (http://msdn.microsoft.com/en-ca/library/bb161287.aspx)
    /// This interface represents a stack frame property, a program document property, or some other property. 
    /// The property is usually the result of an expression evaluation. 
    /// </summary>
    sealed class AD7Property : IDebugProperty2
    {
        /// <summary>
        /// Object that contains all information about a variable / expression.
        /// </summary>
        private readonly VariableInfo _variableInfo;

        /// <summary>
        /// Object that contains all information about a stack frame.
        /// </summary>
        private readonly AD7StackFrame _stackFrame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="vi"> Contains all information about a variable / expression. </param>
        public AD7Property(AD7StackFrame vi)
        {
            _stackFrame = vi;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="vi"> Contains all information about a variable / expression. </param>
        public AD7Property(VariableInfo vi)
        {
            _variableInfo = vi;
        }

        /// <summary>
        /// Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which variables are 
        /// to be filled in. </param>
        /// <returns> The Property info. </returns>
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS flags)
        {
            DEBUG_PROPERTY_INFO propertyInfo = new DEBUG_PROPERTY_INFO();

            if (_variableInfo != null)
            {
                string name = _variableInfo._name;
                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
                {
                    propertyInfo.bstrFullName = name;
                    propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME));
                }

                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
                {
                    propertyInfo.bstrName = name;
                    propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME));
                }

                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
                {
                    propertyInfo.bstrType = _variableInfo._type;
                    propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE));
                }

                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
                {
                    propertyInfo.bstrValue = _variableInfo._value;
                    propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE));
                }

                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
                {
                    if (_variableInfo._children != null)
                    {
                        propertyInfo.dwAttrib |= (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                    }
                }

                // If the debugger has asked for the property, or the property has children (meaning it is a pointer in the sample)
                // then set the pProperty field so the debugger can call back when the chilren are enumerated.
                if ((flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0 || _variableInfo._children != null)
                {
                    propertyInfo.pProperty = this;
                    propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP));
                }

            }
            return propertyInfo;
        }

        #region IDebugProperty2 Members
        
        /// <summary>
        /// Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or 
        /// fields of a class or struct. (http://msdn.microsoft.com/en-us/library/bb161791.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields in 
        /// the enumerated DEBUG_PROPERTY_INFO structures are to be filled in. </param>
        /// <param name="radix"> Specifies the radix to be used in formatting any numerical information. </param>
        /// <param name="guidFilter"> GUID of the filter used with the dwAttribFilter and pszNameFilter parameters to select which 
        /// DEBUG_PROPERTY_INFO children are to be enumerated. For example, guidFilterLocals filters for local variables. </param>
        /// <param name="attributesFilter"> A combination of flags from the DBG_ATTRIB_FLAGS enumeration that specifies what type of 
        /// objects to enumerate, for example DBG_ATTRIB_METHOD for all methods that might be children of this property. Used in 
        /// combination with the guidFilter and pszNameFilter parameters. </param>
        /// <param name="nameFilter"> The name of the filter used with the guidFilter and dwAttribFilter parameters to select which 
        /// DEBUG_PROPERTY_INFO children are to be enumerated. For example, setting this parameter to "MyX" filters for all children 
        /// with the name "MyX." </param>
        /// <param name="timeout"> Specifies the maximum time, in milliseconds, to wait before returning from this method. Use 
        /// INFINITE to wait indefinitely. </param>
        /// <param name="ppEnum"> Returns an IEnumDebugPropertyInfo2 object containing a list of the child properties. </param>
        /// <returns> If successful, returns S_OK; otherwise returns S_FALSE. </returns>
        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS flags, uint radix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS attributesFilter, string nameFilter, uint timeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;

            if (_variableInfo != null)
            {
                if (_variableInfo._children != null)
                {
                    if (_variableInfo._children.Count == 0)
                    {
                        // This is an array, struct, union, or pointer.
                        // Create a variable object so we can list this variable's children.

                        // Some VS variable names cannot be used by GDB. When that happens, it is added the prefix VsNdK_ to the GDB variable 
                        // name and it is stored in the GDBNames array. At the same time, the VS name is stored in the VSNames array using the 
                        // same index position. This bool variable just indicate if this prefix is used or not.
                        bool hasVsNDK;
                        string numChildren = AD7StackFrame._dispatcher.CreateVariable(_variableInfo._name, out hasVsNDK);

                        ArrayList GDBNames = new ArrayList();
                        ArrayList VSNames = new ArrayList();

                        if (hasVsNDK)
                        {
                            _variableInfo._GDBName = "VsNdK_" + _variableInfo._name;
                            GDBNames.Add("VsNdK_" + _variableInfo._name);
                            VSNames.Add(_variableInfo._name);
                        }

                        try // Catch non-numerical data
                        {
                            if (Convert.ToInt32(numChildren) > 0) // If the variable has children evaluate
                            {
                                if (_variableInfo._type.Contains("struct"))
                                    if (_variableInfo._type[_variableInfo._type.Length - 1] == '*')
                                        _variableInfo.ListChildren(AD7StackFrame._dispatcher, "*", GDBNames, VSNames, hasVsNDK, null);
                                    else if (_variableInfo._type.Contains("["))
                                        _variableInfo.ListChildren(AD7StackFrame._dispatcher, "struct[]", GDBNames, VSNames, hasVsNDK, null);
                                    else
                                        _variableInfo.ListChildren(AD7StackFrame._dispatcher, "struct", GDBNames, VSNames, hasVsNDK, null);
                                else if (_variableInfo._type.Contains("["))
                                    _variableInfo.ListChildren(AD7StackFrame._dispatcher, "[]", GDBNames, VSNames, hasVsNDK, null);
                                else if (_variableInfo._type.Contains("*"))
                                    _variableInfo.ListChildren(AD7StackFrame._dispatcher, "*", GDBNames, VSNames, hasVsNDK, null);
                                else
                                    _variableInfo.ListChildren(AD7StackFrame._dispatcher, "", GDBNames, VSNames, hasVsNDK, null);

                            }
                        }
                        catch (FormatException)
                        {
                        }
                        AD7StackFrame._dispatcher.DeleteVariable(_variableInfo._name, hasVsNDK);
                    }
                    DEBUG_PROPERTY_INFO[] properties = new DEBUG_PROPERTY_INFO[_variableInfo._children.Count];
                    int i = 0;
                    foreach (VariableInfo child in _variableInfo._children)
                    {
                        VariableInfo.EvaluateExpression(child._name, out child._value, child._GDBName);
                        properties[i] = new AD7Property(child).ConstructDebugPropertyInfo(flags);
                        i++;
                    }
                    ppEnum = new AD7PropertyEnum(properties);
                    return VSConstants.S_OK;
                }
            }
            else if (_stackFrame != null)
            {
                uint elementsReturned;
                _stackFrame.CreateLocalsPlusArgsProperties(flags, out elementsReturned, out ppEnum);
                return VSConstants.S_OK;
            }

            return VSConstants.S_FALSE;
        }

        /// <summary>
        /// Returns the property that describes the most-derived property of a property. 
        /// (http://msdn.microsoft.com/en-ca/library/bb161781.aspx)
        /// This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the 
        /// most-derived object in a hierarchy. 
        /// The VSNDK debug engine does not support this. Not implemented. 
        /// </summary>
        /// <param name="ppDerivedMost"> Returns an IDebugProperty2 object that represents the derived-most property. </param>
        /// <returns> Not implemented. </returns>
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            ppDerivedMost = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Returns the extended information of a property. (http://msdn.microsoft.com/en-ca/library/bb145070.aspx)
        /// This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the 
        /// IDebugProperty2::GetPropertyInfo method. This includes information about custom viewers, managed type slots and other 
        /// information.
        /// The VSNDK debug engine does not support this. Not implemented. 
        /// </summary>
        /// <param name="guidExtendedInfo"> GUID that determines the type of extended information to be retrieved. </param>
        /// <param name="pExtendedInfo"> Returns a VARIANT (C++) or object (C#) that can be used to retrieve the extended property 
        /// information. For example, this parameter might return an IUnknown interface that can be queried for an IDebugDocumentText2 
        /// interface. </param>
        /// <returns> Not implemented. </returns>
        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            pExtendedInfo = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Returns the memory bytes that compose the value of a property. (http://msdn.microsoft.com/en-ca/library/bb161360.aspx)
        /// Not implemented. 
        /// </summary>
        /// <param name="ppMemoryBytes"> Returns an IDebugMemoryBytes2 object that can be used to retrieve the memory that contains 
        /// the value of the property.</param>
        /// <returns> Not implemented. </returns>
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            ppMemoryBytes = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Returns the memory context for a property value. (http://msdn.microsoft.com/en-ca/library/bb147028.aspx)
        /// Not implemented. 
        /// </summary>
        /// <param name="ppMemory"> Returns the IDebugMemoryContext2 object that represents the memory associated with this property. </param>
        /// <returns> Not implemented. </returns>
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            ppMemory = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Returns the parent of a property. (http://msdn.microsoft.com/en-ca/library/bb146184.aspx)
        /// Not implemented. 
        /// </summary>
        /// <param name="ppParent"> Returns an IDebugProperty2 object that represents the parent of the property. </param>
        /// <returns> Not implemented. </returns>
        public int GetParent(out IDebugProperty2 ppParent)
        {
            ppParent = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Fills in a DEBUG_PROPERTY_INFO structure that describes a property. (http://msdn.microsoft.com/en-ca/library/bb145852.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the DEBUGPROP_INFO_FLAGS enumeration that specifies which fields are to 
        /// be filled out in the pPropertyInfo structure. </param>
        /// <param name="radix"> Radix to be used in formatting any numerical information. </param>
        /// <param name="timeout"> Specifies the maximum time, in milliseconds, to wait before returning from this method. Use 
        /// INFINITE to wait indefinitely. </param>
        /// <param name="rgpArgs"> Reserved for future use; set to a null value. </param>
        /// <param name="argCount"> Reserved for future use; set to zero. </param>
        /// <param name="pPropertyInfo"> A DEBUG_PROPERTY_INFO structure that is filled in with the description of the property. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS flags, uint radix, uint timeout, IDebugReference2[] rgpArgs, uint argCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            pPropertyInfo[0] = new DEBUG_PROPERTY_INFO();
            pPropertyInfo[0] = ConstructDebugPropertyInfo(flags);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns a reference to the property's value. IDebugProperty2 represents a property, while IDebugReference2 represents a 
        /// reference to a property, typically a reference to an object in the program being debugged. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb145600.aspx)
        /// </summary>
        /// <param name="ppReference"> Returns an IDebugReference2 object representing a reference to the property's value. </param>
        /// <returns> Not implemented. </returns>
        public int GetReference(out IDebugReference2 ppReference)
        {
            ppReference = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Returns the size, in bytes, of the property value. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb145093.aspx)
        /// </summary>
        /// <param name="pSize"> Returns the size, in bytes, of the property value. </param>
        /// <returns> Not implemented. </returns>
        public int GetSize(out uint pSize)
        {
            pSize = 0;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Sets the value of the property from the value of a given reference. (http://msdn.microsoft.com/en-ca/library/bb145613.aspx)
        /// The debugger will call this when the user tries to edit the property's values. As the sample has set the read-only flag on 
        /// its properties, this should not be called.
        /// Not implemented.  
        /// </summary>
        /// <param name="rgpArgs"> An array of arguments to pass to the managed code property setter. If the property setter does not 
        /// take arguments or if this IDebugProperty2 object does not refer to such a property setter, rgpArgs should be a null value. 
        /// This parameter is typically a null value. </param>
        /// <param name="argCount"> The number of arguments in the rgpArgs array. </param>
        /// <param name="value"> A reference, in the form of an IDebugReference2 object, to the value to use to set this property. </param>
        /// <param name="timeout"> How long to take to set the value, in milliseconds. A typical value is INFINITE. This affects 
        /// the length of time that any possible evaluation can take. </param>
        /// <returns> Not implemented. </returns>
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint argCount, IDebugReference2 value, uint timeout)
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Sets the value of a property from a given string. (http://msdn.microsoft.com/en-ca/library/bb160956.aspx)
        /// The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        /// </summary>
        /// <param name="value"> A string containing the value to be set. </param>
        /// <param name="radix"> A radix to be used in interpreting any numerical information. This can be 0 to attempt to determine 
        /// the radix automatically. </param>
        /// <param name="timeout"> Specifies the maximum time, in milliseconds, to wait before returning from this method. Use 
        /// INFINITE to wait indefinitely. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int SetValueAsString(string value, uint radix, uint timeout)
        {
            string result;
            VariableInfo.EvaluateExpression(_variableInfo._name + "=" + value, out result, null);
            return VSConstants.S_OK;
        }

        #endregion
    }
}
