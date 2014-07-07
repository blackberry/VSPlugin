using System;
using EnvDTE;
using EnvDTE80;

namespace BlackBerry.Package.Helpers
{
    internal static class CommandHelper
    {
        /// <summary>
        /// Registers specified handlers for a command given by guid:id anywhere inside Visual Studio.
        /// </summary>
        public static CommandEvents Register(DTE2 dte, string commandGuid, int commandId, _dispCommandEvents_BeforeExecuteEventHandler beforeHandler,
            _dispCommandEvents_AfterExecuteEventHandler afterHandler)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");
            if (dte.Events == null)
                throw new ArgumentOutOfRangeException("dte");

            if (beforeHandler == null && afterHandler == null)
                return null;

            var commandEvents = dte.Events.CommandEvents[commandGuid, commandId];
            if (commandEvents != null)
            {
                if (beforeHandler != null)
                {
                    commandEvents.BeforeExecute += beforeHandler;
                }
                if (afterHandler != null)
                {
                    commandEvents.AfterExecute += afterHandler;
                }
            }

            return commandEvents;
        }
    }
}
