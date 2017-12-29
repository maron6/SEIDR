using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Doc.DocQuery;
using SEIDR.Doc;

namespace SEIDR.Test
{
    [TestClass]
    public class DelimitedRecordHashTest
    {
        string[] Header = new[] { "A", "B", "C", "D" };
        DelimitedRecord r1;
        DelimitedRecord r2;
        DelimitedRecord r3;
        DelimitedRecord r4;
        public DelimitedRecordHashTest()
        {            
            r1 = new DelimitedRecord(Header, new[] { "Content!", "Test!", "Heya heya", "3" }, Header.Length);
            r2 = new DelimitedRecord(Header, new[] { "content!", "Test2", "short" }, Header.Length);            
            r3 = new DelimitedRecord(Header, new[] { "Content!", null, "short", "3" }, Header.Length);
            r4 = new DelimitedRecord(Header, new[] { "content!", "Test!", "short" }, Header.Length);
        }
        [TestMethod]
        public void testRollingHash()
        {
            var w = r1.GetPartialHash("A");
            var x = r2.GetPartialHash("A");
            var y = r2.GetPartialHash("B");
            var z = r3.GetPartialHash("B");
            Assert.AreNotEqual(w, z);
            Assert.AreNotEqual(x, w);
            Assert.AreNotEqual(x, y);
            Assert.AreNotEqual(x, z);
            Assert.AreNotEqual(y, w);
            Assert.AreNotEqual(y, z);
            y = r4.GetPartialHash("A");
            Assert.AreEqual(x, y);
            DelimitedRecord.NullIfTruncated = true;
            w = r1.GetPartialHash("D");
            x = r2.GetPartialHash("D");
            y = r3.GetPartialHash("D");
            z = r4.GetPartialHash("D");
            Assert.AreEqual(w, y);
            Assert.IsNull(x);
            Assert.IsNull(z);            
        }        
        [TestMethod]
        public void DelimitedRecordHashTableTest()
        {
            DelimitedRecordHashTable drht = new DelimitedRecordHashTable("A", "C");
            drht.Add(r1, r3, r2, r4);
            Func<DelimitedRecord, int> a = (r => drht.CheckCount(r));
            Assert.AreEqual(2, a(r2));
            Assert.AreEqual(1, a(r1));
            Assert.AreEqual(1, a(r3));
            Assert.AreEqual(2, a(r4));            
            DelimitedRecordHashTable drh2 = new DelimitedRecordHashTable("C", "A");
            drh2.Add(r1, r3, r2, r4);            
            Assert.AreEqual(2, drh2.CheckCount(r2));
            Assert.AreNotEqual(drht.CheckHash(r2), drh2.CheckHash(r2));
            drht.Clear();
            drh2.Clear();
            Assert.AreEqual(0, drht.CheckCount(r2));            
        }
    }
}
