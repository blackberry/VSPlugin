using System.IO;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void PrintSomeMixedMessages()
        {
            var stream = new MemoryStream();
            var listener = new BlackBerryTraceListener(stream, Encoding.UTF8, true);

            listener.Write("First ", TraceLog.Category);
            listener.Write("part ", TraceLog.Category);
            listener.WriteLine("of the message", TraceLog.Category);
            listener.Flush();

            // read the produced log content:
            string producedLog;
            stream.Position = 0;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                producedLog = reader.ReadToEnd();
            }

            Assert.IsNotNull(producedLog);
        }
    }
}
