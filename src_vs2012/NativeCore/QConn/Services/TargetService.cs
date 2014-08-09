using System;

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
        private readonly string _host;
        private readonly int _port;

        /// <summary>
        /// Init constructor.
        /// </summary>
        protected TargetService(string host, int port, string name, Version version)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");

            _host = host;
            _port = port;
            Name = name;
            Version = version;
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
    }
}
