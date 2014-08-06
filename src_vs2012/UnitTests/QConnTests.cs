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
            qdoor.Connect("192.168.9.152", QConnDoor.DefaultPort);
        }
    }
}
