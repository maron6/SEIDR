using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR.Doc.Compression;
using System.IO;

namespace SEIDR.Test
{
    /*
    [TestClass]
    public class CompressionTest
    {        
        string ToCompress = @"C:\Users\Owner\Documents\CompressionTesting\world95.txt";
        string Compressed = "";
        string ToDecompress = "";
        string Decompressed = "";
        [TestMethod]
        public void Compress()
        {
            if (File.Exists(Compressed))
                File.Delete(Compressed);
            PLZ p = new PLZ(ToCompress,  null, Compressed, true);
            p.Key = "Test!";
            p.Compress();
        }
        [TestMethod]
        public void DeCompress()
        {
            if (File.Exists(Decompressed))
                File.Delete(Decompressed);
            PLZ p = new PLZ(ToDecompress, Destination: Decompressed);
            p.Key = "Test!";
            p.Compress();
        }
    }
    */
}
