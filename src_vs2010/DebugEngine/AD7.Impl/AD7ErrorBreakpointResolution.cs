using Microsoft.VisualStudio.Debugger.Interop;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// Represents the resolution of a breakpoint error. (http://msdn.microsoft.com/en-us/library/bb161341.aspx)
    /// </summary>
    class AD7ErrorBreakpointResolution : IDebugErrorBreakpointResolution2
    {
        #region IDebugErrorBreakpointResolution2 Members

        /// <summary>
        /// Gets the breakpoint type. Not implemented. (http://msdn.microsoft.com/en-us/library/bb145065.aspx)
        /// </summary>
        /// <param name="pBPType"> The type of this breakpoint. </param>
        /// <returns> Not implemented. </returns>
        int IDebugErrorBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Gets the breakpoint error resolution information. Not implemented. (http://msdn.microsoft.com/en-us/library/bb161960.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags that determine which fields of pErrorResolutionInfo are to be filled out. </param>
        /// <param name="pErrorResolutionInfo"> The BP_ERROR_RESOLUTION_INFO structure that is filled in with the description of the 
        /// breakpoint resolution. </param>
        /// <returns> Not implemented. </returns>
        int IDebugErrorBreakpointResolution2.GetResolutionInfo(enum_BPERESI_FIELDS dwFields, BP_ERROR_RESOLUTION_INFO[] pErrorResolutionInfo)
        {
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_BPRESLOCATION) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_PROGRAM) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_THREAD) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_MESSAGE) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_TYPE) != 0) { }

            return EngineUtils.NotImplemented();
        }

        #endregion
    }
}
