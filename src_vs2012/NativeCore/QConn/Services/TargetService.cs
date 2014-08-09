using System;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Base class for operational service exposed by QConn.
    /// It can be:
    ///  * process manager
    ///  * file manager
    ///  * profiler
    ///  * ...
    /// </summary>
    public abstract class TargetService
    {
        private readonly IQConnReader _source;

        /// <summary>
        /// Init constructor.
        /// </summary>
        protected TargetService(string name, Version version, IQConnReader source)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");
            if (source == null)
                throw new ArgumentNullException("source");

            Name = name;
            Version = version;
            _source = source;
        }

        #region Properties

        public string Name
        {
            get;
            private set;
        }

        public Version Version
        {
            get;
            private set;
        }

        #endregion

        protected void Select()
        {
            _source.Select(Name);
        }

        protected IDataReader Send(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            return _source.Send(command);
        }
    }
}
