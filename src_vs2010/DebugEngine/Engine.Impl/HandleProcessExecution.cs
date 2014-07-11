using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.Debugger.Interop;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class manages events related to execution control (processes, threads, programs).
    /// </summary>
    public sealed class HandleProcessExecution
    {
        /// <summary>
        /// Thread ID.
        /// </summary>
        private int _threadId = -1; // when threadId is 0, it means all threads.

        /// <summary>
        /// Process ID.
        /// </summary>
        private int _processId = -1;

        /// <summary>
        /// Name of the signal that caused an interruption.
        /// </summary>
        private string _signalName = "";

        /// <summary>
        /// Meaning of the signal that caused an interruption.
        /// </summary>
        private string _signalMeaning = "";

        /// <summary>
        /// File name.
        /// </summary>
        private string _fileName = "";

        /// <summary>
        /// Line number.
        /// </summary>
        private int _line = -1;

        /// <summary>
        /// Address.
        /// </summary>
        private int _address = -1;

        /// <summary>
        /// Function name.
        /// </summary>
        private string _functionName = "";

        /// <summary>
        /// Error caused by a GDB command that failed.
        /// </summary>
        private string _error = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private readonly EventDispatcher _eventDispatcher;

        /// <summary>
        /// Boolean variable that indicates if GDB has to resume execution after handling what caused it to enter in break mode.
        /// </summary>
        public static bool NeedsResumeAfterInterrupt;

        /// <summary>
        /// Used as a communication signal between the Event Dispatcher and the debug engine method responsible for stopping GDB 
        /// execution (IDebugEngine2.CauseBreak()). So, VS is able to wait for GDB's interruption before entering in break mode.
        /// </summary>
        public static ManualResetEvent m_mre = new ManualResetEvent(false);
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventDispatcher"> This object manages debug events in the engine. </param>
        public HandleProcessExecution(EventDispatcher eventDispatcher)
        {
            if (eventDispatcher == null)
                throw new ArgumentNullException("eventDispatcher");

            _eventDispatcher = eventDispatcher;
        }

        /// <summary>
        /// This method manages events related to execution control by classifying each of them by sub-type (e.g. thread created, program interrupted, etc.).
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void Handle(string ev)
        {
            int ini = 0;
            int end = 0;
            int numCommas = 0;
            switch (ev[0])
            {
                case '4':
                    switch (ev[1])
                    {
                        case '0':  
                            // Thread created. Example: 40,2,20537438
                            EventDispatcher._GDBRunMode = true;
                            ini = 3;
                            end = ev.IndexOf(";", 3);
                            _threadId = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                            ini = end + 1;
                            try
                            {
                                _processId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                            }
                            catch
                            {
                                _processId = 0;
                            }

                            _eventDispatcher.Engine._updateThreads = true;

                            break;
                        case '1':  
                            // Process running. Example: 41,1     (when threadId is 0 means "all threads": example: 41,0)
                            EventDispatcher._GDBRunMode = true;
                            _threadId = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                            break;
                        case '2':  
                            // Program exited normally. Example: 42
                            _eventDispatcher.EndDebugSession(0);

                            break;
                        case '3':  
                            // Program was exited with an exit code. Example: 43,1;1 (not sure if there is a threadID, but the last ";" exist)
                            // TODO: not tested yet
                            end = ev.IndexOf(";", 3);
                            uint exitCode = Convert.ToUInt32(ev.Substring(3, (end - 3)));
                            _eventDispatcher.EndDebugSession(exitCode);

                            break;
                        case '4':  
                            // Program interrupted. 
                            // Examples:
                            // 44,ADDR,FUNC,THREAD-ID         
                            // 44,ADDR,FUNC,FILENAME,LINE,THREAD-ID

                            _eventDispatcher.Engine.ResetStackFrames();
                            EventDispatcher._GDBRunMode = false;
                            numCommas = 0;
                            foreach (char c in ev)
                            {
                                if (c == ';')
                                    numCommas++;
                            }

                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            _address = Convert.ToInt32(ev.Substring(ini, (end - ini)), 16);

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            _functionName = ev.Substring(ini, (end - ini));

                            if (_functionName == "??")
                            {
                                EventDispatcher._unknownCode = true;
                            }
                            else
                            {
                                EventDispatcher._unknownCode = false;
                            }

                            switch (numCommas)
                            {
                                case 3:
                                    // Thread ID
                                    ini = end + 1;
                                    _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    EventDispatcher._unknownCode = true;
                                    break;
                                case 4:
                                    // Filename and line number
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    _fileName = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    _line = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                                case 5:
                                    //  Filename, line number and thread ID
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    _fileName = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                    ini = end + 1;
                                    _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                            }

                            _eventDispatcher.Engine.CleanEvaluatedThreads();


                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }
                            if (_threadId > 0)
                            {
                                _eventDispatcher.Engine.SelectThread(_threadId.ToString()).SetCurrentLocation(_fileName, (uint)_line);
                                _eventDispatcher.Engine.SetAsCurrentThread(_threadId.ToString());
                            }
                            
                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            OnInterrupt(_threadId);

                            // Signal that interrupt is processed 
                            m_mre.Set();

                            break;

                        case '5':  
                            // End-stepping-range.
                            _eventDispatcher.Engine.ResetStackFrames();
                            EventDispatcher._GDBRunMode = false;
                            ini = 3;
                            end = ev.IndexOf(';', 3);
                            if (end == -1)
                                end = ev.Length;
                            string temp = ev.Substring(ini, (end - ini));

                            ini = end + 1;

                            if (ev.Length > ini)
                                end = ev.IndexOf(';', ini);
                            else
                                end = -1;

                            if (end == -1)
                            {
                                // Set sane default values for the missing file and line information 
                                _fileName = "";
                                _line = 1;
                                _threadId = Convert.ToInt32(temp);
                                EventDispatcher._unknownCode = true;
                            }
                            else
                            {
                                _fileName = temp;
                                _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                ini = end + 1;
                                _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher._unknownCode = false;
                            }

                            _eventDispatcher.Engine.CleanEvaluatedThreads();


                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }
                            if (_threadId > 0)
                            {
                                if ((EventDispatcher._unknownCode == false) && (_fileName != ""))
                                    _eventDispatcher.Engine.SelectThread(_threadId.ToString()).SetCurrentLocation(_fileName, (uint)_line);
                                _eventDispatcher.Engine.SetAsCurrentThread(_threadId.ToString());
                            }

                            OnStepCompleted(_eventDispatcher, _fileName, (uint)_line);

                            break;
                        case '6':  
                            // Function-finished.
                            _eventDispatcher.Engine.ResetStackFrames();
                            EventDispatcher._GDBRunMode = false;
                            ini = 3;
                            end = ev.IndexOf(';', 3);
                            if (end == -1)
                            {
                                // Set sane default values for the missing file and line information 
                                _fileName = "";
                                _line = 1;
                                _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher._unknownCode = true;
                            }
                            else
                            {
                                _fileName = ev.Substring(ini, (end - ini));
                                ini = end + 1;
                                end = ev.IndexOf(';', ini);
                                _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                ini = end + 1;
                                _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher._unknownCode = false;
                            }

                            _eventDispatcher.Engine.CleanEvaluatedThreads();

                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }
                            if (_threadId > 0)
                            {
                                if ((EventDispatcher._unknownCode == false) && (_fileName != ""))
                                    _eventDispatcher.Engine.SelectThread(_threadId.ToString()).SetCurrentLocation(_fileName, (uint)_line);
                                _eventDispatcher.Engine.SetAsCurrentThread(_threadId.ToString());
                            }

                            OnStepCompleted(_eventDispatcher, _fileName, (uint)_line);

                            break;
                        case '7':  
                            // -exec-interrupt or signal-meaning="Killed". There's nothing to do in this case.
                            _eventDispatcher.Engine.ResetStackFrames();
                            EventDispatcher._GDBRunMode = false;

                            _eventDispatcher.Engine.CleanEvaluatedThreads();

                            _threadId = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }
                            if (_threadId > 0)
                            {
                                if ((EventDispatcher._unknownCode == false) && (_fileName != ""))
                                    _eventDispatcher.Engine.SelectThread(_threadId.ToString()).SetCurrentLocation(_fileName, (uint)_line);
                                _eventDispatcher.Engine.SetAsCurrentThread(_threadId.ToString());
                            }

                            if (_eventDispatcher.Engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
                            {
                                OnInterrupt(_threadId);
                            }
                            // Signal that interrupt is processed 
                            m_mre.Set();

                            break;
                        case '8':  
                            // SIGKILL
                            _eventDispatcher.EndDebugSession(0);
                            break;
                        case '9':  
                            // ERROR, ex: 49,Cannot find bounds of current function
                            _eventDispatcher.Engine.ResetStackFrames();
                            _eventDispatcher.Engine.CleanEvaluatedThreads();

                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }

                            if (ev.Length >= 3)
                            {
                                _error = ev.Substring(3, (ev.Length - 3));
                                if (_error == "Cannot find bounds of current function")
                                {
                                    // We don't have symbols for this function so further stepping won't be possible. Return from this function.
                                    EventDispatcher._unknownCode = true;
                                    _eventDispatcher.Engine.Step(_eventDispatcher.Engine.CurrentThread(), enum_STEPKIND.STEP_OUT, enum_STEPUNIT.STEP_LINE);
                                }
                            }
                            break;
                    }
                    break;
                case '5':
                    switch (ev[1])
                    {
                        case '0':  
                            // Quit (expect signal SIGINT when the program is resumed)
                            _eventDispatcher.countSIGINT += 1;
                            if (_eventDispatcher.countSIGINT > 5)
                            {
                                _eventDispatcher.EndDebugSession(0);
                                MessageBox.Show("Lost communication with GDB. Please refer to documentation for more details.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        case '1':  
                            // Thread exited. Example: 51,2
                            ini = 3;
                            _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                            _eventDispatcher.Engine._updateThreads = true;

                            break;
                        case '2':  
                            // GDB Bugs, like "... 2374: internal-error: frame_cleanup_after_sniffer ...". Example: 52
                            _eventDispatcher.EndDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen when interrupting GDB's execution by hitting the \"break all\" or toggling a breakpoint in run mode. \n\n GDB CRASHED. Details: \"../../gdb/frame.c:2374: internal-error: frame_cleanup_after_sniffer: Assertion `frame->prologue_cache == NULL' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '3':  
                            // Lost communication with device/simulator: ^error,msg="Remote communication error: No error."
                            MessageBox.Show("Lost communication with the device/simulator.", "Communication lost", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            _eventDispatcher.EndDebugSession(0);

                            break;
                        case '4':  
                            // Program interrupted due to a segmentation fault. 
                            // Examples:
                            // 54,ADDR,FUNC,THREAD-ID         
                            // 54,ADDR,FUNC,FILENAME,LINE,THREAD-ID

                            _eventDispatcher.Engine.ResetStackFrames();
                            EventDispatcher._GDBRunMode = false;
                            numCommas = 0;
                            foreach (char c in ev)
                            {
                                if (c == ';')
                                    numCommas++;
                            }

                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            _address = Convert.ToInt32(ev.Substring(ini, (end - ini)), 16);

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            _functionName = ev.Substring(ini, (end - ini));

                            if (_functionName == "??")
                            {
                                EventDispatcher._unknownCode = true;
                            }
                            else
                            {
                                EventDispatcher._unknownCode = false;
                            }

                            switch (numCommas)
                            {
                                case 3:
                                    // Thread ID
                                    ini = end + 1;
                                    _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    EventDispatcher._unknownCode = true;
                                    break;
                                case 5:
                                    //  Filename, line number and thread ID
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    _fileName = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    _line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                    ini = end + 1;
                                    _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                            }

                            MessageBox.Show("Segmentation Fault: If you continue debugging could take the environment to an unstable state.", "Segmentation Fault", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            _eventDispatcher.Engine.CleanEvaluatedThreads();

                            if (_eventDispatcher.Engine._updateThreads)
                            {
                                _eventDispatcher.Engine.UpdateListOfThreads();
                            }
                            if (_threadId > 0)
                            {
                                _eventDispatcher.Engine.SelectThread(_threadId.ToString()).SetCurrentLocation(_fileName, (uint)_line);
                                _eventDispatcher.Engine.SetAsCurrentThread(_threadId.ToString());
                            }

                            OnInterrupt(_threadId);

                            break;

                        case '5':  
                            // Exited-signaled. Ex: 55;SIGSEGV;Segmentation fault
                            // or Aborted. Ex: 55;SIGABRT;Aborted
                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            _signalName = ev.Substring(ini, (end - ini));

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            _signalMeaning = ev.Substring(ini, (end - ini));

                            ini = end + 1;
                            try
                            {
                                _threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                            }
                            catch
                            {
                            }

                            if (_signalMeaning == "Segmentation fault")
                            {
                                MessageBox.Show("Segmentation Fault: Closing debugger.", "Segmentation Fault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                _eventDispatcher.EndDebugSession(0);
                            }

                            if (_signalMeaning == "Aborted")
                            {
                                MessageBox.Show("Program aborted: Closing debugger.", "Program Aborted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                _eventDispatcher.EndDebugSession(0);
                            }

                            break;
                        case '6':  
                            // GDB Bugs, like "... 3550: internal-error: handle_inferior_event ...". Example: 56
                            _eventDispatcher.EndDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen while debugging multithreaded programs. \n\n GDB CRASHED. Details: \"../../gdb/infrun.c:3550: internal-error: handle_inferior_event: Assertion ptid_equal (singlestep_ptid, ecs->ptid)' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '7':  // Not used
                            break;
                        case '8':  // Not used
                            break;
                        case '9':  // Not used
                            break;
                    }
                    break;
            }
        }


        /// <summary>
        /// Update VS when a step action is completed in GDB.
        /// </summary>
        /// <param name="eventDispatcher"> This object manages debug events in the engine. </param>
        /// <param name="file"> File name. </param>
        /// <param name="line"> Line number. </param>
        public static void OnStepCompleted(EventDispatcher eventDispatcher, string file, uint line)
        {
            if (eventDispatcher.Engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
            {
                eventDispatcher.Engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                // Visual Studio shows the line position one more than it actually is
                eventDispatcher.Engine._docContext = eventDispatcher.GetDocumentContext(file, line - 1);
                AD7StepCompletedEvent.Send(eventDispatcher.Engine);
            }
        }


        /// <summary>
        /// Update VS when the debugging process is interrupted in GDB.
        /// </summary>
        /// <param name="threadID"> Thread ID. </param>
        private void OnInterrupt(int threadID)
        {
            Debug.Assert(_eventDispatcher.Engine.m_state == AD7Engine.DE_STATE.RUN_MODE);
            _eventDispatcher.Engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

            if (_fileName != "" && _line > 0)
            {
                // Visual Studio shows the line position one more than it actually is
                _eventDispatcher.Engine._docContext = _eventDispatcher.GetDocumentContext(_fileName, (uint)(_line - 1));
            }

            // Only send OnAsyncBreakComplete if break-all was requested by the user
            if (!NeedsResumeAfterInterrupt)
            {
                _eventDispatcher.Engine.Callback.OnAsyncBreakComplete(_eventDispatcher.Engine.SelectThread(threadID.ToString()));
            }
        }
    }
}