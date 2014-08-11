using System;
using System.Collections.Generic;
using System.Text;
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
    public sealed class QConnClient : IDisposable, IQConnReader
    {
        /// <summary>
        /// Default port the QConn service is running on the target.
        /// </summary>
        public int DefaultPort = 8000;

        private QDataSource _source;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public QConnClient()
        {
            _source = new QDataSource();
            Services = new TargetService[0];
        }

        ~QConnClient()
        {
            Dispose(false);
        }

        #region Properties

        public bool IsConnected
        {
            get { return _source != null && _source.IsConnected; }
        }

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
        /// Opens the connection to target.
        /// </summary>
        public void Connect(string host)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            Connect(host, DefaultPort);
        }

        /// <summary>
        /// Opens the connection to target.
        /// </summary>
        public void Connect(string host, int port)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            var status = _source.Connect(host, port);
            if (status != HResult.OK)
                throw new QConnException("Unable to connect to QConn service (" + status + ")");

            // receive presentation info:
            int length;
            var response = Receive(out length);
            if (string.IsNullOrEmpty(response) || string.CompareOrdinal(response, "QCONN") != 0)
                throw new QConnException("Invalid service running on target");

            // PH: HACK: strange, but looks like
            // the initial invitation *sometimes* is split into two responses,
            // where the second one is only 3-byte control instruction, that translates
            // to an empty string...
            if (length < 10)
            {
                response = Receive(out length);
            }

            LoadInfo(host, port);
        }

        /// <summary>
        /// Closes connection to target.
        /// </summary>
        public void Close()
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");

            Clear();
            var status = _source.Close();
            if (status != HResult.OK)
                throw new QConnException("Unable to close connection to QConn service (" + status + ")");
        }

        #region Send-Receive

        private string Send(string command)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            // send request:
            var status = _source.Send(Encoding.UTF8.GetBytes(command + "\r\n"));
            if (status != HResult.OK)
            {
                throw new QConnException("Unable to send command \"" + command + "\"");
            }

            // receive response:
            int length;
            var response = Receive(out length);

            // verify response:
            if (string.IsNullOrEmpty(response))
            {
                throw new QConnException("Unknown response for command \"" + command + "\"");
            }

            if (response.StartsWith("error ", StringComparison.OrdinalIgnoreCase))
            {
                throw new QConnException("Command \"" + command + "\" finished with error: " + response.Substring(6));
            }

            return response;
        }

        private string Receive(out int length)
        {
            // read all data:
            byte[] data;

            do
            {
                var status = _source.Receive(int.MaxValue, out data);
                length = data.Length;

                QTraceLog.WriteLine("Received response: {0} ({1})", data.Length, status);

                if (status != HResult.OK)
                    return null;
            } while (length == 2 && data[0] == '\r' && data[1] == '\n');

            return ResponseToString(data);
        }

        /// <summary>
        /// Converts raw response to string object, removing all controls chars.
        /// </summary>
        private static string ResponseToString(byte[] data)
        {
            const byte IAC = 255;
            const byte DONT = 254;
            const byte DO = 253;
            const byte WONT = 252;
            const byte WILL = 251;
            const byte SB = 250;
            const byte SE = 240;

            var buff = new StringBuilder();

            int state = 0;
            for (int i = 0; i < data.Length && (state != 0 || data[i] != 10); i++)
            {
                byte c = data[i];

                switch (state)
                {
                    case 0:
                        if (c == IAC)
                        {
                            state = 1;
                        }
                        else if (!char.IsControl((char)c))
                        {
                            buff.Append((char)c);
                        }
                        break;
                    case 1:
                        switch (c)
                        {
                            case IAC:
                                buff.Append((char)c);
                                state = 0;
                                break;
                            case WILL:
                            case WONT:
                            case DO:
                            case DONT:
                                state = 2;
                                break;
                            case SB:
                                state = 3;
                                break;
                            default:
                                state = 0;
                                break;
                        }
                        break;
                    case 2:
                        state = 0;
                        break;
                    case 3:
                        if (c == IAC)
                        {
                            state = 4;
                        }
                        break;
                    case 4:
                        if (c == SE)
                        {
                            state = 0;
                        }
                        else
                        {
                            state = 3;
                        }
                        break;
                }
            }

            return buff.ToString();
        }

        #endregion

        private void LoadInfo(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            // get info about the target:
            var response = Send("info");
            if (string.IsNullOrEmpty(response))
                throw new QConnException("Unable to load QConn info");

            ParseInfoProperties(GetProperties(response));
            if (Endian == Endianess.Unknown)
                throw new QConnException("Unable to determin endianess of the target");
            if (Version == null)
                throw new QConnException("Unable to determin QConn version");

            // list all available services:
            response = Send("service ?");
            if (string.IsNullOrEmpty(response))
                throw new QConnException("Unable to load QConn services");

            string[] serviceNames = ParseServices(response.StartsWith("services: ") ? response.Substring(10) : response);
            Services = CreateDefaultServices(serviceNames);
        }

        private TargetService[] CreateDefaultServices(string[] serviceNames)
        {
            List<TargetService> services = new List<TargetService>();

            if (HasService(serviceNames, "file"))
            {
                var fileService = new TargetServiceFile(GetServiceVersion("file"), this);
                services.Add(fileService);
            }

            ControlService = null;
            if (HasService(serviceNames, "cntl"))
            {
                ControlService = new TargetServiceControl(GetServiceVersion("cntl"), this);
                services.Add(ControlService);
            }

            SysInfoService = null;
            if (HasService(serviceNames, "sinfo"))
            {
                SysInfoService = new TargetServiceSysInfo(GetServiceVersion("sinfo"), this);
                services.Add(SysInfoService);
            }

            return services.ToArray();
        }

        private Version GetServiceVersion(string name)
        {
            var version = Send("version " + name);
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

        #region IQConnReader Implementation

        void IQConnReader.Select(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentNullException("serviceName");

            Send("service " + serviceName);
        }

        IDataReader IQConnReader.Send(string command)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (Endian == Endianess.Unknown)
                throw new InvalidOperationException("Connection to QConn service was invalid, unable to determin target endianess");

            var status = _source.Send(Encoding.UTF8.GetBytes(command + "\r\n"));
            if (status != HResult.OK)
            {
                throw new QConnException("Unable to send command \"" + command + "\"");
            }

            // get response as proper stream-reader object with given endianess support:
            if (Endian == Endianess.LittleEndian)
                return new DataReaderLittleEndian(_source);
            return new DataReaderBigEndian(_source);
        }

        string IQConnReader.Command(string command)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnClient");
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            return Send(command);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
                if (_source != null)
                {
                    _source.Dispose();
                    _source = null;
                }
            }
        }

        private void Clear()
        {
            Endian = Endianess.Unknown;
            System = TargetSystemType.Unknown;
            Name = null;
            Locale = null;
            Version = null;

            SysInfoService = null;
            ControlService = null;
        }

        #endregion
    }
}
