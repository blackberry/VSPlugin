using System;
using NUnit.Framework;
using RIM.VSNDK_Package.Model;

namespace VSNDK.Package.Test
{
    [TestFixture]
    public sealed class ParserTests
    {
        [Test]
        public void ParseDebugTokenData()
        {
            var text = @"
Archive-Manifest-Version: 1.2
Archive-Created-By: BlackBerry BAR Packager v1.5.14

Package-Author: XyXyXyXyXy
Package-Name: debug.token
Package-Version: 0.0.0.6
Package-Type: debug-token
Package-Author-Certificate-Hash: AUnErb136VLE-eXdTgvvlfdM4gg2HIYMviNei3DoLDzeH7Ua0MJ-fayIqwRmazxAm-7Cmg_Zu138qx33shMhlA
Package-Author-Id: graAgmXcffjRiB07222W4tMFuow
Package-Id: 123XyXyQwhm4KpkL-Dy2241SOUw
Package-Version-Id: gYto8ak11kkVJ_Dwsmb83XrS0DY
Package-Issue-Date: 2013-01-17T01:47:50Z

Debug-Token-System-Actions: execute,multi_window,multi_instance,run_air_native,run_native,_sys_use_consumer_push
Debug-Token-Expiry-Date: 2013-02-16T01:47:50Z
Debug-Token-Device-Id: 1311521222,1311052217,1354621311,128366899
";

            var tokenData = DebugTokenInfo.Parse(text);
            Assert.IsNotNull(tokenData);
            Assert.AreEqual("XyXyXyXyXy", tokenData.Author);
            Assert.AreEqual("123XyXyQwhm4KpkL-Dy2241SOUw", tokenData.ID);
            Assert.AreEqual(new DateTime(2013, 01, 17, 1, 47, 50, DateTimeKind.Utc), tokenData.IssueDate);
            Assert.AreEqual(new DateTime(2013, 02, 16, 1, 47, 50, DateTimeKind.Utc), tokenData.ExpiryDate);
        }

        [Test]
        public void FailOnEmptyDebugTokenData()
        {
            var tokenData = DebugTokenInfo.Parse(null);
            Assert.IsNull(tokenData);

            tokenData = DebugTokenInfo.Parse("");
            Assert.IsNull(tokenData);

            tokenData = DebugTokenInfo.Parse(" a b c d e f: xxx");
            Assert.IsNull(tokenData);

            tokenData = DebugTokenInfo.Parse(": yyy");
            Assert.IsNull(tokenData);

            tokenData = DebugTokenInfo.Parse("key: ");
            Assert.IsNull(tokenData);
        }
    }
}
