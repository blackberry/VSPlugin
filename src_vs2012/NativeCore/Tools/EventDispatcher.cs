using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Helper class for creating cross-thread event dispatchers.
    /// </summary>
    public static class EventDispatcher
    {
        #region Windows Forms

        class WindowsFormsEventDispatcher : IEventDispatcher
        {
            private readonly Control _control;

            public WindowsFormsEventDispatcher(Control control)
            {
                if (control == null)
                    throw new ArgumentNullException("control");
                _control = control;
            }

            #region IEventDispatcher Implementation

            public bool IsSynchronous
            {
                get { return true; }
            }

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    if (_control.InvokeRequired)
                    {
                        _control.Invoke(eventHandler, sender, e);
                    }
                    else
                    {
                        eventHandler(sender, e);
                    }
                }
            }

            public void Invoke(Action action)
            {
                if (action != null)
                {
                    if (_control.InvokeRequired)
                    {
                        _control.Invoke(action);
                    }
                    else
                    {
                        action();
                    }
                }
            }

            public void Invoke<T>(Action<T> action, T e)
            {
                if (action != null)
                {
                    if (_control.InvokeRequired)
                    {
                        _control.Invoke(action, e);
                    }
                    else
                    {
                        action(e);
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Classic

        /// <summary>
        /// Class for calling all events synchronously in the same thread.
        /// </summary>
        class ClassicEventDispatcher : IEventDispatcher
        {
            #region IEventDispatcher Implementation

            public bool IsSynchronous
            {
                get { return true; }
            }

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    eventHandler(sender, e);
                }
            }

            public void Invoke(Action action)
            {
                if (action != null)
                {
                    action();
                }
            }

            public void Invoke<T>(Action<T> action, T e)
            {
                if (action != null)
                {
                    action(e);
                }
            }

            #endregion
        }

        class ClassicAsyncEventDispatcher : IEventDispatcher
        {
            #region Private Classes

            abstract class AsyncTask
            {
                public abstract void Execute();
            }

            class EventHandlerTask<T> : AsyncTask  where T : EventArgs
            {
                private readonly EventHandler<T> _eventHandler;
                private readonly object _sender;
                private readonly T _e;

                public EventHandlerTask(EventHandler<T> eventHandler, object sender, T e)
                {
                    if (eventHandler == null)
                        throw new ArgumentNullException("eventHandler");

                    _eventHandler = eventHandler;
                    _sender = sender;
                    _e = e;
                }

                public override void Execute()
                {
                    _eventHandler(_sender, _e);
                }
            }

            class ActionTask : AsyncTask
            {
                private readonly Action _action;

                public ActionTask(Action action)
                {
                    if (action == null)
                        throw new ArgumentNullException("action");

                    _action = action;
                }

                public override void Execute()
                {
                    _action();
                }
            }

            class ActionTask<T> : AsyncTask
            {
                private readonly Action<T> _action;
                private readonly T _e;

                public ActionTask(Action<T> action, T e)
                {
                    if (action == null)
                        throw new ArgumentNullException("action");

                    _action = action;
                    _e = e;
                }

                public override void Execute()
                {
                    _action(_e);
                }
            }

            #endregion

            private readonly AutoResetEvent _dataAvailableEvent;
            private readonly Queue<AsyncTask> _tasks;
            private readonly Thread _worker;

            public ClassicAsyncEventDispatcher(string name)
            {
                _dataAvailableEvent = new AutoResetEvent(false);
                _tasks = new Queue<AsyncTask>();

                _worker = new Thread(DispatchEvents);
                _worker.Name = name;
                _worker.IsBackground = true; // won't block closing of the process
                _worker.Start();
            }

            private void DispatchEvents()
            {
                AsyncTask task = null;

                while (true)
                {
                    // get pending task:
                    lock (_tasks)
                    {
                        if (_tasks.Count > 0)
                        {
                            task = _tasks.Dequeue();
                        }
                    }

                    if (task == null)
                    {
                        // wait for next task:
                        _dataAvailableEvent.WaitOne(Timeout.Infinite);
                    }
                    else
                    {
                        task.Execute();
                        task = null;
                    }
                }
            }

            public bool IsSynchronous
            {
                get { return false; }
            }

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    Add(new EventHandlerTask<T>(eventHandler, sender, e));
                }
            }

            public void Invoke(Action action)
            {
                if (action != null)
                {
                    Add(new ActionTask(action));
                }
            }

            public void Invoke<T>(Action<T> action, T e)
            {
                if (action != null)
                {
                    Add(new ActionTask<T>(action, e));
                }
            }

            private void Add(AsyncTask task)
            {
                if (task == null)
                    throw new ArgumentNullException("task");

                lock (_tasks)
                {
                    _tasks.Enqueue(task);
                }

                // and wake-up the executing thread:
                _dataAvailableEvent.Set();
            }
        }

        #endregion

        #region Dependency Object (WPF/Silverlight)

        class DependencyObjectEventDispatcher : IEventDispatcher
        {
            private readonly Dispatcher _dispatcher;

            public DependencyObjectEventDispatcher()
            {
                // PH: could use System.Windows.Deployment.Current.Dispatcher instead
                // but that would require reference to System.Windows.dll...
                _dispatcher = Dispatcher.CurrentDispatcher;
                if (_dispatcher == null)
                    throw new InvalidOperationException("Could not create dispatcher object associated with current thread");
            }

            public DependencyObjectEventDispatcher(DependencyObject control)
            {
                if (control == null)
                    throw new ArgumentNullException("control");
                _dispatcher = control.Dispatcher;
            }

            #region IEventDispatcher Implementation

            public bool IsSynchronous
            {
                get { return false; }
            }

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    if (_dispatcher.CheckAccess())
                    {
                        eventHandler(sender, e);
                    }
                    else
                    {
                        _dispatcher.BeginInvoke(eventHandler, sender, e);
                    }
                }
            }

            public void Invoke(Action action)
            {
                if (action != null)
                {
                    if (_dispatcher.CheckAccess())
                    {
                        action();
                    }
                    else
                    {
                        _dispatcher.BeginInvoke(action);
                    }
                }
            }

            public void Invoke<T>(Action<T> action, T e)
            {
                if (action != null)
                {
                    if (_dispatcher.CheckAccess())
                    {
                        action(e);
                    }
                    else
                    {
                        _dispatcher.BeginInvoke(action, e);
                    }
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Creates an event dispatcher for invoking events in the same thread.
        /// </summary>
        public static IEventDispatcher New()
        {
            return new ClassicEventDispatcher();
        }

        /// <summary>
        /// Creates an event dispatcher for invoking events withing .NET default threadpool.
        /// </summary>
        public static IEventDispatcher NewAsync(string name)
        {
            return new ClassicAsyncEventDispatcher(name);
        }

        /// <summary>
        /// Creates an event dispatcher for WPF/Silverlight UI thread.
        /// </summary>
        public static IEventDispatcher NewForWPF()
        {
            return new DependencyObjectEventDispatcher();
        }

        /// <summary>
        /// Creates an event dispatcher for Windows Forms UI thread.
        /// </summary>
        public static IEventDispatcher From(Control control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            return new WindowsFormsEventDispatcher(control);
        }

        /// <summary>
        /// Creates an event dispatcher for WPF/Silverlight UI thread.
        /// </summary>
        public static IEventDispatcher From(DependencyObject control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            return new DependencyObjectEventDispatcher(control);
        }
    }
}
