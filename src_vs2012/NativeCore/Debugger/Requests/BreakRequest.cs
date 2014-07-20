using System;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Debugger.Requests
{
    /// <summary>
    /// Class wrapping sending Ctrl+C to the GDB. Since it will be a request,
    /// it could be added into the queue and send at designated time,
    /// without any worries that it was executed in the middle of other request execution.
    /// </summary>
    public sealed class BreakRequest : Request
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BreakRequest()
            : base("-exec-interrupt")
        {
        }

        /// <summary>
        /// Method executed to perform request's action. It should return 'true', when all succeeded.
        /// </summary>
        public override bool Execute(IGdbSender sender)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");

            try
            {
                sender.Break();
                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex);
                return false;
            }
        }
    }
}
