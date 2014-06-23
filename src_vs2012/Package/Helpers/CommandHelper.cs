using System;
using EnvDTE;
using EnvDTE80;

namespace BlackBerry.Package.Helpers
{
    internal sealed class CommandHelper
    {
        /// <summary>
        /// Registers specified handlers for a command given by guid:id anywhere inside Visual Studio.
        /// </summary>
        public static void Register(DTE2 dte, string commandGuid, int commandId, _dispCommandEvents_AfterExecuteEventHandler afterHandler,
            _dispCommandEvents_BeforeExecuteEventHandler beforeHandler)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");
            if (dte.Events == null)
                throw new ArgumentOutOfRangeException("dte");

            var commandEvents = dte.Events.CommandEvents[commandGuid, commandId];
            if (commandEvents != null)
            {
                commandEvents.BeforeExecute += beforeHandler;
                commandEvents.AfterExecute += afterHandler;
            }
        }
    }
}
