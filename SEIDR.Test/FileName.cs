using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR.Doc;

namespace SEIDR.Test
{
    /// <summary>
    /// Summary description for FileName
    /// </summary>
    [TestClass]
    public class FileName
    {
        public FileName()
        {
            //
            // TODO: Add constructor logic here
            //            
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void FileNameTest()
        {
            //
            // TODO: Add test logic here
            //

            string DateFormat = "<a:0YYYY0MM-1DD>test_<a:YY>_<a:MM>_<a:DD>_<DD>.txt";
            DateTime d = new DateTime(2017, 12, 02);
            Assert.AreEqual("test_17_12_01_02.txt", DateFormat.GetFileName(d));

            DateFormat += "<b:-1YY-2M38D>_<b:YYYY><b:MM><b:D>.txt";
            Assert.AreEqual("test_17_12_01_02.txt_2016119.txt", DateFormat.GetFileName(d));
            /// <summary>
            /// Generates a file name using the dateFormat and passed FileDate. Multiple date offsets can be used by offsetting with an alias.
            /// <para>E.g., &lt;a:0YYYY0MM-1D>test_&lt;a:YY>_&lt;a:MM>_&lt;a:DD>_&lt;DD>.txt for date 2017/12/2 should lead to test_17_12_01_02.txt</para>
            /// </summary>
            //string s = GetFileName(string dateFormat, DateTime fileDate)
        }
        [TestMethod]
        public void FileDateParseTest()
        {
            string docName = "test_17_12_01_02.txt_2016119.txt";
            string dateFormat = "*<-1YY-2M38D>txt_<YYYY><MM><D>.txt";
            DateTime d = new DateTime(2017, 12, 02);
            DateTime n = d.Date;
            docName.ParseDateRegex(dateFormat, ref n);
            Assert.AreEqual(d, n);
        }
    }
}
