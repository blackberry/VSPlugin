using System;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn.Model;
using NUnit.Framework;

namespace UnitTests
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
            Assert.IsNotNull(tokenData.Author);
            Assert.AreEqual("XyXyXyXyXy", tokenData.Author.Name);
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

        [Test]
        public void ParseDeviceInfoData()
        {
            var textZ10 = @"
Info: Sending request: DEVICE_INFO
Info: Action: List Device Info
[n]@deviceproperties
defaultTheme::white
device_os::BlackBerry 10
devicename::RIM BlackBerry Device
drmhwfp::0xEBA2E830C0226EF926BAC7A34BB0CF3A356F1E67
fingerprint::oaP4lcLnJDCMBqkLLmwq75U_Un-kaMZjqVsx7Mg8TOsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
hardwareid::0x04002607
icon_res::90x90
modelfamily::Touch
modelfullname::BlackBerry Z10
modelname::Z10
modelnumber::STL100-1
radiofingerprint::iPITe8OMommATz7a0tnLf_Dd2nJcskVqzFFNktStkFkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
sar_body::0.73
sar_head::1.09
scmbundle::10.3.0.338
scmbundle0::10.3.0.338
scmbundle1::10.3.0.338
screen_dimensions::2.2x3.6x4.2
screen_dpi::356
screen_res::768x1280
vendorid::?
wlan_standard::wifi
[n]@deviceproperties
IMEI::004401223444443
devicepin::0x2ddf9993
deviceserialnumber::0000000000024000009968
drmhwfp::0xEBA2E830C0226EF926BAC7A34BB0CF3A356F1E67
[n]@devmode
[n]debug_token_author::
[n]debug_token_expiration::
[n]debug_token_installed:b:false
[n]debug_token_timeout::10d
[n]debug_token_valid:b:
[n]debug_token_validation_error::no debug token found
[n]debug_token_validation_error_code:n:1
[n]dev_mode_enabled:b:true
[n]dev_mode_expiration::10d
[n]dev_mode_waiting:b:true
@versions
air_version::3.5.0.142
flash_version::11.1.121.1310
build_id:: 696031
Build Branch: BB10_3_0
production_device:b:false
perimeter::personal
hostname::BLACKBERRY-46A3
";
            string error;
            var deviceData = DeviceInfo.Parse(textZ10, out error);

            Assert.IsNotNull(deviceData);
            Assert.IsNull(error);
            Assert.AreEqual("Z10", deviceData.ModelName);
            Assert.AreEqual("STL100-1", deviceData.ModelNumber);
            Assert.AreEqual(0x2ddf9993, deviceData.PIN);
            Assert.AreEqual("BLACKBERRY-46A3", deviceData.Name);
            Assert.AreEqual("BlackBerry 10", deviceData.SystemName);
            Assert.AreEqual(new Version("10.3.0.338"), deviceData.SystemVersion);
            Assert.AreEqual(new Size(768, 1280), deviceData.ScreenResolution);
            Assert.IsNotNull(deviceData.DebugToken);
            Assert.AreEqual(false, deviceData.DebugToken.IsInstalled);
            Assert.AreEqual("no debug token found", deviceData.DebugToken.ValidationErrorMessage);
            Assert.AreEqual(1, deviceData.DebugToken.ValidationErrorCode);


            var textZ30 = @"
Info: Sending request: DEVICE_INFO
Info: Action: List Device Info
[n]@deviceproperties
defaultTheme::black
device_os::BlackBerry 10
devicename::RIM BlackBerry Device
drmhwfp::0x8FCE3537B1F0B61B21F15AE716661A44F33511E7
fingerprint::XZIue9Qad8PWvlM-0HrauTnNn35c0WjXC3YhjX4mq1UAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
hardwareid::0x8d00240a
icon_res::90x90
modelfamily::Touch
modelfullname::BlackBerry Z30
modelname::Z30
modelnumber::STA100-2
radiofingerprint::cYIbfizAFhMZKNIDRWbJqg2_Eq1VkSk4WVjBAdDcL9oAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
scmbundle::10.2.1.2141
scmbundle0::10.2.1.2141
scmbundle1::10.2.1.2141
screen_dimensions::2.4x4.3x5.0
screen_dpi::295
screen_res::720x1280
vendorid::?
wlan_standard::wifi
[n]@deviceproperties
IMEI::123456789023454
devicepin::0x22233333
deviceserialnumber::0000000000073126680364
drmhwfp::0x8FCE3537B1F0B61B21F15AE716661A44F33511E7
[n]@devmode
[n]debug_token_author::XyXyXyXyXy
[n]debug_token_expiration::Thu May 29 19:25:34 GMT+0200 2014
[n]debug_token_installed:b:true
[n]debug_token_timeout::10d
[n]debug_token_valid:b:true
[n]debug_token_validation_error::
[n]debug_token_validation_error_code:n:0
[n]dev_mode_enabled:b:true
[n]dev_mode_expiration::10d
[n]dev_mode_waiting:b:true
@versions
air_version::3.5.0.230
flash_version::11.1.121.199
build_id:: 668631
Build Branch: BB10_2_1_EMR_1
production_device:b:true
perimeter::personal
hostname::Drone
";

            deviceData = DeviceInfo.Parse(textZ30, out error);

            Assert.IsNotNull(deviceData);
            Assert.IsNull(error);
            Assert.AreEqual("Z30", deviceData.ModelName);
            Assert.AreEqual("STA100-2", deviceData.ModelNumber);
            Assert.AreEqual(0x22233333, deviceData.PIN);
            Assert.AreEqual("Drone", deviceData.Name);
            Assert.AreEqual("BlackBerry 10", deviceData.SystemName);
            Assert.AreEqual(new Version("10.2.1.2141"), deviceData.SystemVersion);
            Assert.AreEqual(new Size(720, 1280), deviceData.ScreenResolution);
            Assert.IsNotNull(deviceData.DebugToken);
            Assert.AreEqual("XyXyXyXyXy", deviceData.DebugToken.Author);
            Assert.AreEqual(new DateTime(2014, 5, 29, 17, 25, 34, DateTimeKind.Utc), deviceData.DebugToken.ExpiryDate);
            Assert.AreEqual(true, deviceData.DebugToken.IsInstalled);
            Assert.AreEqual(true, deviceData.DebugToken.IsValid);
            Assert.IsNull(deviceData.DebugToken.ValidationErrorMessage);
            Assert.AreEqual(0, deviceData.DebugToken.ValidationErrorCode);


            var textPlayBook = @"
Info: Sending request: DEVICE_INFO
Info: Action: List Device Info
[n]@deviceproperties
device_os::BlackBerry PlayBook OS
drmhwfp::0xC09956358BC8DC00FE3090807ACCCA1519BA1B14
fingerprint::FWe7mi_NL6-VEeDTQXsO7HqjUbYBmZvfHKSSLzNPbJgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
hardwareid::0x06001a06
radiofingerprint::none
scmbundle::2.1.0.1917
scmbundle0::2.1.0.1917
scmbundle1::2.1.0.1753
vendorid::0x1f8
[n]@deviceproperties
devicepin::0x12234565
deviceserialnumber::0000000000134339091771
[n]@devmode
[n]debug_token_author::
[n]debug_token_expiration::
[n]debug_token_installed:b:true
[n]debug_token_timeout::10d
[n]debug_token_valid:b:false
[n]debug_token_validation_error::debug token has expired
[n]debug_token_validation_error_code:n:3
[n]dev_mode_enabled:b:true
[n]dev_mode_expiration::10d
[n]dev_mode_waiting:b:true
@versions
air_version::3.1.0.80
flash_version::11.1.121.80
build_id::  683354
production_device:b:true
";

            deviceData = DeviceInfo.Parse(textPlayBook, out error);

            Assert.IsNotNull(deviceData);
            Assert.IsNull(error);
            Assert.AreEqual("PlayBook", deviceData.ModelName);
            Assert.IsNotNull(deviceData.ModelNumber);
            Assert.AreEqual(0x12234565, deviceData.PIN);
            Assert.AreEqual("PlayBook", deviceData.Name);
            Assert.AreEqual("BlackBerry PlayBook OS", deviceData.SystemName);
            Assert.AreEqual(new Version("2.1.0.1917"), deviceData.SystemVersion);
            Assert.AreEqual(new Size(1024, 600), deviceData.ScreenResolution);
            Assert.IsNotNull(deviceData.DebugToken);
            Assert.IsNull(deviceData.DebugToken.Author);
            Assert.AreEqual(true, deviceData.DebugToken.IsInstalled);
            Assert.AreEqual(false, deviceData.DebugToken.IsValid);
            Assert.AreEqual("debug token has expired", deviceData.DebugToken.ValidationErrorMessage);
            Assert.AreEqual(3, deviceData.DebugToken.ValidationErrorCode);

            var textQ10 = @"
Info: Sending request: DEVICE_INFO
Info: Action: List Device Info
[n]@deviceproperties
defaultTheme::black
device_os::BlackBerry 10
devicename::RIM BlackBerry Device
drmhwfp::0x9D763AB214A956720F6686E015013FB3B3FFDC4E
fingerprint::hNCSaLXyVJsdrHHHzuHGEzN-JWvIa557bzScVPlzUxcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
hardwareid::0x8400270a
icon_res::90x90
modelfamily::Bold
modelfullname::BlackBerry Q10
modelname::Q10
modelnumber::SQN100-1
radiofingerprint::CvTGZPyDu5ZYFNioiqb_y6Jo9YgsxdWL-d61BXdqYbEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
scmbundle::10.3.0.338
scmbundle0::10.3.0.338
scmbundle1::10.3.0.338
screen_dimensions::2.2x2.2x3.1
screen_dpi::330
screen_res::720x720
vendorid::?
wlan_standard::wifi
[n]@deviceproperties
IMEI::012451539066521
devicepin::0x1423508a
deviceserialnumber::0000200000000897217232
drmhwfp::0x9D763AB214A956720F6686E015013FB3B3FFDC4E
[n]@devmode
[n]debug_token_author::
[n]debug_token_expiration::
[n]debug_token_installed:b:false
[n]debug_token_timeout::10d
[n]debug_token_valid:b:
[n]debug_token_validation_error::no debug token found
[n]debug_token_validation_error_code:n:1
[n]dev_mode_enabled:b:true
[n]dev_mode_expiration::10d
[n]dev_mode_waiting:b:true
@versions
air_version::3.5.0.142
flash_version::11.1.121.1310
build_id:: 696031
Build Branch: BB10_3_0
production_device:b:false
perimeter::personal
hostname::BLACKBERRY-845A
";

            deviceData = DeviceInfo.Parse(textQ10, out error);

            Assert.IsNotNull(deviceData);
            Assert.IsNull(error);
            Assert.AreEqual("Q10", deviceData.ModelName);
            Assert.AreEqual("SQN100-1", deviceData.ModelNumber);
            Assert.AreEqual(0x1423508a, deviceData.PIN);
            Assert.AreEqual("BLACKBERRY-845A", deviceData.Name);
            Assert.AreEqual("BlackBerry 10", deviceData.SystemName);
            Assert.AreEqual(new Version("10.3.0.338"), deviceData.SystemVersion);
            Assert.AreEqual(new Size(720, 720), deviceData.ScreenResolution);
            Assert.IsNotNull(deviceData.DebugToken);
            Assert.IsNull(deviceData.DebugToken.Author);
            Assert.AreEqual(false, deviceData.DebugToken.IsInstalled);
            Assert.AreEqual(false, deviceData.DebugToken.IsValid);
            Assert.AreEqual("no debug token found", deviceData.DebugToken.ValidationErrorMessage);
            Assert.AreEqual(1, deviceData.DebugToken.ValidationErrorCode);
        }

        [Test]
        public void LoadDisabledDeviceData()
        {
            string error;
            var text = @"
Info: Sending request: DEVICE_INFO
Error: Device is not in the Development Mode. Switch to Development Mode from Security settings on the device.
";
            var deviceData = DeviceInfo.Parse(text, out error);
            Assert.IsNull(deviceData);
            Assert.AreEqual("Device is not in the Development Mode. Switch to Development Mode from Security settings on the device.", error);
        }

        [Test]
        public void FailOnEmptyDeviceData()
        {
            string error;
            var deviceData = DeviceInfo.Parse(null, out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse("", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse(" a b c d e f:: xxx", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse("::", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse(":", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse(":: yyy", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse("a: bbb", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse("key:: ", out error);
            Assert.IsNull(deviceData);
            Assert.IsNull(error);

            deviceData = DeviceInfo.Parse("Error: message", out error);
            Assert.IsNull(deviceData);
            Assert.AreEqual("message", error);
        }

        [Test]
        public void ParseDeviceId()
        {
            Assert.AreEqual(0x22233333, DeviceInfo.ParseDeviceId("0x22233333"));
            Assert.AreEqual(0x1122, DeviceInfo.ParseDeviceId("0X1122"));
            Assert.AreEqual(1122, DeviceInfo.ParseDeviceId("1122"));
        }

        [Test]
        public void ParseCskToken()
        {
            const string cskTokenData = @"#Do not edit manually. Generated automatically by BlackBerry tools.
#Mon Sep 02 08:43:07 EDT 2013
HMAC=111111111111111111111111111\=
Salt=22222222222\=
IV=33333333333\=
Version=3
Token=44444444444444444444444444\=\=";

            var result = CskTokenInfo.Parse(cskTokenData);

            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTime(2013, 9, 2, 6, 43, 07, DateTimeKind.Utc), result.CreatedAt);
            Assert.AreEqual("3", result.Version);
        }

        [Test]
        public void ParseSLog2InfoOutput()
        {
            const string slog2output = @"
Dec 04 03:05:54.082 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383209714                           0  0  -----ONLINE-----
Dec 04 03:05:54.082 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383209714              default*  8900  5  PPS helper initialized for thread
Dec 04 03:05:54.970 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383152370              default   8900  5  PPS helper destructor
Dec 04 03:05:54.970 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383152370              default   8900  5  BPS shutdown
Dec 04 03:05:54.970 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383152370              default   8900  5  Shutting down navigator
Dec 04 03:05:54.970 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383152370              default   8900  5  navigator_thread quitting
";

            var entries = TargetLogEntry.ParseSLog2(slog2output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));

            Assert.IsNotNull(entries);
            Assert.AreEqual(6, entries.Length);

            var lastItem = entries[entries.Length - 1];
            Assert.AreEqual(383152370, lastItem.PID);
            Assert.AreEqual("navigator_thread quitting", lastItem.Message);
            Assert.AreEqual("default", lastItem.BufferSet);
            Assert.AreEqual("com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40", lastItem.AppID);
        }


        [Test]
        public void ParseSLog2InfoOutput2()
        {
            const string slog2output = @"
Dec 26 02:05:05.568         mm_renderer_QC.5599368                           0  0  -----ONLINE----- ()
";

            var entries = TargetLogEntry.ParseSLog2(slog2output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));

            Assert.IsNotNull(entries);
            Assert.AreEqual(1, entries.Length);

            var lastItem = entries[0];
            Assert.AreEqual(5599368, lastItem.PID);
            Assert.AreEqual("-----ONLINE----- ()", lastItem.Message);
            Assert.AreEqual("default", lastItem.BufferSet); // all empty buffers fallback to 'default'
            Assert.AreEqual("mm_renderer_QC", lastItem.AppID);
        }

        [Test]
        public void ParseSLog2InfoOutput3()
        {
            const string slog2output = @"
Dec 26 00:04:22.278                AdSDK.591851775                           0  0  -----ONLINE----- ()
Dec 26 00:04:22.278                AdSDK.591851775                           0  0  -----ONLINE----- ()
Dec 26 00:04:22.278                AdSDK.591851775          NativeAdSDK*     0  5  banner.cpp(349): bbads_banner_create called. ()
Dec 26 00:04:22.280                AdSDK.591851775          NativeAdSDK      0  5  VisibilityListener.cpp(76): Visibility listener thread launched. ()
Dec 26 00:04:22.280                AdSDK.591851775          NativeAdSDK      0  5  BPSEventListener.cpp(69): BPS event listener thread launched. ()
Dec 26 00:04:22.280                AdSDK.591851775          NativeAdSDK      0  5  VisibilityListener.cpp(87): Visibility listener thread posting. ()
";

            var entries = TargetLogEntry.ParseSLog2(slog2output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));

            Assert.IsNotNull(entries);
            Assert.AreEqual(6, entries.Length);

            var lastItem = entries[entries.Length - 1];
            Assert.AreEqual(591851775, lastItem.PID);
            Assert.AreEqual("VisibilityListener.cpp(87): Visibility listener thread posting. ()", lastItem.Message);
            Assert.AreEqual("NativeAdSDK", lastItem.BufferSet);
            Assert.AreEqual("AdSDK", lastItem.AppID);
        }
    }
}
