using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using SEIDR.Doc;

namespace SEIDR.Test
{
    [TestClass]
    public class DocReaderTest
    {
        [TestMethod]
        public void TestRead()
        {
            DocMetaData.TESTMODE = true; //Allow page size below minimum for test purposes
            string FilePath = @"C:\Users\Owner\Documents\DocReader\Reader.txt";
            string CommaFile = @"C:\Users\Owner\Documents\DocReader\Reader.csv";
            string[] Lines = new[]
            {
                "LineNumber|Description",
                "1|First",
                "2|Second",
                "3|Third",
                "4|Fourth",
                "5|Fifth",
                "6|Sixth",
                "7|Seventh",
                "8|Eighth",
                "9|Ninth",
                "10|Tenth"
            };
            string[] CommaLines = new[]
            {
                "LineNumber,Description",
                "1,First",
                "2,Second",
                "3,Third",
                "4,Fourth",
                "5,Fifth",
                "6,Sixth",
                "7,Seventh",
                "8,Eighth",
                "9,Ninth",
                "10,Tenth"
            };
            File.WriteAllLines(FilePath, Lines);
            File.WriteAllLines(CommaFile, CommaLines);
            DocReader r = new DocReader("F", FilePath);
            Assert.AreEqual(2, r.Columns.Count);
            Assert.AreEqual('|', r.Columns.Delimiter);
            var l = r.GetPage(0);
            Assert.AreEqual(10, l.Count);
            var rec = l[0];            
            Assert.AreEqual("1", l[0]["LineNumber"]);
            Assert.AreEqual("Second", l[1]["Description"]);            
            DocMetaData md = new DocMetaData(FilePath, "F");
            md.AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|')
                .SetSkipLines(2)
                .SetHasHeader(true);
            r.Dispose();
            r = new DocReader(md);
            Assert.AreEqual(2, r.Columns.Count);
            l = r.GetPage(0);
            Assert.AreEqual(8, l.Count);
            r.Dispose();
            md.SetPageSize(30);
            //md.PageSize = 30; 
            /*Page 0 should be 
LineNumber|Description
1|Firs

            After adjusting because of header, page 0 should grab
1|First
2|Second
3|Third
4|
            and exclude the 4| due to being the last segment after splitting on line end with more pages to load after
            */
            r.ReConfigure();
            l = r.GetPage(0); //ToDo: make sure we have a newline delimiter.... ended after |D
            Assert.AreEqual(3, l.Count);
            Assert.AreEqual("3", l[0]["LineNumber"]);
            r.Dispose();
            md = new DocMetaData(CommaFile, "C");
            md.SetHasHeader(true);
            r = new DocReader(md);
            Assert.AreEqual(',', r.Columns.Delimiter);            
            r.Dispose();
        }
    }
}
