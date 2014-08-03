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
    /// This class represents a module loaded in the debuggee process to the debugger. Not implemented, yet.
    ///
    /// It implements:
    ///
    /// IDebugModule2: This interface represents a module—that is, an executable unit of a program—such as a DLL.
    /// (http://msdn.microsoft.com/en-ca/library/bb145360.aspx)
    /// 
    /// IDebugModule3: This interface represents a module that supports alternate locations of symbols and JustMyCode states.
    /// (http://msdn.microsoft.com/en-ca/library/bb145893.aspx)
    /// </summary>
    public sealed class AD7Module : IDebugModule2, IDebugModule3
    {
        /// <summary>
        /// Gets information about this module. (http://msdn.microsoft.com/en-ca/library/bb161975.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the MODULE_INFO_FIELDS enumeration that specify which fields of pInfo 
        /// are to be filled out. </param>
        /// <param name="infoArray"> A MODULE_INFO structure that is filled in with a description of the module. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugModule2.GetInfo(enum_MODULE_INFO_FIELDS flags, MODULE_INFO[] infoArray)
        {
            try
            {
                MODULE_INFO info = new MODULE_INFO();

                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_NAME))
                {
                    info.m_bstrName = "";
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_NAME;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_URL))
                {
                    info.m_bstrUrl = "";
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URL;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS))
                {
                    info.m_addrLoadAddress = 0;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS))
                {
                    // A debugger that actually supports showing the preferred base should crack the PE header and get 
                    // that field. This debugger does not do that, so assume the module loaded where it was suppose to.                   
                    info.m_addrPreferredLoadAddress = 0;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_SIZE))
                {
                    info.m_dwSize = 0;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_SIZE;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_LOADORDER))
                {
                    info.m_dwLoadOrder = 0;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADORDER;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION))
                {
                    info.m_bstrUrlSymbolLocation = "";
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_FLAGS))
                {
                    info.m_dwModuleFlags = 0;
                    info.m_dwModuleFlags |= (enum_MODULE_FLAGS.MODULE_FLAG_SYMBOLS);
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_FLAGS;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_VERSION))
                {
                    info.m_bstrVersion = "";
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_VERSION;
                }
                if (flags.HasFlag(enum_MODULE_INFO_FIELDS.MIF_DEBUGMESSAGE))
                {
                    info.m_bstrDebugMessage = "";
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_DEBUGMESSAGE;
                }

                infoArray[0] = info;
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented
        
        /// <summary>
        /// OBSOLETE. DO NOT USE. Reloads the symbols for this module. (http://msdn.microsoft.com/en-ca/library/bb145113.aspx)
        /// </summary>
        /// <param name="urlToSymbols"> The path to the symbol store. </param>
        /// <param name="debugMessage"> Returns an informational message, such as a status or error message, that is displayed to the 
        /// right of the module name in the Modules window. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugModule2.ReloadSymbols_Deprecated(string urlToSymbols, out string debugMessage)
        {
            debugMessage = null;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// This method is not presented in IDebugModule3 webpage but the debug engine fails to build without it. It should have the same
        /// behavior as the above IDebugModule2.ReloadSymbols_Deprecated. (http://msdn.microsoft.com/en-ca/library/bb145893.aspx)
        /// </summary>
        /// <param name="urlToSymbols"></param>
        /// <param name="pbstrDebugMessage"></param>
        /// <returns> Not implemented. </returns>
        int IDebugModule3.ReloadSymbols_Deprecated(string urlToSymbols, out string pbstrDebugMessage)
        {
            pbstrDebugMessage = null;
            return EngineUtils.NotImplemented();
        }

        #endregion

        /// <summary>
        /// This method is not presented in IDebugModule3 webpage but the debug engine fails to build without it. 
        /// (http://msdn.microsoft.com/en-ca/library/bb145893.aspx). It should have the same behavior as the above 
        /// IDebugModule2.GetInfo, i.e., gets information about this module. (http://msdn.microsoft.com/en-ca/library/bb161975.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the MODULE_INFO_FIELDS enumeration that specify which fields of pInfo 
        /// are to be filled out. </param>
        /// <param name="pinfo"> A MODULE_INFO structure that is filled in with a description of the module. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugModule3.GetInfo(enum_MODULE_INFO_FIELDS flags, MODULE_INFO[] pinfo)
        {
            return ((IDebugModule2)this).GetInfo(flags, pinfo);
        }

        /// <summary>
        /// Returns a list of paths searched for symbols and the results of searching each path. 
        /// [http://msdn.microsoft.com/en-ca/library/bb161971(v=vs.100).aspx]
        /// </summary>
        /// <param name="flags"> A combination of flags from the SYMBOL_SEARCH_INFO_FIELDS enumeration specifying which fields 
        /// of pInfo are to be filled in. </param>
        /// <param name="pinfo"> A MODULE_SYMBOL_SEARCH_INFO structure whose members are to be filled in with the specified information. 
        /// If this is a null value, this method returns E_INVALIDARG. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugModule3.GetSymbolInfo(enum_SYMBOL_SEARCH_INFO_FIELDS flags, MODULE_SYMBOL_SEARCH_INFO[] pinfo)
        {
            pinfo[0] = new MODULE_SYMBOL_SEARCH_INFO();
            pinfo[0].dwValidFields = 1; // SSIF_VERBOSE_SEARCH_INFO;
            pinfo[0].bstrVerboseSearchInfo = "Symbols not loaded";
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Retrieves information on whether the module represents user code or not.  Used to support the JustMyCode features of the 
        /// debugger. [http://msdn.microsoft.com/en-ca/library/bb146644(v=vs.100).aspx]
        /// The VSNDK debug engine does not support JustMyCode and therefore all modules are considered "My Code"
        /// </summary>
        /// <param name="pIsUserCode"> Nonzero (TRUE) if module represents user code, zero (FALSE) if it does not. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugModule3.IsUserCode(out int pIsUserCode)
        {
            pIsUserCode = 1;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Loads and initializes symbols for the current module when the user explicitly asks for them to load. Not implemented.
        /// [http://msdn.microsoft.com/en-ca/library/bb146634(v=vs.100).aspx]
        /// </summary>
        /// <returns> Not implemented. </returns>
        int IDebugModule3.LoadSymbols()
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Specifies whether the module should be considered user code or not. Used to support the JustMyCode features of the debugger.
        /// [http://msdn.microsoft.com/en-ca/library/bb146327(v=vs.100).aspx]
        /// The VSNDK debug engine does not support JustMyCode so this is not implemented
        /// </summary>
        /// <param name="fIsUserCode"> Nonzero (TRUE) if the module should be considered user code, zero (FALSE) if it should not. </param>
        /// <returns> Not implemented. </returns>
        int IDebugModule3.SetJustMyCodeState(int fIsUserCode)
        {
            return EngineUtils.NotImplemented();
        }
    }
}
