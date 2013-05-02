using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VSNDK.Tasks;
using Microsoft.Build.Framework;
using NUnit.Framework;

namespace VSNDK.Tasks.Test
{
    [TestFixture]
    public class MakeAllAndFormatOutputMessagesTestClass
    {
        [TestCase]
        public void MakeAllAndFormatOutputMessagesConstructorTest()
        {
            MakeAllAndFormatOutputMessages target = new MakeAllAndFormatOutputMessages();
            Assert.IsNotNull(target);
        }

    }
}
