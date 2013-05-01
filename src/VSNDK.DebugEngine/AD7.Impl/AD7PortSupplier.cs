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
    [ComVisible(false)]
    [Guid("92A2B753-00BD-40FF-9964-6AB64A1D6C9F")]
    public class AD7PortSupplier : IDebugPortSupplier2
    {

        int IDebugPortSupplier2.AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.CanAddPort()
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.GetPortSupplierId(out Guid pguidPortSupplier)
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.GetPortSupplierName(out string pbstrName)
        {
            throw new NotImplementedException();
        }

        int IDebugPortSupplier2.RemovePort(IDebugPort2 pPort)
        {
            throw new NotImplementedException();
        }

    }
}