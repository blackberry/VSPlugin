using System;
using System.Threading;
using BlackBerry.NativeCore.QConn;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class QConnTests
    {
        [Test]
        public void Connect()
        {
            var qdoor = new QConnDoor();
            qdoor.Authenticated += QConnDoorOnAuthenticated;

            // verify connection is not established:
            Assert.IsFalse(qdoor.IsAuthenticated);
            Assert.IsFalse(qdoor.IsConnected);

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);

            Assert.IsTrue(qdoor.IsAuthenticated);
            Assert.IsTrue(qdoor.IsConnected);

            // keep the connection alive for some time:
            qdoor.KeepAlive();      // send immediatelly
            qdoor.KeepAlive(200);   // first request will be sent after a delay
            Thread.Sleep(1000);     // can sleep here, as 'keep-alive' uses a thread-pool to work...

            // and close:
            qdoor.Close();

            Assert.IsFalse(qdoor.IsAuthenticated);
            Assert.IsFalse(qdoor.IsConnected);
        }

        private void QConnDoorOnAuthenticated(object sender, QConnAuthenticationEventArgs e)
        {
            Console.WriteLine("Is authenticated: {0}", e.IsAuthenticated);
        }
    }
}
