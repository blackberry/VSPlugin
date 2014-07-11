using System;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class manages breakpoints events.
    /// </summary>
    public sealed class HandleBreakpoints
    {
        /// <summary>
        /// GDB breakpoint ID.
        /// </summary>
        private int _number = -1;

        /// <summary>
        /// Boolean variable that indicates if this breakpoint is enable (true) or disable (false).
        /// </summary>
        private bool _enabled;

        /// <summary>
        /// Breakpoint address.
        /// </summary>
        private string _address = "";

        /// <summary>
        /// Name of the function that contains this breakpoint.
        /// </summary>
        private string _functionName = "";

        /// <summary>
        /// File name that contains this breakpoint.
        /// </summary>
        private string _fileName = "";

        /// <summary>
        /// Line number for this breakpoint.
        /// </summary>
        private int _line = -1;

        /// <summary>
        /// Number of hits for this breakpoint.
        /// </summary>
        private int _hits = -1;

        /// <summary>
        /// Number of hits to be ignored by this breakpoint.
        /// </summary>
        private int _ignoreHits = -1;

        /// <summary>
        /// Condition associated to this breakpoint.
        /// </summary>
        private string _condition = "";

        /// <summary>
        /// Thread ID that was interrupted when this breakpoint was hit.
        /// </summary>
        private string _threadID = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private EventDispatcher _eventDispatcher;

        /// <summary>
        /// GDB_ID Property
        /// </summary>
        public int Number 
        {
            get { return _number; }
        }

        /// <summary>
        /// GDB Line Position Property
        /// </summary>
        public int LinePosition
        {
            get { return _line; }
        }

        /// <summary>
        /// GDB File name
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// GDB Address
        /// </summary>
        public string Address
        {
            get { return _address; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ed"> This object manages debug events in the engine. </param>
        public HandleBreakpoints(EventDispatcher ed)
        {
            _eventDispatcher = ed;
        }
        
        /// <summary>
        /// This method manages breakpoints events by classifying each of them by sub-type (e.g. breakpoint inserted, modified, etc.).
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void Handle(string ev)
        {
            int ini;
            int end;
            switch (ev[1])
            {
                case '0':  
                    // Breakpoint inserted (synchronous). 
                    // Example: 20,1,y,0x0804d843,main,C:/Users/xxxxx/vsplugin-ndk/samples/Square/Square/main.c,319,0       
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    _number = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    if (ev[end + 1] == 'y')
                        _enabled = true;
                    else
                        _enabled = false;

                    ini = end + 3;
                    end = ev.IndexOf(';', ini);
                    _address = ev.Substring(ini, (end - ini));
                    if (_address == "<PENDING>")
                    {
                        _functionName = "??";
                        EventDispatcher._unknownCode = true;
                        _fileName = "";
                        _line = 0;
                        _hits = 0;
                        return;
                    }

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _functionName = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _fileName = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    ini = end + 1;
                    _hits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));                   

                    break;
                case '1':  
                    // Breakpoint modified (asynchronous). 
                    // Example: 21,1,y,0x0804d843,main,C:/Users/xxxxxx/vsplugin-ndk/samples/Square/Square/main.c,318,1
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    _number = Convert.ToInt32(ev.Substring(ini, (end - ini))); ;

                    if (ev[end + 1] == 'y')
                        _enabled = true;
                    else
                        _enabled = false;

                    ini = end + 3;
                    end = ev.IndexOf(';', ini);
                    _address = ev.Substring(ini, (end - ini));

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _functionName = ev.Substring(ini, end - ini);

                    // Need to set the flag for unknown code if necessary.
                    if (_functionName == "??")
                    {
                        EventDispatcher._unknownCode = true;
                    }
                    else
                    {
                        EventDispatcher._unknownCode = false;
                    }

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _fileName = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    ini = end + 1;
                    _hits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                    // Update hit count on affected bound breakpoint.
                    _eventDispatcher.UpdateHitCount((uint)_number, (uint)_hits);

                    break;
                case '2':  
                    // Breakpoint deleted asynchronously (a temporary breakpoint). Example: 22,2\r\n
                    _number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                    break;
                case '3':  
                    // Breakpoint enabled. Example: 23 (enabled all) or 23,1 (enabled only breakpoint 1)
                    if (ev.Length > 2)
                        _number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        _number = 0;  // 0 means ALL breakpoints.

                    break;
                case '4':  
                    // Breakpoint disabled. Example: 24 (disabled all) or 24,1 (disabled only breakpoint 1)
                    if (ev.Length > 2)
                        _number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        _number = 0;  // 0 means ALL breakpoints.

                    break;
                case '5':  
                    // Breakpoint deleted. Example: 25 (deleted all) or 25,1 (deleted only breakpoint 1)
                    if (ev.Length > 2)
                        _number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        _number = 0;  // 0 means ALL breakpoints.

                    break;
                case '6':  
                    // Break after "n" hits (or ignore n hits). Example: 26;1;100
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    _number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                    ini = end + 1;
                    _ignoreHits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                    break;
                case '7':  
                    // Breakpoint hit. 
                    // Example: 27,1,C:/Users/xxxxxx/vsplugin-ndk/samples/Square/Square/main.c,319;1\r\n
                    bool updatingCondBreak = _eventDispatcher.Engine._updatingConditionalBreakpoint.WaitOne(0);
                    if (updatingCondBreak)
                    {

                        _eventDispatcher.Engine.ResetStackFrames();

                        ini = 3;
                        end = ev.IndexOf(';', ini);
                        _number = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                        ini = end + 1;
                        end = ev.IndexOf(';', ini);
                        _fileName = ev.Substring(ini, end - ini);

                        ini = end + 1;
                        end = ev.IndexOf(';', ini);
                        _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                        ini = end + 1;
                        _threadID = ev.Substring(ini, (ev.Length - ini));

                        _eventDispatcher.Engine.CleanEvaluatedThreads();

                        // Call the method/event that will stop SDM because a breakpoint was hit here.
                        if (_eventDispatcher.Engine._updateThreads)
                        {
                            _eventDispatcher.Engine.UpdateListOfThreads();
                        }
                        _eventDispatcher.Engine.SelectThread(_threadID).SetCurrentLocation(_fileName, (uint)_line);
                        _eventDispatcher.Engine.SetAsCurrentThread(_threadID);

                        // A breakpoint can be hit during a step
                        if (_eventDispatcher.Engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
                        {
                            HandleProcessExecution.OnStepCompleted(_eventDispatcher, _fileName, (uint)_line);
                        }
                        else
                        {
                            // Visual Studio shows the line position one more than it actually is
                            _eventDispatcher.Engine._docContext = _eventDispatcher.GetDocumentContext(_fileName, (uint)(_line - 1));
                            _eventDispatcher.BreakpointHit((uint)_number, _threadID);
                        }
                        _eventDispatcher.Engine._updatingConditionalBreakpoint.Set();
                    }
                    break;
                case '8':  
                    // Breakpoint condition set. Example: 28;1;expression
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    if (end != -1)
                    {
                        _number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                        ini = end + 1;
                        _condition = ev.Substring(ini, (ev.Length - ini));
                    }
                    else
                    {
                        _number = Convert.ToInt32(ev.Substring(3));
                        _condition = "";
                    }
                    break;
                case '9':  // Error in testing breakpoint condition
                    break;
            }
        }
    }
}