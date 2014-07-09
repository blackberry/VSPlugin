using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Helpers;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class CollectionHelperTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SerializeInvalidKey_with_failure()
        {
            var dict = new Dictionary<string, string>();

            dict["a;"] = "1";

            CollectionHelper.Serialize(dict);
            Assert.Fail("Serialization should fail already");
        }

        [Test]
        public void SerializeEmptyValues_with_success()
        {
            var dict = new Dictionary<string, string>();

            dict["abcdef"] = null;
            dict["123456"] = string.Empty;
            dict["xyz"] = " ";

            var result = CollectionHelper.Serialize(dict);
            Assert.IsNotNull(result);

            var deserialized = CollectionHelper.Deserialize(result);
            Assert.IsNotNull(deserialized);
            Assert.IsNull(deserialized["abcdef"]);
            Assert.AreEqual(string.Empty, deserialized["123456"]);
            Assert.AreEqual(" ", deserialized["xyz"]);
        }

        [Test]
        public void SerializeTypicalValues_with_success()
        {
            var dict = new Dictionary<string, string>();

            dict["libs"] = "abc;def;ghi;jkl";
            dict["json"] = "{\"a\": 1, \"b\": \"=5,123;48\"}";
            dict["path"] = "C:\\Windows\\system32\\cmd.exe";
            dict["key-val"] = "a=1;b=2;c=3";

            var result = CollectionHelper.Serialize(dict);
            Assert.IsNotNull(result);

            var deserialized = CollectionHelper.Deserialize(result);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(dict.Count, deserialized.Count, "Deserialized data has a different size!");

            // compare each value:
            foreach (var pair in dict)
            {
                Assert.AreEqual(pair.Value, deserialized[pair.Key], "Deserialized data is not matching");
            }
        }
    }
}
