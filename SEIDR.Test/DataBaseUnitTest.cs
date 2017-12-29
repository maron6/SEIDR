using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR.DataBase;
using System.Data;

namespace SEIDR.DataBase.Test
{
    [TestClass]
    public class DataBaseUnitTest
    {
        //static DatabaseConnection db = new DatabaseConnection(@"OWNER-PC\SQLEXPRESS", "MIMIR");        
        static DatabaseConnection db = new DatabaseConnection(@"DESKTOP-MUB3P9M\SQLEXPRESS", "MIMIR") { ApplicationName = "'test;'" };
        DatabaseManager m = new DatabaseManager(db, "SEIDR");
        #region Command text to Validate existence/setup of procedures/test table
        const string CHECK_FIRST_PROC = @"
IF OBJECT_ID('SEIDR.usp_DatamanagerTest', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE SEIDR.usp_DataManagerTest as set nocount on;')
END
DECLARE @SQL varchar(max) = '
ALTER PROCEDURE SEIDR.usp_DataManagerTest
    @MapName varchar(50),
    @TestOther int,
    @DefaultTest bit = 1
AS
BEGIN
    SELECT @MapName [Map], @TestOther [TestOther], @DefaultTest [DefaultTest]
END'
exec (@SQL)
";
        const string CHECK_SECOND_PROC = @"
IF OBJECT_ID('SEIDR.DM_Test') IS NOT NULL 
    DELETE SEIDR.DM_Test --Truncate would also be fine, but removes ident
ELSE
BEGIN    
    EXEC('CREATE TABLE SEIDR.DM_Test(ID int identity(1,1) primary key , DC datetime)' )
END
IF OBJECT_ID('SEIDR.usp_DataManagerTest2', 'P') is null
    EXEC('CREATE PROCEDURE SEIDR.usp_DatamanagerTest2 AS SET NOCOUNT ON;')

    DECLARE @SQL varchar(max) = '
ALTER PROCEDURE SEIDR.usp_DataManagerTest2
    @ID int output
AS
BEGIN
	INSERT INTO SEIDR.DM_Test(DC)
	VALUES(GETDATE())
    SET @ID = SCOPE_IDENTITY()
	SELECT * FROM SEIDR.DM_TEST
    RETURN 1
END'

EXEC (@SQL) 

";
        #endregion
        public DataBaseUnitTest()
        {
            m.ExecuteText(CHECK_FIRST_PROC);
            m.ExecuteTextNonQuery(CHECK_SECOND_PROC);
        }
        public class TestClass
        {
            public int ThreadID { get; set; }
            public int BatchID { get; set; }
            public int Test { get; set; }
            public string Files { get; set; }
            public string Name { get; set; }
        }
        [TestMethod]
        public void DatabaseConnectionFromString()
        {
            string conn = "SERVER=test;Database='Test;';Application Name={Hey My App}";
            DatabaseConnection db = DatabaseConnection.FromString(conn);
            string expected = "Server='test';Database='Test;';Application Name='{Hey My App}';Trusted_Connection=true;";
            Assert.AreEqual(expected, db.ConnectionString);
        }
        [TestMethod]
        public void DatabaseExtensionTest()
        {
            var b = new TestClass
            {
                ThreadID = 2,
                BatchID = 1,
                Test = 3,
                Files = "Test123",
                Name = "TEST"
            };
            DataTable dt = b.ToTable("udt_Batch", "FileXML", "Files");
            var r = dt.GetFirstRowOrNull();
            Assert.IsNotNull(r);            
            Assert.AreEqual(b.ThreadID, (int)r["ThreadID"]);
            Assert.IsFalse(r.Table.Columns.Contains("Files"));
            TestClass b2 = r.ToContentRecord<TestClass>();
            Assert.AreEqual(b.ThreadID, b2.ThreadID);
            Assert.AreEqual(b.BatchID, b2.BatchID);
            Assert.AreEqual(b.Name, b2.Name);
        }

        [TestMethod]
        public void BasicExecution()
        {   
            var map = new
            {
                MapName="Teeeeeest",
                TestOther = 22
            };
            var r = m.Execute("SEIDR.usp_DatamanagerTest", map).GetFirstRowOrNull();
            Assert.IsNotNull(r);
            Assert.AreEqual(r["Map"], map.MapName);
            Assert.AreEqual(r["TestOther"], map.TestOther);
            Assert.AreEqual(r["DefaultTest"], true);
            map = new
            {
                MapName = "Test 2!", TestOther = 21
            };
            r = m.Execute("SEIDR.usp_DatamanagerTest", map).GetFirstRowOrNull();
            Assert.IsNotNull(r);
            Assert.AreEqual(r["Map"], map.MapName);
            Assert.AreEqual(r["TestOther"], map.TestOther);
            Assert.AreEqual(r["DefaultTest"], true);
            DatabaseManagerHelperModel h = new DatabaseManagerHelperModel(map, "SEIDR.usp_DatamanagerTest");
            Assert.AreEqual("[usp_DatamanagerTest]", h.Procedure);
            Assert.AreEqual("[SEIDR].[usp_DatamanagerTest]", h.QualifiedProcedure);
            h.SetKey("DefaultTest", false);
            r = m.Execute(h).GetFirstRowOrNull();
            Assert.IsNotNull(r);
            Assert.AreEqual(r["Map"], map.MapName);
            Assert.AreEqual(r["TestOther"], map.TestOther);
            Assert.AreEqual(r["DefaultTest"], false);
            using (h = m.GetBasicHelper(true))
            {
                h.ParameterMap = map;
                h.Procedure = "usp_DatamanagerTest";
                h.ExpectedReturnValue = 0;
                h.SetKey("DefaultTest", false);
                m.Execute(h);
                Assert.AreEqual(0, h.ReturnValue);
                h.SetKey("DefaultTest", true);
                r = m.Execute(h).GetFirstRowOrNull();
                Assert.IsNotNull(r);
                Assert.AreEqual(r["DefaultTest"], true);
            }
        }

        [TestMethod]
        public void TestTran()
        {
           
            using (var h = m.GetBasicHelper(true))
            {
                h.BeginTran();
                h.AddKey("ID", 0);
                h.Procedure = "usp_DataManagerTest2";
                h.ExpectedReturnValue = 1;
                m.Execute(h);
                int id = (int)h["ID"];
                Assert.AreNotEqual(0, id);
                Assert.AreNotEqual(0, h.ReturnValue);
                m.Execute(h);
                Assert.AreNotEqual(id, h["ID"]); //Should be greater.
                var ds = m.Execute(h).GetFirstTableOrNull();
                Assert.IsNotNull(ds);
                Assert.AreNotEqual(0, ds.Rows.Count);
                h.RollbackTran();
                h.BeginTran();
                ds = m.Execute(h, true).GetFirstTableOrNull();
                Assert.AreEqual(1, ds.Rows.Count);
                Assert.IsFalse(h.HasOpenTran);                
                ds = m.Execute(h).GetFirstTableOrNull();
                Assert.AreEqual(2, ds.Rows.Count);
            }
        }
        [TestMethod]
        public void TestUnexpectedReturnValue_Rollback()
        {
            using (var h = m.GetBasicHelper(true))
            {
                h.BeginTran();
                h.AddKey("ID", 0);                
                h.QualifiedProcedure = "SEIDR.usp_DataManagerTest2";
                Assert.AreEqual("[usp_DataManagerTest2]", h.Procedure);
                Assert.AreEqual(m.DefaultSchema, h.Schema);
                h.ExpectedReturnValue = 0;
                m.Execute(h);
                Assert.IsTrue(h.IsRolledBack);
            }
        }
    }
}
