using BlackBerry.NativeCore.Debugger;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class ResponseParsingTests
    {
        [Test]
        public void ParseListOfProcesses()
        {
            var data = new[]
            {
                "&\"info pidlist\\n",
                "~\"usr/sbin/qconn - 76595423/1\\n",
                "~\"usr/sbin/qconn - 76595423/2\\n",
                "~\"usr/bin/pdebug - 76611814/1\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/1\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/2\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/3\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/4\\n"
            };
            var response = new Response(null, data);
            var request = RequestsFactory.ListProcesses();
            var result = request.Complete(response);

            Assert.IsTrue(result);
            Assert.IsNotNull(request.Processes);
            Assert.AreEqual(3, request.Processes.Length);
        }
    }
}
