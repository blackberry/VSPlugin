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
            qdoor.Open("192.168.9.150", QConnDoor.DefaultPort);
        }
    }
}
