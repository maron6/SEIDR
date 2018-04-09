using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using SEIDR.Doc;
using System.Collections.Generic;

namespace SEIDR.Test
{
    [TestClass]
    public class DocReaderTest
    {
        [TestMethod]
        public void ListInsertTest()
        {
            List<string> l = new List<string>();
            l.InsertWithExpansion(5, "Heya", "FILLER");
            l.SetWithExpansion(7, "Test", "SECOND FILLER");
            Assert.AreEqual(l[6], "SECOND FILLER");
            l.InsertWithExpansion(3, "Third");
            Assert.AreEqual("Heya", l[6]);
        }
        [TestMethod]
        public void LineEndingSortTest()
        {
            var l = new List<string>(new[] { "\r", "\n", "\r\n", "\n\n" });
            l.Sort((a, b) =>
            {
                if (a.IsSuperSet(b))
                    return -1;
                if (a.IsSubset(b))
                    return 1;
                return 0;
            });
            l.ForEach(a => System.Diagnostics.Debug.WriteLine(a));
        }
        [TestMethod]
        public void MultiReaderReadTest()
        {            
            FilePrep();
            using (DocReader r1 = new DocReader("r1", FilePath))
            using (DocReader r2 = new DocReader("r2", FilePath))
            {
                DocMetaData dm = new DocMetaData(TEST_FOLDER + "multiWrit.txt", "w")
                    .CopyDetailedColumnCollection(DocRecordColumnCollection.Merge("w", r1.Columns, r2.Columns))
                    .RemoveColumn("LineNumber", "r2")
                    .SetDelimiter('|');
                using (DocWriter dw = new DocWriter(dm))
                {
                    foreach(var r1r in r1)
                    {
                        foreach(var r2r in r2)
                        {
                            var dwr = DocRecord.Merge(dw.Columns, r1r, r2r, checkExist: true); 
                            dwr["r1", "Description"] = "R1  - " + dwr["r1", "Description"];
                            dwr["r2", "Description"] = "R2  - " + dwr["r2", "Description"];
                            dw.AddRecord(dwr);
                        }
                    }
                    foreach (var r2r in r2)                        
                    {
                        foreach (var r1r in r1)
                        {
                            var dwr = DocRecord.Merge(dw.Columns, r2r, r1r, checkExist: true);//removed a column above, so need  checkExist set to true.
                            dwr["r1", "Description"] = "R1  SECOND - " + dwr["r1", "Description"];
                            dwr["r2", "Description"] = "R2  SECOND - " + dwr["r2", "Description"];
                            dw.AddRecord(dwr);
                        }
                    }
                }
            }
        }
        [TestMethod]
        public void TestReadMultiLineEndDelimiter()
        {
            DocMetaData.TESTMODE = true;
            FilePrep();
            DocReader r = new DocReader("lines", FilePath);
            DocMetaData mixed = new DocMetaData(MultiLineEndFilePath, "Multi")
                .SetHasHeader(true)
                .AddMultiLineEndDelimiter("\r", "\n"); //The normal LineEnd Delimiter is also going to be included when actually reading.
            DocReader m = new DocReader(mixed);
            var normal = r.GetPage(0);
            var mixPage = m.GetPage(0);
            Assert.AreEqual(normal.Count, mixPage.Count);
            Assert.AreEqual(normal[0][0], mixPage[0][0]);
            Assert.AreEqual(normal[3]["Description"], mixPage[3]["Description"]);            
            mixed
                .SetSkipLines(2) //Skip two lines, so we'll get LineNumber = 3 (skip LineNumber = 1 and LineNumber = 2). Header is already configured, so that will be skipped as well, since meta data indicates file has a header.
                .SetPageSize(130)
                //.AddDelimitedColumns("LineNumber", "Garbage", "Description") //header was configured already.
                ;
            m.ReConfigure();
            var record = m[0, 0];
            Assert.AreEqual(normal[2][0], record[0]);    //2 - zero based indexes, this gives the third record in the normal file.       
            DocMetaData doc = new DocMetaData(MultiLineEndWriteFilePath, "w").AddDetailedColumnCollection(mixed.Columns).SetHasHeader(true).SetDelimiter('\t');
            using(DocWriter dw = new DocWriter(doc))
            {
                foreach (var rec in m)
                    dw.AddRecord(rec);
            }
            m.Dispose();
            r.Dispose();
        }
        [TestMethod]
        public void ToFixWidthFile()
        {
            DocMetaData mixed = new DocMetaData(MultiLineEndFilePath, "Multi")                
                 .SetHasHeader(true)
                 .AddMultiLineEndDelimiter("\r", "\n"); //The normal LineEnd Delimiter is also going to be included when actually reading.
            mixed.CanWrite = true;
            using (DocReader m = new DocReader(mixed))
            {
                var extra = new DocRecordColumnInfo("", "fw", 1).SetMaxLength(5);
                DocMetaData fixWidth = new DocMetaData(TEST_FOLDER + "FixWidth.txt", "fw")
                    .AddDetailedColumnCollection(mixed.Columns);
                fixWidth
                    .AddDetailedColumn(extra);
                fixWidth.Columns["LineNumber"].SetMaxLength("LineNumber".Length).SetLeftJustify(false);
                fixWidth.Columns["Garbage"].SetMaxLength(300);
                fixWidth.Columns["Description"].SetMaxLength(120);
                fixWidth.TrySetFixedWidthMode(true);
                using (var w = new DocWriter(fixWidth))
                {
                    foreach(var record in m)
                    {
                        w.AddRecord(record);
                    }
                }
            }
            
        }

