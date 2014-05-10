using System;
using System.IO;

namespace RIM.VSNDK_Package.Diagnostics
{
    /// <summary>
    /// Helper class for managing the 'time' part of the trace-log messages.
    /// </summary>
    internal sealed class TimeTracker
    {
        private readonly char[] _time;
        private int _messagePart;

        public TimeTracker()
        {
            _time = CreateTimeArray();
        }

        private static char[] CreateTimeArray()
        {
            return new[] { '0', '0', ':', '0', '0', ':', '0', '0', '.', '0', '0', '0', ':', ' ' };
        }

        /// <summary>
        /// Update the already allocated array with current time.
        /// </summary>
        private void UpdateTimeArray()
        {
            DateTime time = DateTime.Now;

            _time[0] = time.Hour < 10 ? '0' : (char)('0' + (time.Hour / 10));
            _time[1] = (char)('0' + (time.Hour % 10));

            _time[3] = time.Minute < 10 ? '0' : (char)('0' + (time.Minute / 10));
            _time[4] = (char)('0' + (time.Minute % 10));

            _time[6] = time.Second < 10 ? '0' : (char)('0' + (time.Second / 10));
            _time[7] = (char)('0' + (time.Second % 10));

            int millisecond = time.Millisecond;
            _time[11] = (char)('0' + (millisecond % 10));
            millisecond /= 10;
            _time[10] = (char)('0' + (millisecond % 10));
            millisecond /= 10;
            _time[9] = (char)('0' + (millisecond % 10));
        }

        public void Write(TextWriter output)
        {
            if (_messagePart == 0)
            {
                UpdateTimeArray();
                output.Write(_time);
            }

            _messagePart++;
        }

        public void WriteAndReset(TextWriter output)
        {
            if (_messagePart == 0)
            {
                UpdateTimeArray();
                output.Write(_time);
            }

            _messagePart = 0;
        }

        public string GetCurrent()
        {
            string result = null;

            if (_messagePart == 0)
            {
                UpdateTimeArray();
                result = new string(_time);
            }

            _messagePart++;
            return result;
        }

        public string GetCurrentAndReset()
        {
            string result = null;

            if (_messagePart == 0)
            {
                UpdateTimeArray();
                result = new string(_time);
            }

            _messagePart = 0;
            return result;
        }
    }
}
