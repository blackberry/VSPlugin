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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This interface supplies ports to the session debug manager (SDM). (http://msdn.microsoft.com/en-ca/library/bb145819.aspx)
    /// It is not implemented yet, but it seems to be needed to enable updating the list of processes in the "attach to process" 
    /// user interface.
    /// </summary>
    [ComVisible(false)]
    [Guid("92A2B753-00BD-40FF-9964-6AB64A1D6C9F")]
    public class AD7PortSupplier : IDebugPortSupplier2
    {

        /// <summary>
        /// Adds a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb161980.aspx)
        /// </summary>
        /// <param name="pRequest"> An IDebugPortRequest2 object that describes the port to be added. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Verifies that a port supplier can add new ports. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb145880.aspx)
        /// </summary>
        /// <returns> Not implemented. It should returns S_OK if the port can be added, or S_FALSE to indicate no ports can be added 
        /// to this port supplier. </returns>
        int IDebugPortSupplier2.CanAddPort()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Retrieves a list of all the ports supplied by a port supplier. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb146984.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPorts2 object containing a list of ports supplied. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets a port from a port supplier. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb161812.aspx)
        /// </summary>
        /// <param name="guidPort"> Globally unique identifier (GUID) of the port. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets the port supplier identifier. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb146617.aspx)
        /// </summary>
        /// <param name="pguidPortSupplier"> Returns the GUID of the port supplier. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.GetPortSupplierId(out Guid pguidPortSupplier)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets the port supplier name. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162136.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port supplier. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.GetPortSupplierName(out string pbstrName)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Removes a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162306.aspx)
        /// </summary>
        /// <param name="pPort"> An IDebugPort2 object that represents the port to be removed. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.RemovePort(IDebugPort2 pPort)
        {
            throw new NotImplementedException();
        }

    }
}