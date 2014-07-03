using System;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Interface simplifying events invocations.
    /// </summary>
    public interface IEventDispatcher
    {
        bool IsSynchronous
        {
            get;
        }

        void Invoke<T>(EventHandler<T> eventHandler, object sender, T e) where T : EventArgs;
        void Invoke(Action action);
        void Invoke<T>(Action<T> action, T e);
    }
}