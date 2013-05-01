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
using Microsoft.VisualStudio.Debugger.Interop;

namespace VSNDK.DebugEngine
{
    // And implementation of IDebugCodeContext2 and IDebugMemoryContext2. 
    // IDebugMemoryContext2 represents a position in the address space of the machine running the program being debugged.
    // IDebugCodeContext2 represents the starting position of a code instruction. 
    // For most run-time architectures today, a code context can be thought of as an address in a program's execution stream.
    public class AD7MemoryAddress : IDebugCodeContext2, IDebugCodeContext100
    {
        readonly AD7Engine m_engine;
        readonly uint m_address;
        IDebugDocumentContext2 m_documentContext;
        
        public AD7MemoryAddress(AD7Engine engine, uint address)
        {
            m_engine = engine;
            m_address = address;
        }

        public void SetDocumentContext(IDebugDocumentContext2 docContext)
        {
            m_documentContext = docContext;
        }

        #region IDebugMemoryContext2 Members

        // Adds a specified value to the current context's address to create a new context.
        public int Add(ulong dwCount, out IDebugMemoryContext2 newAddress)
        {
            newAddress = new AD7MemoryAddress(m_engine, (uint)dwCount + m_address);
            return VSConstants.S_OK;
        }

        // Compares the memory context to each context in the given array in the manner indicated by compare flags, 
        // returning an index of the first context that matches.
        public int Compare(enum_CONTEXT_COMPARE uContextCompare, IDebugMemoryContext2[] compareToItems, uint compareToLength, out uint foundIndex)
        {
            foundIndex = uint.MaxValue;

            try
            {
                enum_CONTEXT_COMPARE contextCompare = (enum_CONTEXT_COMPARE)uContextCompare;

                for (uint c = 0; c < compareToLength; c++)
                {
                    AD7MemoryAddress compareTo = compareToItems[c] as AD7MemoryAddress;
                    if (compareTo == null)
                    {
                        continue;
                    }

                    if (!AD7Engine.ReferenceEquals(this.m_engine, compareTo.m_engine))
                    {
                        continue;
                    }

                    bool result;

                    switch (contextCompare)
                    {
                        case enum_CONTEXT_COMPARE.CONTEXT_EQUAL:
                            result = (this.m_address == compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN:
                            result = (this.m_address < compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN:
                            result = (this.m_address > compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN_OR_EQUAL:
                            result = (this.m_address <= compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN_OR_EQUAL:
                            result = (this.m_address >= compareTo.m_address);
                            break;

                        // The sample debug engine doesn't understand scopes or functions
                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_SCOPE:
                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_FUNCTION:
                            result = (this.m_address == compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_MODULE:
                            result = (this.m_address == compareTo.m_address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_PROCESS:
                            result = true;
                            break;

                        default:
                            // A new comparison was invented that we don't support
                            return VSConstants.E_NOTIMPL;
                    }

                    if (result)
                    {
                        foundIndex = c;
                        return VSConstants.S_OK;
                    }
                }

                return VSConstants.S_FALSE;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets information that describes this context.
        public int GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
        {
            return VSConstants.E_NOTIMPL;
        }

        // Gets the user-displayable name for this context
        // This is not supported by the sample engine.
        public int GetName(out string pbstrName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Subtracts a specified value from the current context's address to create a new context.
        public int Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        {
            ppMemCxt = new AD7MemoryAddress(m_engine, (uint)dwCount - m_address);
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugCodeContext2 Members

        // Gets the document context for this code-context
        public int GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
        {
            ppSrcCxt = m_documentContext;
            return VSConstants.S_OK;
        }

        // Gets the language information for this code context.
        public int GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            if (m_documentContext != null)
            {
                m_documentContext.GetLanguageInfo(ref pbstrLanguage, ref pguidLanguage);
                return VSConstants.S_OK;
            }
            else
            {
                return VSConstants.S_FALSE;
            }
        }

        #endregion

        #region IDebugCodeContext100 Members

        // Returns the program being debugged. In the case of this sample
        // debug engine, AD7Engine implements IDebugProgram2 which represents
        // the program being debugged.
        int IDebugCodeContext100.GetProgram(out IDebugProgram2 pProgram)
        {
            pProgram = m_engine;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
