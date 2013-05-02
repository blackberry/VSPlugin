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

namespace VSNDK.AddIn
{
    //Command IDs exposed by VisualStudio
    public static class CommandConstants
    {
        public const int cmdidAddSolutionSCC = 21016;
        public const int cmdidUndoCheckoutSCC = 21009;
        public const int cmdidGetLatestVersionSCC = 21011;
        public const int cmdidGetSCC = 21501;
        public const int cmdidViewHistorySCC = 21508;
        public const int cmdidStartDebug = 295;
        public const int cmdidStartDebugContext = 356;
        public const int cmdidRestartDebug = 296;
        public const int cmdidStopDebug = 179;
        public const int cmdidPreviewInBrowser = 334;
        public const int cmdidBrowseWith = 336;
        public const int cmdidStartNoDebug = 368;
        public const int cmdidSolutionCfg = 684;
        public const int cmdidSolutionPlatform = 1990;
        public const int cmdidSolutionPlatformGetList = 1991;
        public const int cmdidDebugBreakatFunction = 311;
    }
}
