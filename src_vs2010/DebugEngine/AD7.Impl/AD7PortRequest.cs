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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class describes a port. This description is used to add the port to a port supplier.
    /// 
    /// It implements IDebugPortRequest2 (http://msdn.microsoft.com/en-us/library/bb146168.aspx) 
    /// </summary>
    public class AD7PortRequest : IDebugPortRequest2
    {
        /// <summary>
        /// The name of the port to be created.
        /// </summary>
        private string m_name;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"> The name of the port. It is contains the IP and password of the Device/Simulator. The password must be
        /// removed when creating the port because this name will be displayed in Attach to Process UI. </param>
        public AD7PortRequest(string name)
        {
            m_name = name;
        }

        #region Implementation of IDebugPortRequest2

        /// <summary>
        /// Gets the name of the port to create.
        /// </summary>
        /// <param name="portName"> Returns the name of the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortName(out string portName)
        {
            portName = m_name;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
