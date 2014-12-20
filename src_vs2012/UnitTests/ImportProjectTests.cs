using System.IO;
using BlackBerry.NativeCore.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ImportProjectTests
    {
        [TestCase]
        public void LoadSimpleGlesProjectInfo()
        {
            var projectDirectory = Path.Combine(Defaults.SampleOpenGlesProjectPath, "Ch2_Hello_Triangle");

            var info = ImportProjectInfo.Load(projectDirectory);
            Assert.IsNotNull(info);
            Assert.AreEqual("Ch2_Hello_Triangle", info.Name);
            Assert.IsNotNull(info.Files);
            Assert.IsTrue(info.Files.Length > 0);
        }

        [TestCase]
        public void LoadCascadesProjectInfoWithManifest()
        {
            var projectDirectory = Path.Combine(Defaults.SampleCascadesProjectPath, "Accelerometer");

            var info = ImportProjectInfo.Load(projectDirectory);
            Assert.IsNotNull(info);
            Assert.AreEqual("Accelerometer", info.Name);
            Assert.IsNotNull(info.Files);
            Assert.IsTrue(info.Files.Length > 0);
        }
    }
}
