using System;
using System.Diagnostics;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.Package.Diagnostics
{
    /// <summary>
    /// Trace log listener class that stores all logs from given categories into newly created file inside specified folder.
    /// </summary>
    internal sealed class PersistentTraceListener : TraceListener
    {
        /// <summary>
        /// Prefix of the log file.
        /// </summary>
        public const string Prefix = "vsndk_log_";
            
        private StreamWriter _output;
        private readonly TimeTracker _time;
        private readonly string[] _categories;

        /// <summary>
        /// Init constructor.
        /// It already creates the file.
        /// </summary>
        public PersistentTraceListener(string outputFolder, bool printTime, params string[] categories)
        {
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException("outputFolder");

            FileName = GenerateOutputFileName(outputFolder);
            _categories = categories;
            if (printTime)
                _time = new TimeTracker();

            _output = File.CreateText(FileName);
        }

        #region Properties

        public string FileName
        {
            get;
            private set;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_output != null)
                {
                    _output.Dispose();
                    _output = null;
                }
            }

            base.Dispose(disposing);
        }

        private static string GenerateFileName(int iteration)
        {
            return string.Concat(Prefix, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), iteration > 0 ? "_" : string.Empty, iteration > 0 ? iteration.ToString() : string.Empty, ".txt");
        }

        private static string GenerateOutputFileName(string outputFolder)
        {
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException("outputFolder");

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // try to come up with a file name, that doesn't exist yet in specified folder:
            int iteration = 0;
            string name;
            do
            {
                name = Path.Combine(outputFolder, GenerateFileName(iteration));
                iteration++;
            } while (File.Exists(name));

            return name;
        }

        private bool IsExpectedCategory(string category)
        {
            if (_categories == null || _categories.Length == 0)
                return true;

            for (int i = 0; i < _categories.Length; i++)
            {
                if (string.CompareOrdinal(category, _categories[i]) == 0)
                    return true;
            }

            return false;
        }

        public override void Write(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void WriteLine(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void Write(string message, string category)
        {
            // print only messages of designated category:
            if (_output == null || !IsExpectedCategory(category))
                return;

            var timeString = _time != null ? _time.GetCurrent() : null;
            if (!string.IsNullOrEmpty(timeString))
                _output.Write(timeString);

            _output.Write(message);
            _output.Flush();
        }

        public override void WriteLine(string message, string category)
        {
            // print only messages of designated category:
            if (_output == null || !IsExpectedCategory(category))
                return;

            var timeString = _time != null ? _time.GetCurrentAndReset() : null;
            if (!string.IsNullOrEmpty(timeString))
                _output.Write(timeString);

            _output.WriteLine(message);
            _output.Flush();
        }
    }
}
