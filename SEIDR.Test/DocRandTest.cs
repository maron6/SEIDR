using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Test
{
    [TestClass]
    public class DocRandTest
    {
        Doc.DocValueRandom r = new Doc.DocValueRandom();
        [TestMethod]
        public void RandomDateTime()
        {

            DateTime? n = r.GetDateTimeNullable(30, 2010, yearThrough: 2020);
            if (n == null)
                return;
            DateTime dt = n.Value;
            Assert.IsTrue(dt.Year >= 2010);
            Assert.IsTrue(dt.Year <= 2020);


        }
        [TestMethod]
        public void percentCheckTest()
        {
            for(int i = 0; i < 10000; i++)
            {
                Assert.IsTrue(r.PercentCheck(100));
                Assert.IsFalse(r.PercentCheck(0));
            }            
        }
        [TestMethod]
        public void PercentDecimalPointTest()
        {
            int counter = 0;
            const int LIMIT_COUNT = 10000000;
            const double PERCENT = 7.05834;
            const double EXPECTED = LIMIT_COUNT * PERCENT * .01;
            const double VARIANCE = LIMIT_COUNT * .00010; //.001%
            for (int i = 0; i < LIMIT_COUNT; i++)
            {
                if (r.PercentCheck(PERCENT))
                    counter++;
            }
            Assert.IsTrue(counter.Between((int)(EXPECTED - VARIANCE), (int)(EXPECTED + VARIANCE)));
        }

        [TestMethod]
        public void RandomString()
        {
            string phonePattern = "([1-9]{3})[1-9]{3}-[1-9]{4}@@"; //Phone pattern + @@
            string s = r.GetString(phonePattern);

            string phonePatternRegexAssert = @"\([1-9]{3}\)[1-9]{3}\-[1-9]{4}@@"; //Escape regex special characters to assert.
            Assert.IsTrue(s.Like(phonePatternRegexAssert, false));

            string ssnPattern = "[1-57-9][0-9]{2}-[0-9]{2}-[1-9][0-9]{3}";
            s = r.GetString(ssnPattern);

            string ssnPatternRegexAssert = @"[1-57-9][0-9]{2}\-[0-9]{2}\-[1-9][0-9]{3}";
            Assert.IsTrue(s.Like(ssnPatternRegexAssert, false));

            string escapePattern = @"@[1-2]\{2}";
            s = r.GetString(escapePattern);
            Assert.IsTrue(s.Like(escapePattern, false));
        }
    }
}
