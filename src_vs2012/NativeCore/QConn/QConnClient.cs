using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Client to the QConn service running on target.
    /// It requires QConnDoor to authorize secure connection, before this one can access it.
    /// This class allows to perform some basic functionalities on target like:
    ///   * read info
    ///   * list/start/kill processes
    ///   * transfer files.
    /// </summary>
    public sealed class QConnClient
    {
        /// <summary>
        /// Default port the QConn service is running on the target.
        /// </summary>
        public int DefaultPort = 8000;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public QConnClient()
        {
            Services = new TargetService[0];
        }

        #region Properties

        public Endianess Endian
        {
            get;
            private set;
        }

        public TargetSystemType System
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Locale
        {
            get;
            private set;
        }

        public Version Version
        {
            get;
            private set;
        }

        public TargetService[] Services
        {
            get;
            private set;
        }

        public TargetServiceSysInfo SysInfoService
        {
            get;
            private set;
        }

        public TargetServiceControl ControlService
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Loads info about the target.
        /// </summary>
        public void Load(string host)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            Load(host, DefaultPort);
        }

        /// <summary>
        /// Loads info about the target.
        /// </summary>
        public void Load(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            using (var connection = new QConnConnection(host, port).Connect())
            {
                // get info about the target:
                var response = connection.Send("info");
                if (string.IsNullOrEmpty(response))
                    throw new QConnException("Unable to load QConn info");

                ParseInfoProperties(GetProperties(response));
                if (Endian == Endianess.Unknown)
                    throw new QConnException("Unable to determin endianess of the target");
                if (Version == null)
                    throw new QConnException("Unable to determin QConn version");

                // list all available services:
                response = connection.Send("service ?");
                if (string.IsNullOrEmpty(response))
                    throw new QConnException("Unable to load QConn services");

                string[] serviceNames = ParseServices(response.StartsWith("services: ") ? response.Substring(10) : response);
                Services = CreateDefaultServices(connection, serviceNames, host, port);
            }
        }

        private TargetService[] CreateDefaultServices(QConnConnection connection, string[] serviceNames, string host, int port)
        {
            List<TargetService> services = new List<TargetService>();

            if (HasService(serviceNames, "file"))
            {
                var fileService = new TargetServiceFile(GetServiceVersion(connection, "file"), new QConnConnection(host, port, "file", Endian));
                services.Add(fileService);
            }

            ControlService = null;
            if (HasService(serviceNames, "cntl"))
            {
                ControlService = new TargetServiceControl(GetServiceVersion(connection, "cntl"), new QConnConnection(host, port, "cntl", Endian));
                services.Add(ControlService);
            }

            SysInfoService = null;
            if (HasService(serviceNames, "sinfo"))
            {
                SysInfoService = new TargetServiceSysInfo(GetServiceVersion(connection, "sinfo"), new QConnConnection(host, port, "sinfo", Endian));
                services.Add(SysInfoService);
            }

            return services.ToArray();
        }

        private Version GetServiceVersion(QConnConnection connection, string name)
        {
            var version = connection.Send("version " + name);
            if (string.IsNullOrEmpty(version))
                throw new QConnException("Unable to load version of \"" + name + "\" service");

            QTraceLog.WriteLine("Loaded '{0}' version: {1}", name, version);

            uint numberVersion;
            if (uint.TryParse(version, out numberVersion))
            {
                return new Version((int)((numberVersion & 0xFF00) >> 8), (int)(numberVersion & 0xFF));
            }

            throw new QConnException("Unable to parse version of \"" + name + "\" service");
        }

        /// <summary>
        /// Checks, if given string belongs to collection.
        /// </summary>
        private static bool HasService(IEnumerable<string> services, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (services == null)
                return false;

            foreach (var serviceName in services)
            {
                if (string.Compare(serviceName, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        private static string[] ParseServices(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[0];

            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void ParseInfoProperties(Dictionary<string, string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            if (properties.ContainsKey("ENDIAN"))
            {
                Endian = ParseEndian(properties["ENDIAN"]);
            }
            if (properties.ContainsKey("OS"))
            {
                System = ParseSystem(properties["OS"]);
            }
            if (properties.ContainsKey("QCONN_VERSION"))
            {
                Version = ParseVersion(properties["QCONN_VERSION"]);
            }
            if (properties.ContainsKey("LOCALE"))
            {
                Locale = properties["LOCALE"];
            }
            if (properties.ContainsKey("HOSTNAME"))
            {
                Name = properties["HOSTNAME"];
            }
        }

        private static Version ParseVersion(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // find second '.'
            int i1 = text.IndexOf('.');
            if (i1 < 0)
                return new Version(text + ".0");    // to force: x.0

            int i2 = text.IndexOf('.', i1 + 1);
            if (i2 < 0)
                return new Version(text);

            return new Version(text.Substring(0, i2));
        }

        private static TargetSystemType ParseSystem(string text)
        {
            if (string.IsNullOrEmpty(text))
                return TargetSystemType.Unknown;

            if (string.Compare("nto", text, StringComparison.OrdinalIgnoreCase) == 0)
                return TargetSystemType.Neutrino;

            return TargetSystemType.Unknown;
        }

        private static Endianess ParseEndian(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Endianess.Unknown;

            if (string.Compare("le", text, StringComparison.OrdinalIgnoreCase) == 0)
                return Endianess.LittleEndian;
            if (string.Compare("be", text, StringComparison.OrdinalIgnoreCase) == 0)
                return Endianess.BigEndian;

            return Endianess.Unknown;
        }

        /// <summary>
        /// Converts space separated list of key=value items to dictionary.
        /// </summary>
        private static Dictionary<string, string> GetProperties(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            var result = new Dictionary<string, string>();
            var items = text.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in items)
            {
                int index = line.IndexOf('=');
                if (index >= 0)
                {
                    var key = line.Substring(0, index);
                    var value = line.Substring(index + 1);
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
