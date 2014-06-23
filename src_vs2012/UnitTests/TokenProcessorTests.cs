using System;
using BlackBerry.NativeCore.Components;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class TokenProcessorTests
    {
        [TestCase]
        public void FormGuid()
        {
            const string ExpectedResult = "0x01020304, 0x0506, 0x0708, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10";
            var guid = new Guid(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

            var result = TokenProcessor.GuidToForm(guid);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExpectedResult, result);
        }
    }
}