        void FilePrep()
        {
            DirectoryInfo di = new DirectoryInfo(TEST_FOLDER);
            if (!di.Exists)
                di.Create();
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
            string multiEndcontent = "LineNumber|Garbage|Description\r1|alifmaif|First\r\n2|aliefnaognaognagfnbaovoaignaoehngvoah|Second\n3|lai fmaovfhahv aawoief jslafima|Third\r\n"
                + "4|amfoeaijvnba[pbn [a a[va[n apv mfa,iutva;fja jan b[a [anmvfiesp|Fourth\r\n5|oaifjvnamv  anafjwno; nafsaj|Fifth\r\n"
                + "6|al;vin ;ambivewauwfu sma[nbmaj[nb|Sixth\r7|a;vnslirj|Seventh\n8|Testjao;sv garbage|Eighth\r\n9||Ninth\r"
                + "10|a;svna tbija nswowros snv|Tenth";
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            if (File.Exists(CommaFile))
                File.Delete(CommaFile);
            if (File.Exists(MultiLineEndFilePath))
                File.Delete(MultiLineEndFilePath);
            File.WriteAllLines(FilePath, Lines);
            File.WriteAllLines(CommaFile, CommaLines);
            File.WriteAllText(MultiLineEndFilePath, multiEndcontent);
        }
        const string TEST_FOLDER = @"C:\DocReaderTest\";
        string MultiLineEndFilePath = @"C:\DocReaderTest\MixedLineEnding.txt";
        string MultiLineEndWriteFilePath = @"C:\DocReaderTest\WRITE_MixedLineEnding.txt";
        string FilePath = @"C:\DocReaderTest\Reader.txt";
        string CommaFile = @"C:\DocReaderTest\Reader.csv";
        [TestMethod]
        public void TestRead()
        {
            DocMetaData.TESTMODE = true; //Allow page size below minimum for test purposes
            FilePrep();

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
            var md2 = new DocMetaData(CommaFile, "C");
            md2.SetHasHeader(true);
            r = new DocReader(md2);
            Assert.AreEqual(',', r.Columns.Delimiter);

            md= new DocMetaData(@"C:\DocReaderTest\ReaderFromCSV.txt", "F").AddDelimitedColumns("LineNumber", "Empty", "Description", "Empty", "Test").SetDelimiter('\t').SetHasHeader(true);
            var w = new DocWriter(md);
            w.SetTextQualify(true, "Description", "Test");
            Dictionary<int, DocRecordColumnInfo> map = new Dictionary<int, DocRecordColumnInfo>();
            map[md.Columns["Test"].Position] = md2.Columns["LineNumber"]; //Position = 4 should be the 
            foreach(var record in r)
            {                
                w.AddRecord(record, map);
            }
            w.Dispose();
            md = new DocMetaData(@"C:\DocReaderTest\ReaderFromCSV.csv", "C").AddDelimitedColumns("Description", "LineNumber").SetDelimiter(',').SetHasHeader(true);
            w = new DocWriter(md);
            for(int p = 0; p < r.PageCount; p++)
            {
                w.BulkWrite(r[p]); //ignores meta data...! Assumes matching meta data or already formatted.
            }
            w.Dispose();
            r.Dispose();
            System.Diagnostics.Debug.WriteLine(md.GetHeader());
            System.Diagnostics.Debug.WriteLine(md2.GetHeader());
            string s = File.ReadAllText(md.FilePath).Substring(md.GetHeader().Length);
            string s2 = File.ReadAllText(md2.FilePath).Substring(md2.GetHeader().Length);
            Assert.AreNotEqual(File.ReadAllText(md.FilePath), File.ReadAllText(md2.FilePath)); //Meta data was ignored, so even though the md has a different header, the content is the same.
            Assert.AreEqual(s, s2);
        }
        const int nineNineMil = 99999999; //2 min for Inheritance record test, 3 min for Inheritance RecordTestUpdate
        const int nineNineHT = 9999999; //14 seconds for InheritanceRecordTest, 18 seconds for Inheritance RecordTestUpdate
        [TestMethod]
        public void InheritanceRecordTest()
        {
            FilePrep();
            var md = new DocMetaData(TEST_FOLDER + "Inheritor.txt", "i")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|')
                .SetHasHeader(false);
            using (var w = new DocWriter(md))
            {
                for (int i = 1; i < nineNineHT; i++)
                {
                    var ti = new TestRecordInheritance(w.Columns, i, "Line # " + i); //initialize with constructor originall, will be a bit faster.
                    if (i % 183 == 0)
                        ti.Description += " - Mod 183 = 0";
                    w.AddRecord(ti);
                }
            }
        }
        [TestMethod]
        public void InheritanceRecordTestUpdate()
        {
            FilePrep();
            var md = new DocMetaData(TEST_FOLDER + "Inheritor.txt", "i")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|')
                .SetHasHeader(false);
            using (var w = new DocWriter(md))
            {
                for (int i = 1; i < nineNineHT; i++)
                {
                    var ti = new TestRecordInheritance(w.Columns) //Update columns *after* constructor
                    {
                        LineNumber = i,
                        Description = "Line # " + i
                    };
                    if (i % 183 == 0)
                        ti.Description += " - Mod 183 = 0";
                    w.AddRecord(ti);
                }
            }
        }

    }

    public class TestRecordInheritance: DocRecord
    {
        public TestRecordInheritance(DocRecordColumnCollection columnCollection, int LineNumber, string Description)
            :base(columnCollection, true, new List<string> { LineNumber.ToString(), Description })
        {

        }
        public TestRecordInheritance(DocRecordColumnCollection col)
            : base(col, true)
        {
        }
        public TestRecordInheritance(DocRecordColumnCollection col, IList<string> parsedContent)
            : base(col, true, parsedContent)
        {
        }
        public int LineNumber
        {
            get { return Convert.ToInt32(this[nameof(LineNumber)]); }
            set { this[nameof(LineNumber)] = value.ToString(); }
        }
        public string Description
        {
            get { return this[nameof(Description)]; }
            set { this[nameof(Description)] = value; }
        }
    }
}
