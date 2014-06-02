using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Interface simplifying events invocations.
    /// </summary>
    internal interface IEventDispatcher
    {
        void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs;
        void Invoke<T>(Action<T> action, T e);
    }

    /// <summary>
    /// Helper class for creating cross-thread event dispatchers.
    /// </summary>
    internal static class EventDispatcher
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

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    eventHandler(sender, e);
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

        #endregion

        #region Dependency Object (WPF/Silverlight)

        class DependencyObjectEventDispatcher : IEventDispatcher
        {
            private readonly Dispatcher _dispacher;

            public DependencyObjectEventDispatcher()
            {
                // PH: could use System.Windows.Deployment.Current.Dispatcher instead
                // but that would require reference to System.Windows.dll...
                _dispacher = Dispatcher.CurrentDispatcher;
                if (_dispacher == null)
                    throw new InvalidOperationException("Could not create dispatcher object associated with current thread");
            }

            public DependencyObjectEventDispatcher(DependencyObject control)
            {
                if (control == null)
                    throw new ArgumentNullException("control");
                _dispacher = control.Dispatcher;
            }


            #region IEventDispatcher Implementation

            public void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
            {
                if (eventHandler != null)
                {
                    if (_dispacher.CheckAccess())
                    {
                        eventHandler(sender, e);
                    }
                    else
                    {
                        _dispacher.BeginInvoke(eventHandler, sender, e);
                    }
                }
            }

            public void Invoke<T>(Action<T> action, T e)
            {
                if (action != null)
                {
                    if (_dispacher.CheckAccess())
                    {
                        action(e);
                    }
                    else
                    {
                        _dispacher.BeginInvoke(action, e);
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
