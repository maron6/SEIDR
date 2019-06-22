using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Doc.FormatHelper;

namespace SEIDR.Test
{
    [TestClass]
    public class DocHelperTest
    {
        [TestMethod]
        public void DelimitedSplitTest()
        {
            string test = "bla bla|'test|'something\r\nheya'\r\ntest'\r\n";
            Doc.DocMetaData md = new Doc.DocMetaData("", "");
            md.SetDelimiter('|').SetLineEndDelimiter("\r\n");
                md.SetFileEncoding(Encoding.Default)
                .SetTextQualifier("'");
            var i = DelimiterHelper.EnumerateLines(test, "|", true, md).ToArray();
            Assert.AreEqual("bla bla", i[0]);
            Assert.AreEqual("'test|'something\r\nheya'\r\ntest'\r\n", i[1]); //Note: splitting on PIPE, not \r\n.
            var l = DelimiterHelper.SplitString(test, '|', out _, true, md, true);
            Assert.AreEqual(i.Length, l.Count);
            for(int idx = 0; idx < i.Length; idx++)
            {
                Assert.AreEqual(i[idx], l[idx]);
            }

            i = DelimiterHelper.EnumerateLines(test, "|", false, md).ToArray();
            Assert.AreEqual("bla bla", i[0]);
            Assert.AreEqual("'test", i[1]);

            i = DelimiterHelper.EnumerateLines(test, "\r\n", true, md).ToArray();
            Assert.AreEqual("heya'\r\ntest'", i[1]);

            i = DelimiterHelper.EnumerateLines(test, "\r\n", false, md).ToArray();
            Assert.AreEqual("heya'", i[1]);

        }
    }
}
