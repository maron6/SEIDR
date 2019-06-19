using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using SEIDR.Doc;
using System.Collections.Generic;
using Microsoft.CSharp;

namespace SEIDR.Test
{
    [TestClass]
    public class DocReaderTest: TestBase
    {
        [TestMethod]
        public void BSONtest()
        {
            const int RECORDCOUNT = 30;
            DocMetaData write = new DocMetaData(TEST_FOLDER, "Test.bson", "Test.bson");
            write
                .SetHasHeader(false)
                .SetFormat(DocRecordFormat.SBSON)
                //.SetLineEndDelimiter(Environment.NewLine + Environment.NewLine)
                .SetFileAccess(FileAccess.ReadWrite);
            write.AddColumn("LineNumber", DataType: DocRecordColumnType.Int);
            write.AddColumn("TimeStamp", DataType: DocRecordColumnType.DateTime);
            write.AddColumn("NOTE"); //ToDo: 
            using (DocWriter w = new DocWriter(write))
            {
                for (int i = 0; i < RECORDCOUNT; i++)
                {
                    DocRecord r = write.GetBasicRecord();
                    r.SetValue("LineNumber", i);
                    r.SetValue("TimeStamp", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    if (i % 2 == 0)
                        r.SetValue("NOTE", "Bla bla Note # " + i);
                    w.AddRecord(r);
                }
            }
            using (var read = new DocReader(write))
            {
                Assert.AreEqual(RECORDCOUNT, read.RecordCount);
                read.ForEachIndex((r, idx) =>
                {
                    Assert.AreEqual(idx, r.Evaluate<int>("LineNumber"));
                    DateTime dt = default;
                    Assert.IsTrue(r.TryEvaluate("TimeStamp", out dt));                    
                    Assert.AreNotEqual(default, dt);
                    if (idx % 2 == 0)
                        Assert.IsNotNull(r["NOTE"]);
                    else
                        Assert.IsNull(r["NOTE"]);
                });
            }
            
        }
        [TestMethod]
        public void GetterTest()
        {
            FilePrep();

            
            using (var reader = new DocReader("F", FilePath))
            {
                reader.MetaData.CanWrite = true;
                ((DocMetaData)reader.MetaData).AddDelimitedColumns("Test", "test 2", "Test3");
                ((DocMetaData)reader.MetaData).Columns["LineNumber"].DataType = DocRecordColumnType.Int;
                ((DocMetaData)reader.MetaData).Columns["Test3"].DataType = DocRecordColumnType.Money;
                reader.MetaData.AllowMissingColumns = true;

                reader.GuessColumnDataTypes();
                int i = 0;
                foreach (var record in reader)
                {

                    record
                        .SetValue("Test", "Hi")
                        .SetValue("Test3", (i % 2 == 0 ? "+" : "-") + "$" + i + ",000.54")
                        .SetValue("test 2", i++);
                    //writer.AddRecord(record, dm);
                    int x = record.Evaluate<int>("LineNumber");

                    int? x2 = record.Evaluate<int?>("LineNumber");
                    Assert.AreEqual(i, x);
                    Assert.AreEqual(i, x2.Value);
                    decimal d = record.Evaluate<decimal>("Test3");
                    Assert.IsTrue(record.TryEvaluate("LineNumber", out x2));
                    Assert.AreEqual(i, x2.Value);
                    Assert.AreNotEqual(0, d);
                }
                    /*
                     * Test -> Extra, test 2 -> Random
                    LineNumber|Description|ZeroIndex|test 2|Random Output!|Extra output
                    1|First|Hi|0|0|Hi
                    2|Second|Hi|1|1|Hi
                    3|Third|Hi|2|2|Hi
                    4|Fourth|Hi|3|3|Hi
                    5|Fifth|Hi|4|4|Hi
                    6|Sixth|Hi|5|5|Hi
                    7|Seventh|Hi|6|6|Hi
                    8|Eighth|Hi|7|7|Hi
                    9|Ninth|Hi|8|8|Hi
                    10|Tenth|Hi|9|9|Hi
                     
                    */
                

            }
        }
        [TestMethod]
        public void DynamicRecordTest()
        {
            FilePrep();

            var output = new DocMetaData(TEST_FOLDER, "WriterMapOutput.txt", "w");
            using (var reader = new DocReader("F", FilePath))
            {
                reader.MetaData.CanWrite = true;
                ((DocMetaData)reader.MetaData).AddDelimitedColumns("Test", "test 2");
                ((DocMetaData)reader.MetaData).Columns["LineNumber"].DataType = DocRecordColumnType.Int;                
                reader.MetaData.AllowMissingColumns = true;
                output.SetDelimiter('|');
                output
                    .AddDetailedColumnCollection(reader.Columns)
                    .AddDelimitedColumns("Random Output!", "Extra output")
                    .Columns.RenameColumn(output.Columns["Test"], "ZeroIndex")                    
                    ;
                using (var writer = new DocWriter(output))
                {
                    DocWriterMap dm = new DocWriterMap(writer, reader);
                    dm.AddMapping(reader.Columns["test 2"], writer.Columns["Random Output!"])
                        .AddMapping("Test", "Extra output")
                        .AddMapping("Test", "ZeroIndex");
                    int i = 0;
                    foreach (dynamic record in reader)
                    {

                        record
                            .SetValue("Test", "Hi")
                            .SetValue("test 2", i++);
                        writer.AddDocRecord(record, dm);
                        int x = record.LineNumber;
                        Assert.AreEqual(i, x);
                    }
                    /*
                     * Test -> Extra, test 2 -> Random
                    LineNumber|Description|ZeroIndex|test 2|Random Output!|Extra output
                    1|First|Hi|0|0|Hi
                    2|Second|Hi|1|1|Hi
                    3|Third|Hi|2|2|Hi
                    4|Fourth|Hi|3|3|Hi
                    5|Fifth|Hi|4|4|Hi
                    6|Sixth|Hi|5|5|Hi
                    7|Seventh|Hi|6|6|Hi
                    8|Eighth|Hi|7|7|Hi
                    9|Ninth|Hi|8|8|Hi
                    10|Tenth|Hi|9|9|Hi
                     
                    */
                }

            }
        }
        [TestMethod]
        public void DocWriterMapTest()
        {
            FilePrep();

            var output = new DocMetaData(TEST_FOLDER, "WriterMapOutput.txt", "w");
            using (var reader = new DocReader("F", FilePath))
            {
                ((DocMetaData)reader.MetaData).AddDelimitedColumns("Test", "test 2");
                reader.MetaData.CanWrite = true;
                reader.MetaData.AllowMissingColumns = true;
                reader.GuessColumnDataTypes();
                output
                    .AddDetailedColumnCollection(reader.Columns)
                    .AddDelimitedColumns("Random Output!", "Extra output")
                    .Columns.RenameColumn(output.Columns["Test"], "ZeroIndex");
                output.SetDelimiter('|');
                using (var writer = new DocWriter(output))
                {
                    DocWriterMap dm = new DocWriterMap(writer, reader);
                    dm.AddMapping(reader.Columns["test 2"], writer.Columns["Random Output!"])
                        .AddMapping("Test", "Extra output")
                        .AddMapping("Test", "ZeroIndex");
                    int i = 0;
                    foreach (var record in reader)
                    {
                        record
                            .SetValue("Test", "Hi")
                            .SetValue("test 2", i++);

                        writer.AddDocRecord(record, dm);
                    }
                    /*
                     * Test -> Extra, test 2 -> Random
                    LineNumber|Description|ZeroIndex|test 2|Random Output!|Extra output
                    1|First|Hi|0|0|Hi
                    2|Second|Hi|1|1|Hi
                    3|Third|Hi|2|2|Hi
                    4|Fourth|Hi|3|3|Hi
                    5|Fifth|Hi|4|4|Hi
                    6|Sixth|Hi|5|5|Hi
                    7|Seventh|Hi|6|6|Hi
                    8|Eighth|Hi|7|7|Hi
                    9|Ninth|Hi|8|8|Hi
                    10|Tenth|Hi|9|9|Hi
                     
                    */
                }

            }
            string s = File.ReadAllText(output.FilePath);
            Assert.AreEqual("LineNumber|Description|ZeroIndex|test 2|Random Output!|Extra output\r\n1|First|Hi|0|0|Hi\r\n2|Second|Hi|1|1|Hi\r\n3|Third|Hi|2|2|Hi\r\n4|Fourth|Hi|3|3|Hi\r\n5|Fifth|Hi|4|4|Hi\r\n6|Sixth|Hi|5|5|Hi\r\n7|Seventh|Hi|6|6|Hi\r\n8|Eighth|Hi|7|7|Hi\r\n9|Ninth|Hi|8|8|Hi\r\n10|Tenth|Hi|9|9|Hi\r\n", s);

        }

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
                DocMetaData dm = (DocMetaData) new DocMetaData(TEST_FOLDER + "multiWrit.txt", "w")
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
                            dw.AddDocRecord(dwr);
                        }
                    }
                    foreach (var r2r in r2)
                    {
                        foreach (var r1r in r1)
                        {
                            var dwr = DocRecord.Merge(dw.Columns, r2r, r1r, checkExist: true);//removed a column above, so need  checkExist set to true.
                            dwr["r1", "Description"] = "R1  SECOND - " + dwr["r1", "Description"];
                            dwr["r2", "Description"] = "R2  SECOND - " + dwr["r2", "Description"];
                            dw.AddDocRecord(dwr);
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
            var mixed = (DocMetaData)new DocMetaData(MultiLineEndFilePath, "Multi")
                .SetHasHeader(true)
                .AddMultiLineEndDelimiter("\r", "\n"); //The normal LineEnd Delimiter is also going to be included when actually reading.
            mixed.CanWrite = true;
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
            DocMetaData doc = (DocMetaData)new DocMetaData(MultiLineEndWriteFilePath, "w")
                .AddDetailedColumnCollection(mixed.Columns)
                .SetHasHeader(true)
                .SetDelimiter('\t');
            using(DocWriter dw = new DocWriter(doc))
            {
                foreach (var rec in m)
                {
                    rec["Description"] += "\t Test";
                    dw.AddDocRecord(rec);
            	}
            }
            m.Dispose();
            r.Dispose();
        }
        [TestMethod]
        public void ToFixWidthFile()
        {
            FilePrep();
            DocMetaData mixed = (DocMetaData)new DocMetaData(MultiLineEndFilePath, "Multi")
                 .SetHasHeader(true)
                 .AddMultiLineEndDelimiter("\r", "\n"); //The normal LineEnd Delimiter is also going to be included when actually reading.
            mixed.CanWrite = true;
            using (DocReader m = new DocReader(mixed))
            {
                var extra = new DocRecordColumnInfo("", "fw", 1).SetMaxLength(5); //extra, hard coded boundary between LineNumber and garbage for readability with justification switch
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
                        w.AddDocRecord(record);
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
            string[] bigLines = new[]
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
                "10|Tenth",
                "11|First",
                "12|Second",
                "13|Third",
                "14|Fourth",
                "15|Fifth",
                "16|Sixth",
                "17|Seventh",
                "18|Eighth",
                "19|Ninth",
                "20|Tenth",
                "21|First",
                "22|Second",
                "23|Third",
                "24|Fourth",
                "25|Fifth",
                "26|Sixth",
                "27|Seventh",
                "28|Eighth",
                "29|Ninth",
                "30|Tenth",
                "31|First",
                "32|Second",
                "33|Third",
                "34|Fourth",
                "35|Fifth",
                "36|Sixth",
                "37|Seventh",
                "38|Eighth",
                "39|Ninth",
                "40|Tenth",
                "41|First",
                "42|Second",
                "43|Third",
                "44|Fourth",
                "45|Fifth",
                "46|Sixth",
                "47|Seventh",
                "48|Eighth",
                "49|Ninth",
                "50|Tenth",
                "51|First",
                "52|Second",
                "53|Third",
                "54|Fourth",
                "55|Fifth",
                "56|Sixth",
                "57|Seventh",
                "58|Eighth",
                "59|Ninth",
                "60|Tenth",
                "61|First",
                "62|Second",
                "63|Third",
                "64|Fourth",
                "65|Fifth",
                "66|Sixth",
                "67|Seventh",
                "68|Eighth",
                "69|Ninth",
                "70|Tenth",
                "71|First",
                "72|Second",
                "73|Third",
                "74|Fourth",
                "75|Fifth",
                "76|Sixth",
                "77|Seventh",
                "78|Eighth",
                "79|Ninth",
                "80|Tenth",
                "81|First",
                "82|Second",
                "83|Third",
                "84|Fourth",
                "85|Fifth",
                "86|Sixth",
                "87|Seventh",
                "88|Eighth",
                "89|Ninth",
                "90|Tenth",
                "91|First",
                "92|Second",
                "93|Third",
                "94|Fourth",
                "95|Fifth",
                "96|Sixth",
                "97|Seventh",
                "98|Eighth",
                "99|Ninth",
                "100|Tenth"
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
                + "6|al;vin ;\"  |  \"ambivewauwfu sma[nbmaj[nb|Sixth\r7|a;vnslirj|Seventh\n8|Testjao;sv garbage|Eighth\r\n9||Ninth\r"
                + "10|a;svna tbija nswowros snv|Tenth";
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            if (File.Exists(CommaFile))
                File.Delete(CommaFile);
            if (File.Exists(MultiLineEndFilePath))
                File.Delete(MultiLineEndFilePath);
            if (File.Exists(BiggerFilePath))
                File.Delete(BiggerFilePath);

            File.WriteAllLines(FilePath, Lines);
            File.WriteAllLines(CommaFile, CommaLines);
            File.WriteAllText(MultiLineEndFilePath, multiEndcontent);
            File.WriteAllLines(BiggerFilePath, bigLines);
        }
        const string TEST_FOLDER = @"C:\DocReaderTest\";
        string MultiLineEndFilePath = @"C:\DocReaderTest\MixedLineEnding.txt";
        string MultiLineEndWriteFilePath = @"C:\DocReaderTest\WRITE_MixedLineEnding.txt";
        string FilePath = @"C:\DocReaderTest\Reader.txt";
        string CommaFile = @"C:\DocReaderTest\Reader.csv";
        string BiggerFilePath = @"C:\DocReaderTest\BigReader.txt";
        [TestMethod]
        public void TestReaderGeneric()
        {
            DocMetaData.TESTMODE = true; //Allow page size below minimum for test purposes
            FilePrep();

            var r = new DocReader<TestRecordInheritance>("F", FilePath);
            Assert.AreEqual(2, r.Columns.Count);
            Assert.AreEqual(10, r.RecordCount);
            System.Diagnostics.Debug.WriteLine(r[0, 0].LineNumber + ":" + r[0, 0].Description + " - first line parsed LineNumber:Description");
        }
        [TestMethod]
        public void TestReadExtraColumns()
        {
            FilePrep();
            using (var reader = new DocReader("F", FilePath))                
            {                
                ((DocMetaData)reader.MetaData).AddDelimitedColumns("Test", "test 2");
                reader.MetaData.CanWrite = true;
                ((DocMetaData)reader.MetaData).AllowMissingColumns = true;

                var output = (DocMetaData)new DocMetaData(TEST_FOLDER, "ExtraColumnsOut.txt", "w")
                    .AddDetailedColumnCollection(reader.Columns)
                    .SetDelimiter('|');
                using (var writer = new DocWriter(output))
                {
                    foreach (var record in reader)
                    {
                        record["Test"] = "hi";
                        record["test 2"] = "5";
                        writer.AddDocRecord(record);
                    }
                }

            }
        }


        [TestMethod]
        public void TestRead()
        {
            DocMetaData.TESTMODE = true; //Allow page size below minimum for test purposes
            FilePrep();

            DocReader r = new DocReader("F", FilePath);
            Assert.AreEqual(2, r.Columns.Count);
            Assert.AreEqual('|', r.MetaData.Delimiter);
            var l = r.GetPage(0);
            Assert.AreEqual(10, l.Count);
            Assert.AreEqual(10, r.RecordCount);
            var rec = l[0];
            Assert.AreEqual("1", l[0]["LineNumber"]);
            Assert.AreEqual("Second", l[1]["Description"]);
            DocMetaData md = new DocMetaData(FilePath, "F");
            md.AddDelimitedColumns("LineNumber", "Description")
                .SetHasHeader(true)
                .SetDelimiter('|')
                .SetSkipLines(2);
            r.Dispose();
            r = new DocReader(md);
            Assert.AreEqual(2, r.Columns.Count);
            l = r.GetPage(0);
            Assert.AreEqual(8, l.Count);
            Assert.AreEqual(8, r.RecordCount); //skipped the first 2 lines, so record count should also go down by 2.
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
            Assert.AreEqual(8, r.RecordCount); //skipped first 2 lines
            Assert.AreEqual("3", l[0]["LineNumber"]);
            r.Dispose();
            var md2 = new DocMetaData(CommaFile, "C");
            md2.SetHasHeader(true);
            r = new DocReader(md2);
            Assert.AreEqual(',', r.MetaData.Delimiter);

            md= (DocMetaData)new DocMetaData(@"C:\DocReaderTest\ReaderFromCSV.txt", "F")
                .SetHasHeader(true)
                .AddDelimitedColumns("LineNumber", "Empty", "Description", "Empty", "Test")
                .SetDelimiter('\t');
            var w = new DocWriter(md);
            w.SetTextQualify(true, "Description", "Test");
            Dictionary<int, DocRecordColumnInfo> map = new Dictionary<int, DocRecordColumnInfo>();
            map[md.Columns["Test"].Position] = md2.Columns["LineNumber"]; //Position = 4 should be the
            foreach(var record in r)
            {
                w.AddDocRecord(record, map);
            }
            w.Dispose();
            md = (DocMetaData)new DocMetaData(@"C:\DocReaderTest\ReaderFromCSV.csv", "C")
                .SetHasHeader(true)
                .AddDelimitedColumns("Description", "LineNumber")
                .SetDelimiter(',');
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
            const int LOOP_LIMIT = nineNineHT;
            FilePrep();
            var md = (DocMetaData)new DocMetaData(TEST_FOLDER + "Inheritor.txt", "i")
                .SetHasHeader(true)
                .AddDelimitedColumns("LineNumber", "Description", "IsMod183")
                .SetDelimiter('|');
            using (var w = new DocWriter(md))
            {
                for (int i = 1; i <= LOOP_LIMIT; i++)
                {
                    bool b = i % 183 == 0;
                    var ti = new TestRecordInheritance(w.Columns, i, "Line # " + i, b); //initialize with constructor originall, will be a bit faster.
                    if (b)
                        ti.Description += " - Mod 183 = 0";
                    w.AddDocRecord(ti);
                }
            }
            using (var dr = new DocReader(md))
            {
                Assert.AreEqual(LOOP_LIMIT, dr.RecordCount);
            }
        }
        [TestMethod]
        public void InheritanceRecordTestUpdate()
        {
            const int LOOP_LIMIT = nineNineHT;
            FilePrep();
            var md = (DocMetaData)new DocMetaData(TEST_FOLDER + "Inheritor.txt", "i")
                .SetHasHeader(false)
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|');
            using (var w = new DocWriter(md))
            {
                for (int i = 1; i <= LOOP_LIMIT; i++)
                {
                    var ti = new TestRecordInheritance(w.Columns) //Update columns *after* constructor
                    {
                        LineNumber = i,
                        Description = "Line # " + i
                    };
                    if (i % 183 == 0)
                        ti.Description += " - Mod 183 = 0";
                    w.AddDocRecord(ti);
                }
            }
            using (var dr = new DocReader(md))
            {
                Assert.AreEqual(LOOP_LIMIT, dr.RecordCount);
            }
        }
        [TestMethod]
        public void InheritanceRecordSortTest()
        {
            /*
             * with 9.99.. million, 29 'page' file, sorter cache size of 5 
             * Index creation: 32 ~minutes
             * Write sorted file out: ~22 minutes
             * Total: ~54 minutes             
             * 
             * Cache size of 7:
             * Index Creation: ~27 minutes
             * Write sorted file out: ~20 minutes
             * Total: ~46 m
             * 
             * 
             * After switching from cache to storing sort columns in index: ~19m to create index, ~21 m to write out. total 40m
             * 
             * Storing sort columns in index + merging groups of index pages instead of going into a target index: ~8m to create index, ~20m to write out, total 28m. 
             * Note: Cache is still used some for indexer access from the sorter/getting a page's content
             */
            InheritanceRecordTest(); 
            using (var reader = new DocReader("i", TEST_FOLDER + "Inheritor.txt"))
            using (var sorter = new DocSorter(reader, 11, true, false, DuplicateHandling.Ignore, reader.Columns[1]))                
            {                
                var md = (DocMetaData)new DocMetaData(reader.FilePath + "_Sorted")
                    .AddDetailedColumnCollection(reader.Columns)
                    .SetFileAccess(FileAccess.Write)
                    .SetDelimiter('|');
                using (var writer = new DocWriter(md))
                {
                    for(int i =0; i < reader.PageCount; i++)
                    {
                        writer.BulkWrite(sorter.GetPage(i));
                    }
                }

            }
        }
        [TestMethod]
        public void InheritanceRecordSortToFileTest()
        {           
            InheritanceRecordTest();
            using (var reader = new DocReader("i", TEST_FOLDER + "Inheritor.txt"))
            using (var sorter = new DocSorter(reader, 1, true, false, DuplicateHandling.Ignore, reader.Columns[1]))
            {
                sorter.WriteToFile(reader.FilePath + "_SortedFromSorter");//don't bother with meta data, going to be ignored by bulk write anyway. Also, use getPageLINES in the method, less processing, same effect since no transforming.

            }
        }
        [TestMethod]
        public void InheritanceRecordDedupeToFileTest()
        {
            InheritanceRecordTest();
            using (var reader = new DocReader("i", TEST_FOLDER + "Inheritor.txt"))
            using (var sorter = new DocSorter(reader, 1, true, false, DuplicateHandling.Delete, reader.Columns[2]))
            {
                sorter.WriteToFile(reader.FilePath + "_DedupeFromSorter");//don't bother with meta data, going to be ignored by bulk write anyway. Also, use getPageLINES in the method, less processing, same effect since no transforming.

            }
        }
        [TestMethod]
        public void SortedReadTest()
        {
            FilePrep();
            DocMetaData.TESTMODE = true;
            var md = (DocMetaData)new DocMetaData(TEST_FOLDER, "SortedFile.txt", "w")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|');
            using (var r = new DocReader("r", FilePath, pageSize: 30))
            using (var s = new DocSorter(r, r.Columns["Description"]))
            using (var w = new DocWriter(md))
            {
                foreach(var record in s)
                {
                    w.AddDocRecord(record);
                }
            }
        }
        [TestMethod]
        public void BigSortedReadTest()
        {
            FilePrep();
            MetaDataBase.TESTMODE = true;
            var md = (DocMetaData)new DocMetaData(TEST_FOLDER + "BiggerSortedFile.txt", "w")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|');
            //using (var r = new DocReader("r", BiggerFilePath, pageSize: 50))
            using (var r = new DocReader("r", BiggerFilePath, pageSize: 90))
            using (var s = new DocSorter(r, 5, true, false, r.Columns["Description"], r.Columns["LineNumber"]))
            using (var w = new DocWriter(md))
            {
                foreach (var record in s)
                {
                    w.AddDocRecord(record);
                }
            }
        }
        [TestMethod]
        public void BigSortedReadToFileTest()
        {
            FilePrep();
            DocMetaData.TESTMODE = true;
            var md = new DocMetaData(TEST_FOLDER + "BiggerSortedFile.txt", "w")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|');
            using (var r = new DocReader("r", BiggerFilePath, pageSize: 50))
            using (var s = new DocSorter(r, 5, true, false, r.Columns["Description"], r.Columns["LineNumber"]))    
            {
                s.WriteToFile(TEST_FOLDER, "BigSortToFile.txt");
            }
        }
        [TestMethod]
        public void BigSortedReadDedupeToFileTest()
        {
            FilePrep();
            DocMetaData.TESTMODE = true;
            var md = new DocMetaData(TEST_FOLDER + "BiggerSortedFile.txt", "w")
                .AddDelimitedColumns("LineNumber", "Description")
                .SetDelimiter('|');
            using (var r = new DocReader("r", BiggerFilePath, pageSize: 50))
            using (var s = new DocSorter(r, 5, true, false, DuplicateHandling.Delete, r.Columns["Description"]))
            {
                s.WriteToFile(TEST_FOLDER, "BigSortToFile.NoDupe.txt");
            }
        }
        [TestMethod]
        public void BOM_ReadTest()
        {
            DocMetaData.TESTMODE = true;
            var md = (DocMetaData)new DocMetaData(TEST_FOLDER + "bomCheck.txt", "bc").SetDelimiter('|').SetPageSize(80);
            //md.SetFileEncoding(System.Text.Encoding.UTF8);
            md.SetFileEncoding(null); 
            using (var r = new DocReader(md))
            {
                foreach(var line in r)
                {
                    System.Diagnostics.Debug.WriteLine(line[0]);
                }
            }
        }
        [TestMethod]
        public void MultiRecordTest()
        {
            PrepDirectory(true);
            var input = GetFile("TestFiles", "MultiRecordIn.txt");
            var mrmd = new MultiRecordDocMetaData(input.FullName, "mrm");
            mrmd.SetDelimiter(',').SetMultiLineEndDelimiters("\r", "\n", "\r\n");
            var c0 = mrmd.CreateCollection("0"); //Key Column already included by default            
            c0.AddColumn("Desc");
            c0.AddColumn("WorkDate", dataType: DocRecordColumnType.Date);
            var c1 = mrmd.CreateCollection("1", true, "Role", "Secondary Role", "Quote");
            //c1.AddColumn("Key");
            /*
            c1.AddColumn("Role");
            c1.AddColumn("SecondaryRole");
            c1.AddColumn("Quote");*/
            mrmd.CreateCollection("3", true, "DESC");
            //c3.AddColumn("Key");
            using (var r = new DocReader(mrmd))
            {
                var p = r.GetPage(0);
                Assert.IsTrue(p[0].HasColumn("Desc"));

                Assert.IsTrue(p[0].TryGet("WorkDate", out object dt));
                Assert.AreEqual(new DateTime(2019, 06, 12), (DateTime)dt);
                Assert.IsFalse(p[0].HasColumn("Role"));
                Assert.IsTrue(p[1].HasColumn("Role"));
                Assert.AreEqual("END", p[4]["DESC"].Trim());
                Assert.AreEqual("3", p[4][MultiRecordDocMetaData.DEFAULT_KEY_NAME]);
            }
        }

    }

    public class TestRecordInheritance: DocRecord
    {
            public TestRecordInheritance() : base() { }
        public TestRecordInheritance(DocRecordColumnCollection columnCollection, int LineNumber, string Description, bool mod)
            :base(columnCollection, true, 
				new List<string> { LineNumber.ToString(), Description , mod? "true": "false" })
        {
            if (columnCollection.Count != 3)
                throw new ArgumentException("Incorrect number of columns specified", nameof(columnCollection));
        }
        public TestRecordInheritance(DocRecordColumnCollection col)
            : base(col, true)
        {
        }
        public TestRecordInheritance(DocRecordColumnCollection col, IList<string> parsedContent)
            : base(col, true, parsedContent)
        {
        }
        protected override void Configure(DocRecordColumnCollection owner, bool? canWrite = default(bool?), IList<string> parsedContent = null)
        {
            base.Configure(owner, canWrite, parsedContent);
            if(owner.Count != 2 && owner.Count != 3)
                throw new ArgumentException("Incorrect number of columns specified", nameof(owner));
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
        public bool IsMod183
        {
            get { return this[nameof(IsMod183)] == "true"; }
            set
            {
                if (value)
                    this[nameof(IsMod183)] = "true";
                else
                    this[nameof(IsMod183)] = "false";
            }
        }
    }
}
