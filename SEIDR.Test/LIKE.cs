using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR;

namespace SEIDR.Test
{
    [TestClass]
    public class LIKE
    {
        [TestMethod]
        public void TestMethod1()
        {
            string test = "abcdefghijklmnopqr";
            Assert.IsTrue(test.Like("abc_e%"));
            Assert.IsTrue(test.Like("%abC_e%"));
            Assert.IsTrue(test.Like("%fgHi%"));
            Assert.IsFalse(test.Like("fghi%"));
        }
    }
}
