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
    #region Base Class


    /// <summary>
    /// These classes use a generic enumerator implementation to create the various enumerators required by the engine.
    /// They allow the enumeration of everything from programs to breakpoints.
    /// </summary>
    /// <typeparam name="T"> Array of T elements. </typeparam>
    /// <typeparam name="I"> Enumerator interface. </typeparam>
    class AD7Enum<T,I> where I: class
    {
        readonly T[] m_data;
        uint m_position;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of T elements. </param>
        public AD7Enum(T[] data)
        {
            m_data = data;
            m_position = 0;
        }


        /// <summary>
        /// Returns a copy of the current enumeration as a separate object. Not implemented.
        /// </summary>
        /// <param name="ppEnum"> Returns a copy of this enumeration as a separate object.</param>
        /// <returns> Not implemented. </returns>
        public int Clone(out I ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Returns the number of elements in the enumeration.
        /// </summary>
        /// <param name="pcelt"> Returns the number of elements in the enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetCount(out uint pcelt)
        {
            pcelt = (uint)m_data.Length;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration.
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of T elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, T[] rgelt, out uint celtFetched)
        {
            return Move(celt, rgelt, out celtFetched);
        }


        /// <summary>
        /// Resets the enumeration to the first element.
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int Reset()
        {
            lock (this)
            {
                m_position = 0;

                return VSConstants.S_OK;
            }
        }


        /// <summary>
        /// Skips over the specified number of elements.
        /// </summary>
        /// <param name="celt"> Number of elements to skip. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Skip(uint celt)
        {
            uint celtFetched;

            return Move(celt, null, out celtFetched);
        }


        /// <summary>
        /// Returns/Skips over the specified number of elements.
        /// </summary>
        /// <param name="celt"> Number of elements to retrieve/skip. </param>
        /// <param name="rgelt"> Array of T elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned/skipped in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        private int Move(uint celt, T[] rgelt, out uint celtFetched)
        {
            lock (this)
            {
                int hr = VSConstants.S_OK;
                celtFetched = (uint)m_data.Length - m_position;

                if (celt > celtFetched)
                {
                    hr = VSConstants.S_FALSE;
                }
                else if (celt < celtFetched)
                {
                    celtFetched = celt;
                }

                if (rgelt != null)
                {
                    for (int c = 0; c < celtFetched; c++)
                    {
                        rgelt[c] = m_data[m_position + c];
                    }
                }

                m_position += celtFetched;

                return hr;
            }
        }
    }
    #endregion Base Class




    /// <summary>
    /// This class enumerates the processes running on a debug port. (http://msdn.microsoft.com/en-ca/library/bb145005.aspx)
    /// </summary>
    class AD7ProcessEnum : AD7Enum<IDebugProcess2, IEnumDebugProcesses2>, IEnumDebugProcesses2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of IDebugProcess2 elements. </param>
        public AD7ProcessEnum(IDebugProcess2[] data)
            : base(data)
        {
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb147027.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugProcess2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugProcess2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates the ports of a machine or port supplier. (http://msdn.microsoft.com/en-ca/library/bb145137.aspx)
    /// </summary>
    class AD7PortEnum : AD7Enum<IDebugPort2, IEnumDebugPorts2>, IEnumDebugPorts2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of IDebugPort2 elements. </param>
        public AD7PortEnum(IDebugPort2[] data)
            : base(data)
        {
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb147027.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugPort2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugPort2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    
    
    /// <summary>
    /// This class enumerates the programs running in the current debug session. (http://msdn.microsoft.com/en-ca/library/bb146727.aspx)
    /// </summary>
    class AD7ProgramEnum : AD7Enum<IDebugProgram2, IEnumDebugPrograms2>, IEnumDebugPrograms2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of IDebugProgram2 elements. </param>
        public AD7ProgramEnum(IDebugProgram2[] data) : base(data)
        {
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb147027.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugProgram2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugProgram2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates FRAMEINFO structures. (http://msdn.microsoft.com/en-us/library/bb147119.aspx)
    /// </summary>
    class AD7FrameInfoEnum : AD7Enum<FRAMEINFO, IEnumDebugFrameInfo2>, IEnumDebugFrameInfo2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of FRAMEINFO elements. </param>
        public AD7FrameInfoEnum(FRAMEINFO[] data)
            : base(data)
        {
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-us/library/bb146293.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of FRAMEINFO elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, FRAMEINFO[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates DEBUG_PROPERTY_INFO structures. (http://msdn.microsoft.com/en-ca/library/bb162336.aspx)
    /// </summary>
    class AD7PropertyInfoEnum : AD7Enum<DEBUG_PROPERTY_INFO, IEnumDebugPropertyInfo2>, IEnumDebugPropertyInfo2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"> Array of DEBUG_PROPERTY_INFO elements. </param>
        public AD7PropertyInfoEnum(DEBUG_PROPERTY_INFO[] data)
            : base(data)
        {
        }
    }


    /// <summary>
    /// This class enumerates the threads running in the current debug session. (http://msdn.microsoft.com/en-ca/library/bb145142.aspx)
    /// </summary>
    class AD7ThreadEnum : AD7Enum<IDebugThread2, IEnumDebugThreads2>, IEnumDebugThreads2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="threads"> Array of IDebugThread2 elements. </param>
        public AD7ThreadEnum(IDebugThread2[] threads)
            : base(threads)
        {
            
        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb161679.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugThread2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugThread2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates a list of modules. (http://msdn.microsoft.com/en-ca/library/bb145925.aspx)
    /// </summary>
    class AD7ModuleEnum : AD7Enum<IDebugModule2, IEnumDebugModules2>, IEnumDebugModules2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="modules"> Array of IDebugModule2 elements. </param>
        public AD7ModuleEnum(IDebugModule2[] modules)
            : base(modules)
        {

        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb145898.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugModule2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugModule2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates DEBUG_PROPERTY_INFO structures. (http://msdn.microsoft.com/en-ca/library/bb162336.aspx)
    /// </summary>
    class AD7PropertyEnum : AD7Enum<DEBUG_PROPERTY_INFO, IEnumDebugPropertyInfo2>, IEnumDebugPropertyInfo2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties"> Array of DEBUG_PROPERTY_INFO elements. </param>
        public AD7PropertyEnum(DEBUG_PROPERTY_INFO[] properties)
            : base(properties)
        {

        }
    }


    /// <summary>
    /// This class enumerates the code contexts associated with the debug session, or with a particular program or document.
    /// (http://msdn.microsoft.com/en-us/library/bb146612.aspx)
    /// </summary>
    class AD7CodeContextEnum : AD7Enum<IDebugCodeContext2, IEnumDebugCodeContexts2>, IEnumDebugCodeContexts2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="codeContexts"> Array of IDebugCodeContext2 elements. </param>
        public AD7CodeContextEnum(IDebugCodeContext2[] codeContexts)
            : base(codeContexts)
        {

        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. ()http://msdn.microsoft.com/en-us/library/bb145085.aspx
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugCodeContext2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugCodeContext2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }


    /// <summary>
    /// This class enumerates the bound breakpoints associated with a pending breakpoint or breakpoint bound event.
    /// (http://msdn.microsoft.com/en-ca/library/bb162182.aspx)
    /// </summary>
    class AD7BoundBreakpointsEnum : AD7Enum<IDebugBoundBreakpoint2, IEnumDebugBoundBreakpoints2>, IEnumDebugBoundBreakpoints2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="breakpoints"> Array of IDebugBoundBreakpoint2 elements. </param>
        public AD7BoundBreakpointsEnum(IDebugBoundBreakpoint2[] breakpoints)
            : base(breakpoints)
        {

        }


        /// <summary>
        /// Returns the next set of elements from the enumeration. (http://msdn.microsoft.com/en-ca/library/bb161772.aspx)
        /// </summary>
        /// <param name="celt"> The number of elements to retrieve. Also specifies the maximum size of the rgelt array. </param>
        /// <param name="rgelt"> Array of IDebugBoundBreakpoint2 elements to be filled in. </param>
        /// <param name="celtFetched"> Returns the number of elements actually returned in rgelt. </param>
        /// <returns> If successful, returns S_OK. If not, returns S_FALSE. </returns>
        public int Next(uint celt, IDebugBoundBreakpoint2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }  

}
