using System;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class manages events related to output messages.
    /// </summary>
    public sealed class HandleOutputs
    {
        /// <summary>
        /// GDB textual output from the running target to be presented in the VS standard output window.
        /// </summary>
        private string _stdOut = "";

        /// <summary>
        /// Other GDB messages to be presented in the VS standard output window.
        /// </summary>
        private string _console = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private EventDispatcher _eventDispatcher;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventDispatcher"> This object manages debug events in the engine. </param>
        public HandleOutputs(EventDispatcher eventDispatcher)
        {
            if (eventDispatcher == null)
                throw new ArgumentNullException("eventDispatcher");

            _eventDispatcher = eventDispatcher;
        }

        /// <summary>
        /// This method manages events related to output messages by classifying each of them by sub-type.
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void Handle(string ev)
        {
            int ini;
            int end;
            switch (ev[1])
            {
                case '0':  // Display the m_console message in the VS output window. Example: 80,\"\"[New pid 15380494 tid 2]\\n\"\"!80
                    ini = 4;
                    end = ev.IndexOf("\"!80", 4);
                    if (end == -1)
                        end = ev.Length;
                    _console = ev.Substring(ini, (end - ini));

                    // TODO: Call the method/event that will output this message in the VS output window.

                    break;
                case '1':  // Display the m_stdOut message in the VS standard output window. Instruction should look like this: 81,\"\" ... "\"!81
                    ini = 4;
                    end = ev.IndexOf("\"!81", 4);
                    if (end == -1)
                        end = ev.Length;
                    _stdOut = ev.Substring(ini, (end - ini));

                    // TODO: Call the method/event that will output this message in the VS standar output window.

                    break;
            }
        }
    }
}