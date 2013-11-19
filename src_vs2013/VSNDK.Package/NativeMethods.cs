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
using System.Runtime.InteropServices;

namespace RIM.VSNDK_Package
{
    /// <summary>
    /// This class will contain all methods that we need to import.
    /// </summary>
    internal class NativeMethods 
    {
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_MBUTTONDOWN = 0x0207;

        //Including a private constructor to prevent a compiler-generated default constructor
        private NativeMethods()
        {
        }

        // Import the SendMessage function from user32.dll
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd,
                                                int Msg,
                                                IntPtr wParam,
                                                [MarshalAs(UnmanagedType.IUnknown)] out object lParam);
    }
}