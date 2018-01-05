using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.DataBase;
using SEIDR.Doc;
using System.IO;
using System.Data;
using SEIDR;

namespace SEIDR.OperationServiceModels
{
    public class Batch: DatabaseObject<Batch>
    {        
        public Batch():base() { }
        public Batch(DatabaseManager db) : base(db) { }
        public Batch(int BatchProfileID, DatabaseManager db = null) : base(db) { this.BatchProfileID = BatchProfileID; }
        public int? BatchID { get; private set; } = null;
        /// <summary>
        /// For some thread control stuff. And file limiting, etc. 
        /// <para>BatchType Table also contains a description for the type of files expected to be used
        /// </para>
        /// </summary>
        public string BatchTypeCode { get; private set; }
        public string BatchStatus { get; set; }        
        /// <summary>
        /// Depend on Operation + BatchType + Profile. If Operation/BatchType/Profile do not set, will be picked by whatever Thread has open spots in its queue and updated
        /// </summary>
        public byte? ThreadID { get; private set; }
        public bool IgnoreParents { get; private set; }
        public bool ForceOperationSequence { get; private set; }
        /// <summary>
        /// Sets the ThreadID on the batch if it's missing/null
        /// </summary>
        /// <param name="ID"></param>
        public void SetMissingThreadID(byte ID)
        {
            if (ThreadID == null)
                ThreadID = ID;
        }
        public int BatchProfileID { get; private set; }

        public DateTime BatchDate { get; private set; }
        /// <summary>
        /// The current step number - determines the operation + parameters.
        /// </summary>
        public short Step { get; private set; } = 1;
        /// <summary>
        /// Current step number stored in raw table. Returns <see cref="Step"/>, which is used by view
        /// </summary>
        public short CurrentStep { get { return Step; } }
        /// <summary>
        /// Number of attempts on the operation/step. Will be incremented each time work starts.
        /// <para>Begins at 1 for the first attempt.</para>
        /// <para>Resets when the status/Current step change</para>
        /// </summary>
        public short AttemptCount { get; private set; } = 1;

        public string SuccessNotification { get; private set; }
        public string FailureNotification { get; private set; }
        
        public int Profile_OperationID { get; private set; }
        public int OperationID { get; private set; }

        public string Operation { get; private set; }
        public string OperationSchema { get; private set; }
        public bool OperationSuccess { get; set; }
        /// <summary>
        /// Operation's Version.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Register the list of Batch_File records with the profile. Returns the first batch. 
        /// <para>Consider passing to an Operator thread if the corresponding one is free. 
        /// </para><para>(Should include a bool on the worker indicating it's free, also locking. </para>
        /// <para>Be able to set the batch so that it starts doing work instead of querying database for work.)</para>
        /// </summary>
        /// <param name="mgr"></param>
        /// <param name="BatchProfileID"></param>
        /// <param name="fileList">List of Batch_Files to be created by a File Watcher</param>
        /// <param name="rejected">Files that were not added to any batches as a result of hash or path matching originals of Batch_File's already in use</param>
        /// <returns>First batch registered with the files, or null</returns>
        public static IEnumerable<Batch> Register(DatabaseManager mgr, int BatchProfileID, List<Batch_File> fileList, out IEnumerable<Batch_File> rejected)
        {
            var param = new
            {
                BatchProfileID,
                FileXML = Batch_File.ToXML(fileList)
            };            
            using (var c = mgr.GetBasicHelper(param, "usp_Batch_BulkRegister"))
            {
                c.RetryOnDeadlock = false;
                c.RethrowException = false;
                c.BeginTran();                
                DataSet ds = mgr.Execute(c);

                if (c.ReturnValue != 0)
                {
                    c.RollbackTran();
                    rejected = null;
                    return null;
                }
                else
                    c.CommitTran();

                IEnumerable<Batch> bList = ds.ToContentList<Batch>(0);
                IEnumerable<Batch_File> fList = ds.ToContentList<Batch_File>(1);
                rejected = ds.ToContentList<Batch_File>(2);
                
                if (fList == null || fList.Count() == 0)
                    return bList;

                bList.ForEach(b =>
                {
                    b._Files = fList.Where(f => f.BatchID == b.BatchID).ToList();
                });

                //Batch b = mgr.SelectSingle<Batch>(c, false);
                //if (b != null)                
                //    b._Files = fileList;            
                return bList;
            }
        }
        List<Batch_File> _Files = null;/*
        public IEnumerable<Batch_File> Files
        {
            get
            {                
                if (_Files == null)
                    _Files = GetList<Batch_File>("SEIDR.usp_Batch_File_sl");                
                return _Files;
            }
        }*/

        public string FileXML => null;/*
        {
            get
            {
                return Batch_File.ToXML(Files);
            }
        }*/
        /// <summary>
        /// If the file exists and is not already in the batch, add it to the batch.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="FileDate">Specifies file date - file creation time will be used if not provided</param>
        public Batch_File AddFile(string FilePath, DateTime? FileDate = null) => AddFile(new FileInfo(FilePath), FileDate);
        /// <summary>
        /// If the file exists and the path is not in the batch yet, add it to the batch.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="FileDate">Specifies file date - file creation time will be used if not provided</param>
        public Batch_File AddFile(FileInfo file, DateTime? FileDate = null)
        {
            if (!file.Exists || _Files.Exists(a => a.FilePath == file.FullName) )
                return null;            
            Batch_File f = Batch_File.FromFileInfo(file, FileDate);
            _Files.Add(f);
            return f;
        }        
        /// <summary>
        /// Removes the file - will be applied when the batch status is updated.
        /// </summary>
        /// <param name="FilePath"></param>
        public void DeleteFile(string FilePath)
        {
            //if (Files.Count() == 0)
                return;
            _Files.RemoveAll(f => f.FilePath == FilePath);
        }
    }
}
