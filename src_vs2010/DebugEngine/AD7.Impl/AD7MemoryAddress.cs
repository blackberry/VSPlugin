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

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// And implementation of IDebugCodeContext2 and IDebugMemoryContext2. 
    /// IDebugMemoryContext2 represents a position in the address space of the machine running the program being debugged.
    /// IDebugCodeContext2 represents the starting position of a code instruction. For most run-time architectures today, a code 
    /// context can be thought of as an address in a program's execution stream.
    /// </summary>
    public class AD7MemoryAddress : IDebugCodeContext2, IDebugCodeContext100
    {
        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        readonly AD7Engine m_engine;

        /// <summary>
        /// The current context's address. 
        /// </summary>
        readonly uint m_address;

        /// <summary>
        /// The IDebugDocumentContext2 object that corresponds to the code context. 
        /// </summary>
        IDebugDocumentContext2 m_documentContext;
        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="address"> The current context's address. </param>
        public AD7MemoryAddress(AD7Engine engine, uint address)
        {
            m_engine = engine;
            m_address = address;
        }


        /// <summary>
        /// Sets the document context.
        /// </summary>
        /// <param name="docContext"> The IDebugDocumentContext2 object that corresponds to the code context. </param>
        public void SetDocumentContext(IDebugDocumentContext2 docContext)
        {
            m_documentContext = docContext;
        }

        #region IDebugMemoryContext2 Members


        /// <summary>
        /// Adds a specified value to the current context's address to create a new context. 
        /// (http://msdn.microsoft.com/en-ca/library/bb145861.aspx)
        /// </summary>
        /// <param name="dwCount"> The value to add to the current context. </param>
        /// <param name="newAddress"> Returns a new IDebugMemoryContext2 object. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int Add(ulong dwCount, out IDebugMemoryContext2 newAddress)
        {
            newAddress = new AD7MemoryAddress(m_engine, (uint)dwCount + m_address);
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Compares the memory context to each context in the given array in the manner indicated by compare flags, 
        /// returning an index of the first context that matches. (http://msdn.microsoft.com/en-ca/library/bb161750.aspx)
        /// </summary>
        /// <param name="uContextCompare"> A value from the CONTEXT_COMPARE enumeration that determines the type of comparison. </param>
        /// <param name="compareToItems"> An array of references to the IDebugMemoryContext2 objects to compare against. </param>
        /// <param name="compareToLength"> The number of contexts in the compareToItems array. </param>
        /// <param name="foundIndex"> Returns the index of the first memory context that satisfies the comparison. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
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

                        // The VSNDK debug engine doesn't understand scopes or functions
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


        /// <summary>
        /// Gets information that describes this context. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb145034.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the CONTEXT_INFO_FIELDS enumeration that indicate which fields of the 
        /// CONTEXT_INFO structure are to be fill in. </param>
        /// <param name="pinfo"> The CONTEXT_INFO structure that is filled in. </param>
        /// <returns> Not implemented. </returns>
        public int GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
        {
            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets the user-displayable name for this context. This is not supported by the VSNDK debug engine.
        /// (http://msdn.microsoft.com/en-ca/library/bb146997.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the memory context. </param>
        /// <returns> Not implemented. </returns>
        public int GetName(out string pbstrName)
        {
            throw new Exception("The method or operation is not implemented.");
        }


        /// <summary>
        /// Subtracts a specified value from the current context's address to create a new context. 
        /// (http://msdn.microsoft.com/en-ca/library/bb146285.aspx)
        /// </summary>
        /// <param name="dwCount"> The number of memory bytes to decrement. </param>
        /// <param name="ppMemCxt"> Returns a new IDebugMemoryContext2 object. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        {
            ppMemCxt = new AD7MemoryAddress(m_engine, (uint)dwCount - m_address);
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugCodeContext2 Members


        /// <summary>
        /// Gets the document context that corresponds to this code context. The document context represents a position in the source file
        /// that corresponds to the source code that generated this instruction. (http://msdn.microsoft.com/en-ca/library/bb161811.aspx)
        /// </summary>
        /// <param name="ppSrcCxt"> Returns the IDebugDocumentContext2 object that corresponds to the code context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
        {
            ppSrcCxt = m_documentContext;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the language information for this code context. (http://msdn.microsoft.com/en-ca/library/bb144925.aspx)
        /// </summary>
        /// <param name="pbstrLanguage"> Returns a string that contains the name of the language, such as "C++.". </param>
        /// <param name="pguidLanguage"> Returns the GUID for the language of the code context, for example, guidCPPLang. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns S_FALSE. </returns>
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


        /// <summary>
        /// Returns the program being debugged. In the case of this VSNDK debug debug engine, AD7Engine implements IDebugProgram2 which 
        /// represents the program being debugged. 
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugcodecontext100.getprogram.aspx)
        /// </summary>
        /// <param name="pProgram"> Returns the program being debugged. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugCodeContext100.GetProgram(out IDebugProgram2 pProgram)
        {
            pProgram = m_engine;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
