using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.DataBase;
using System.IO;
using SEIDR.Doc;

namespace SEIDR.OperationServiceModels
{
    public class BatchProfile: DatabaseObject
    {
        #region Base constructors
        public BatchProfile(DatabaseConnection db) : base(new DatabaseManager(db)) { }
        public BatchProfile():base(){ }
#endregion

        public int? BatchProfileID { get; private set; } = null;
        public string BatchTypeCode { get; private set; }
        
        
        public int ExecutionThread { get; private set; } = 1;
        public string InputFolder { get; private set; }
        public string FileMask { get; private set; }
        /// <summary>
        /// TODO: set up Schedule object... Use would be in the DB, though, just calling a proc to create Batches as needed
        /// </summary>
        public int ScheduleID { get; private set; } = 0;
        public int DayOffset { get; private set; } = 0;
        public string InputFileDateFormat { get; private set; } = "*<YYYY><MM><DD>*";
        public int MinimumFileCount { get; private set; } = 0;
        public int MaximumFileCount { get; private set; }
        public bool MultiFile
        {
            get
            {
                return MaximumFileCount > 1;
            }
        }
        public List<Batch_File> PrepFileInfo(FileInfo[] fileList)
        {
            List<Batch_File> rl = new List<Batch_File>();
            fileList.ForEach(f => {
                DateTime temp;
                if(f.Name.ParseDate(InputFileDateFormat, out temp))
                {
                    rl.Add(Batch_File.FromFileInfo(f, temp));
                }
                else
                {
                    rl.Add(Batch_File.FromFileInfo(f, null));
                }
            });
            return rl;
        }
    }
}
