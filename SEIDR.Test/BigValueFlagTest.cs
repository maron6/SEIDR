using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SEIDR.Test
{
    [TestClass]
    public class BigValueFlagTest
    {
        [TestMethod]
        public void BigValueFlagTestMethod()
        {
            BigValueFlag f = new BigValueFlag();
            f[100000] = true;
            f[12000000] = true;
            f[1] = true;
            f[0] = true;

            f[64] = true;
            f[63] = true;
            f[65] = true;
            Assert.IsTrue(f[0]);
            Assert.IsTrue(f[1]);
            Assert.IsTrue(f[100000]);
            Assert.IsTrue(f[12000000]);
            Assert.IsTrue(f[63]);
            Assert.IsTrue(f[64]);
            Assert.IsTrue(f[65]);
            Assert.IsFalse(f[2]);
            Assert.IsFalse(f[100000000000000000]);
            Assert.AreEqual(7, f.Count);
            Assert.AreEqual(0ul, f.MinFlagged);            
            f[0] = false;
            Assert.IsFalse(f[0]);
            Assert.AreEqual(6, f.Count);
            Assert.AreEqual(12000000ul, f.MaxFlagged);
            Assert.AreEqual(1ul, f.MinFlagged);
            f.Clear();
            Assert.IsFalse(f[1]);
            Assert.AreEqual(0, f.Count);
        }
        [TestMethod]
        public void BigValueFlagEnumeratorTest()
        {
            BigValueFlag f = new BigValueFlag();
            f[1] = true;
            f[1000000] = true;
            f[1200000000000000000] = true;
            f[712000086] = true;
            f[65] = true;
            f[64] = true;
            f[63] = true;
            List<ulong> values = new List<ulong>();
            foreach(var flagged in f)
            {
                values.Add(flagged);
                System.Diagnostics.Debug.WriteLine(flagged);
            }
            Assert.AreEqual(7, values.Count);
            Assert.AreEqual(f.Count, values.Count);
        }
    }
}
