#if DEBUG

using BlackBerry.Package.Helpers;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ProjectHelperTests
    {
        [TestCase]
        public void MergeProperties()
        {
            Assert.AreEqual("a;b", ProjectHelper.MergePropertyValues("a;b", null, ';', "%(def)"));
            Assert.AreEqual("a;b;c", ProjectHelper.MergePropertyValues("a;b", ";c", ';', "%(def)"));
            Assert.AreEqual("a;b;c;%(def)", ProjectHelper.MergePropertyValues("", "a;b;c", ';', "%(def)"));
            Assert.AreEqual("", ProjectHelper.MergePropertyValues("", "", ';', "%(def)"));
            Assert.AreEqual("a", ProjectHelper.MergePropertyValues("a", "", ';', "%(def)"));
            Assert.AreEqual("a;b;c", ProjectHelper.MergePropertyValues("a", "a;b;b;c", ';', "%(def)"));
        }
    }
}

#endif // DEBUG
