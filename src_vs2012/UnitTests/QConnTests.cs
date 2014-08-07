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
            qdoor.Connect("192.168.9.150", QConnDoor.DefaultPort, "qwer", @"C:\Users\Pawel\AppData\Local\Research In Motion\bbt_id_rsa.pub");
            qdoor.KeepAlive();
            qdoor.Close();
        }
    }
}
