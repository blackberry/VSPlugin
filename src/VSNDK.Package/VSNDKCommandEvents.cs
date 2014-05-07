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
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;

namespace RIM.VSNDK_Package
{

    /// <summary> 
    /// Responsible for customization of VSShell command events for VSNDK. 
    /// </summary>
    public class VSNDKCommandEvents
    {
        private DTE2 dte;
        private Dictionary<int, CommandEvents> cmdEvents;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dte"> Application Object. </param>
        public VSNDKCommandEvents(DTE2 dte)
        {
            this.dte = dte;
            cmdEvents = new Dictionary<int, CommandEvents>();
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="cmdId"></param>
        /// <param name="afterHandler"></param>
        /// <param name="beforeHandler"></param>
        public void RegisterCommand( string guid, int cmdId, _dispCommandEvents_AfterExecuteEventHandler afterHandler, _dispCommandEvents_BeforeExecuteEventHandler beforeHandler)
        {
            cmdEvents[cmdId] = dte.Events.get_CommandEvents(guid, cmdId);
            CommandEvents e = cmdEvents[cmdId];
            if ( e != null)
            {
                e.AfterExecute += afterHandler;
                e.BeforeExecute += beforeHandler;
            }
        }
    }
}
