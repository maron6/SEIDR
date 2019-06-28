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
        }
    }
}
