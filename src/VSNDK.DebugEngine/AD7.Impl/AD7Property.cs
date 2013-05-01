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
using System.Text;
using Microsoft.VisualStudio;
using VSNDK.Parser;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;

namespace VSNDK.DebugEngine
{
    // An implementation of IDebugProperty2
    // This interface represents a stack frame property, a program document property, or some other property. 
    // The property is usually the result of an expression evaluation. 
    //
    // The sample engine only supports locals and parameters for functions that have symbols loaded.
    class AD7Property : IDebugProperty2
    {
        private VariableInfo _variableInfo;

        public AD7Property()
        {
            _variableInfo = null;
        }

        public AD7Property(VariableInfo vi)
        {
            _variableInfo = vi;
        }

        // Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields)
        {
            DEBUG_PROPERTY_INFO propertyInfo = new DEBUG_PROPERTY_INFO();

            string name = (_variableInfo._exp == null)? _variableInfo._name : _variableInfo._exp;
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
            {
                propertyInfo.bstrFullName = name;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                propertyInfo.bstrName = name;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
            {
                propertyInfo.bstrType = _variableInfo._type;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
            {
                propertyInfo.bstrValue = _variableInfo._value;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                // We don't support writing of values displayed in the debugger, so mark them all as read-only.
                propertyInfo.dwAttrib = (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;

                if (_variableInfo._children != null)
                {
                    propertyInfo.dwAttrib |= (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }
            }

            // If the debugger has asked for the property, or the property has children (meaning it is a pointer in the sample)
            // then set the pProperty field so the debugger can call back when the chilren are enumerated.
            //if (((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0) || (this.m_variableInformation.child != null))
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0 ||
                _variableInfo._children != null) 
            {
                propertyInfo.pProperty = (IDebugProperty2)this;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP));
            }

            return propertyInfo;
        }

        #region IDebugProperty2 Members

        // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
        // The sample debugger only supports pointer dereferencing as children. This means there is only ever one child.
        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref System.Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;

            if (_variableInfo._children != null)
            {
                if (_variableInfo._children.Count == 0)
                {
                    // This is an array, struct, union, or pointer.
                    // Create a variable object so we can list this variable's children.
                    bool hasVsNdK_ = false;

                    string numChildren = AD7StackFrame.m_dispatcher.createVar(_variableInfo._name, ref hasVsNdK_);

                    ArrayList GDBNames = new ArrayList();
                    ArrayList VSNames = new ArrayList();

                    if (hasVsNdK_)
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
                                    _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                                else if (_variableInfo._type.Contains("["))
                                    _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "struct[]", GDBNames, VSNames, hasVsNdK_, null);
                                else
                                    _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "struct", GDBNames, VSNames, hasVsNdK_, null);
                            else if (_variableInfo._type.Contains("["))
                                _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "[]", GDBNames, VSNames, hasVsNdK_, null);
                            else if (_variableInfo._type.Contains("*"))
                                _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "*", GDBNames, VSNames, hasVsNdK_, null);
                            else
                                _variableInfo.listChildren(AD7StackFrame.m_dispatcher, "", GDBNames, VSNames, hasVsNdK_, null);

                        }
                    }
                    catch (FormatException e)
                    {
                    }
                    AD7StackFrame.m_dispatcher.deleteVar(_variableInfo._name, hasVsNdK_);
                }
                DEBUG_PROPERTY_INFO[] properties = new DEBUG_PROPERTY_INFO[_variableInfo._children.Count];
                int i = 0;
                foreach (VariableInfo child in _variableInfo._children)
                {
                    VariableInfo.evaluateExpression(child._name, ref child._value, child._GDBName);
                    properties[i] = new AD7Property(child).ConstructDebugPropertyInfo(dwFields);
                    i++;
                }
                ppEnum = new AD7PropertyEnum(properties);
                return VSConstants.S_OK;
            }

            return VSConstants.S_FALSE;
        }

        // Returns the property that describes the most-derived property of a property
        // This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the most-derived 
        // object in a hierarchy. This engine does not support this.
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the IDebugProperty2::GetPropertyInfo 
        // method. This includes information about custom viewers, managed type slots and other information.
        // The sample engine does not support this.
        public int GetExtendedInfo(ref System.Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory bytes for a property value.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory context for a property value.
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the parent of a property.
        // The sample engine does not support obtaining the parent of properties.
        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            pPropertyInfo[0] = new DEBUG_PROPERTY_INFO();
            rgpArgs = null;
            pPropertyInfo[0] = ConstructDebugPropertyInfo(dwFields);
            return VSConstants.S_OK;
        }

        //  Return an IDebugReference2 for this property. An IDebugReference2 can be thought of as a type and an address.
        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the size, in bytes, of the property value.
        public int GetSize(out uint pdwSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
