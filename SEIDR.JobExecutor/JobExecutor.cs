using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SEIDR.Doc;
using SEIDR;
using SEIDR.OperationServiceModels;
using System.Threading;
using System.ServiceProcess;
using System.ComponentModel.Composition;
using System.Configuration;

namespace SEIDR.JobExecutor
{
    [Export(typeof(IOperatorManager))]
    public class JobExecutor : ServiceBase, IOperatorManager
    {
        const string CLEAN_LOCKS = "SEIDR.usp_Batch_CleanLocks";
        public JobExecutor()
        {
            DataBase.DatabaseManagerHelperModel.DefaultRetryOnDeadlock = true;
            ServiceName = "SEIDR.JobExecutor";
            AutoLog = true;
            CanStop = true;
            CanPauseAndContinue = true;
            CanShutdown = false;
            CanHandleSessionChangeEvent = false;
            CanHandlePowerEvent = false;
        }        
        public const byte CANCEL_ID = 201;
        //public const int QUEUE_ID = 1;
        public byte QueueThreadCount = 1;
        public byte ExecutionThreadCount = 4; //Default
        public OperationServiceModels.Status.ServiceStatus _MyStatus = new OperationServiceModels.Status.ServiceStatus();
        public OperationServiceModels.Status.ServiceStatus MyStatus { get { return _MyStatus; } }
        List<Operator> MyOperators;
        void SetupFromConfig()
        {

            var appSettings = ConfigurationManager.AppSettings;
            int tempInt;
            string temp = appSettings["Timeout"];
            if (!int.TryParse(temp, out tempInt) || tempInt < 60)
                tempInt = 60;

            DataBase.DatabaseConnection db = new DataBase.DatabaseConnection(
                appSettings["DatabaseServer"],
                appSettings["DatabaseCatalog"]
                );
            db.Timeout = tempInt;

            _MGR = new DataBase.DatabaseManager(db, "SEIDR") { RethrowException = false, ProgramName = "SEIDR.JobExecutor"};
            OperationExecutor.ExecutionManager = _MGR.Clone(reThrowException: true, programName: "SEIDR.JobExecutor Query");
            

            LogDirectory = appSettings["LogRootDirectory"] ?? @"C:\Logs\SEIDR.Operator\";
            if (!Directory.Exists(LogDirectory))
            {
                try
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                catch (Exception ex)
                {
                    LogDirectory = @"C:\Logs\SEIDR.Operator\";
                    if (!Directory.Exists(LogDirectory))
                        Directory.CreateDirectory(LogDirectory);
                    LogFileMessage("Error Checking LogDirectory: " + ex.Message);
                }
            }
            
            try
            {
                OperationExecutor.SetLibrary(appSettings["OperationLibrary"]);
            }
            catch(Exception ex)
            {
                LogFileMessage("Library set up error - " + ex.Message);
                throw;
            }
            Mailer.SMTPServer = appSettings["SmtpServer"];
            Mailer.Domain = appSettings["MailDomain"];

            _Mailer = new Mailer("SEIDR.OperatorManager", appSettings["StatusMailTo"]);
            OperationExecutor.ExecutionAlerts = new Mailer("SEIDR.Operator");

            temp = appSettings["ThreadCount"];
            if (!int.TryParse(temp, out tempInt))
                tempInt = 4;
            else if (tempInt > 15)
                tempInt = 15;

            byte ThreadCount;            
            ThreadCount = (byte)tempInt;
            ExecutionThreadCount = ThreadCount;
            
            temp = appSettings["QueueThreadCount"];
            if (!byte.TryParse(temp, out ThreadCount) || ThreadCount < 1)
                ThreadCount = 1;
            else if (ThreadCount > 6)
                ThreadCount = 6;
            QueueThreadCount = ThreadCount;

            temp = appSettings["BatchSize"];
            if (!byte.TryParse(temp, out _BatchSize) || _BatchSize < 1)
                _BatchSize = 1;
            else if (_BatchSize > 10)
                _BatchSize = 10;

        }
        
        public Operator GetOperator(OperatorType type, byte ID)
        {
            return (from o in MyOperators
                    where o.MyType == type
                    && o.ID == ID
                    select o).FirstOrDefault();
        }

        public Operator GetOperatorByBatchID(OperatorType type, int BatchID)
        {
            var ops = (from op in MyOperators
                       where op.MyType == type
                       select op);
            foreach(var op in ops)
            {
                if (op.CheckForBatch(BatchID))
                    return op;
            }
            return null;
        }

        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                JobExecutor om = new JobExecutor();
                om.Run();
            }
            else
            {
                Run(new JobExecutor());
                //Don't use the array version, 
                //because we don't have multiple implementations of ServiceBase in the executable
            }
        }
        /// <summary>
        /// Static to set from static constructor time (before instance constructor)
        /// </summary>
        static readonly DateTime STARTUP = DateTime.Now;
        public void Run()
        {            
            SetupFromConfig();
            LogFileMessage("STARTING UP");
            #region Operator Set up
            MyOperators = new List<Operator>();
            for (byte i = 1; i <= ExecutionThreadCount; i++)
            {
                MyOperators.Add(new OperationExecutor(this, i));
            }
            //MyOperators.Add(new Queue(this, QUEUE_ID));
            for(byte i= 1; i <= QueueThreadCount; i++)
            {
                MyOperators.Add(new Queue(this, i));
            }
            MyOperators.Add(new CancellationExecutor(this, CANCEL_ID));

            _ServiceAlive = true;
            #endregion

            DataManager.Execute(CLEAN_LOCKS); //clean up locks before starting up.
            MyOperators.ForEach(o => o.Call());
            LogFileMessage("START UP DONE, OPERATORS STARTED");

            bool EmailSent = false, StatusFileLogged = false;
            Task LibMaintenance = new Task(() => {
                try
                {
                    OperationExecutor.CheckLibrary();
                }
                catch(Exception ex)
                {
                    LogFileMessage("Check Operation Library - " + ex.Message);
                }
            });

            while (ServiceAlive)
            {
                _mre.WaitOne();
                //Make sure library is up to date and table data sync'd
                OperationExecutor.CheckLibrary(); 
                _mre.WaitOne();

                DateTime n = DateTime.Now;
                int hour = n.Hour;
                int minute = n.Minute;
                if (minute % 60 == 0)
                {
                    if (!EmailSent)
                    {
                        _Mailer.SendMailAlert(ServiceName + " Status", GetOverallStatus(true));
                        EmailSent = true;                        
                    }                    
                }
                else
                    EmailSent = false;
                if (minute % 5 == 0)
                {
                    if (!StatusFileLogged)
                    {
                        LogFileMessage(GetOverallStatus(false));
                        StatusFileLogged = true;
                        MyStatus.WriteToFile(LogDirectory);                         
                        //Don't use the daily folder for current status XML file.   
                        //Maybe to do: Delete old log directories? Not something I'd consider especially important, though..                     
                    }

                    if (LibMaintenance.Status.In(TaskStatus.RanToCompletion, TaskStatus.Created, TaskStatus.Canceled))
                        LibMaintenance.Start();
                }
                else
                    StatusFileLogged = false;
                Thread.Sleep(5 * MILISECOND_TO_SECOND);
            }            
        }       
        const int MILISECOND_TO_SECOND = 1000;
        string GetOverallStatus(bool HTML)
        {
            string 
                PARA = "<p>", 
                PARA_END = "</p>", 
                BREAK = "<br />";
            if (!HTML) { PARA = ""; PARA_END = Environment.NewLine; BREAK = Environment.NewLine; }

            string Message = 
                PARA +
                StartupTimeMessage +
                PARA +
                CurrentTimeMessage + 
                PARA_END + PARA + 
                "Execution Operator Count: " + ExecutorCount +
                PARA_END + BREAK;
            var orderedOperators = MyOperators.OrderBy(o => ((int)o.MyType * 1000) + o.ID);
            foreach(Operator o in orderedOperators)
            {
                Message += PARA + o.Name + ", " + o.MyType.ToString() + " - " + o.ID + PARA_END + PARA
                    + o.MyStatus.LastStatusMessage 
                    + (o.MyStatus.LastStatus.HasValue 
                        ? (" - " + o.MyStatus.LastStatus.Value.ToString("MMM dd, yyyy hh:mm"))
                        : string.Empty)
                    + PARA_END + PARA + "Working ? " + o.Working.ToString() 
                    + "     WorkQueue Size: " + o.Workload 
                    + PARA_END + BREAK;
            }
            return Message + BREAK + BREAK;
        }
        ManualResetEvent _mre = new ManualResetEvent(true);
        bool _Paused = false;
        public bool Paused { get { return _Paused; } }
        protected override void OnPause()
        {
            LogFileMessage("PAUSED");
            _Paused = true;
            _mre.Reset();            
        }
        protected override void OnContinue()
        {
            LogFileMessage("CONTINUE");
            _Paused = false;            
            _mre.Set();
        }
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            Thread Main = new Thread(() =>
            {
                Run();
            })
            {
                IsBackground = false,
                Name = "SEIDR.OperatorManager"
            };
            Main.Start();
        }
        protected override void OnStop()
        {
            base.OnStop();
            int MaxWaitLoops = 15;
            int WaitLoops = 0;
            _ServiceAlive = false;
            while(WorkingCount > 0 && WaitLoops < MaxWaitLoops)
            {
                RequestAdditionalTime(14 * 1000);
                Thread.Sleep(11 * 1000);
                WaitLoops++;
            }
        }
        DataBase.DatabaseManager _MGR;
        public DataBase.DatabaseManager DataManager { get { return _MGR; } }
        Mailer _Mailer; //Sends status emails only...
        bool _ServiceAlive;
        public bool ServiceAlive
        {
            get
            {
                return _ServiceAlive;
            }
        }
        #region Logging
        public bool LogBatchError(Batch errBatch, string Message, string ExtraInfo, byte? ThreadID, int? Batch_FileID = default(int?))
        {
            const string sproc = "SEIDR.usp_Batch_Error_i";
            var m = new
            {
                errBatch.BatchID,
                Message,
                Extra = ExtraInfo,
                Batch_FileID,
                ThreadID
            };
            try
            {
                _MGR.ExecuteNonQuery(sproc, m);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public bool LogBatchError(Batch errBatch, string Message, string ExtraInfo, int? Batch_FileID = default(int?))
            => LogBatchError(errBatch, Message, ExtraInfo, errBatch.ThreadID, Batch_FileID);
        public bool LogBatchError(Operator caller, Batch errBatch, string Message, string Extra, int? Batch_FileID = default(int?))
        {
            caller.MyStatus.SetStatus(Message, OperationServiceModels.Status.ThreadStatus.StatusType.Error);
            bool a = LogBatchError(errBatch, Message, Extra, (byte)caller.ID, Batch_FileID);
            bool b = LogFileError(caller, errBatch, Message);
            return a && b;
        }
        public bool LogFileError(Operator callingOperator, Batch b, string Message)
        {
            string tempMessage = CurrentTimeMessage;
            if (b != null)
                tempMessage += $"BatchProfile {b.BatchProfileID}, BatchID: [{b.BatchID}], Step: {b.CurrentStep}, BatchDate: [{b.BatchDate.ToString("MM dd yyyy")}] ";
            tempMessage += Message;
            string File = Path.Combine(DailyLogDirectory, string.Format(LogFileFormat, callingOperator.Name, callingOperator.ID));
            try
            {
                System.IO.File.AppendAllText(File, tempMessage);
            }
            catch
            {                
                callingOperator.MyStatus.SetStatus("Could not set error!", OperationServiceModels.Status.ThreadStatus.StatusType.Error);                
                return false;
            }
            return true;
        }
        string CurrentTimeMessage
        {
            get
            {
                return "[" + DateTime.Now.ToString("MM dd yyyy hh:mm:ss") + "] ";
            }
        }
        readonly string StartupTimeMessage = "[SERVICE STARTUP TIME: " + STARTUP.ToString("MM dd yyyy hh:mm:ss") + "] ";
        void LogFileMessage(string Message)
        {
            string tempMessage = CurrentTimeMessage;
            tempMessage += Message;
            string File = Path.Combine(DailyLogDirectory, "SEIDR.OperatorManager.txt");
            try
            {
                System.IO.File.AppendAllText(File, tempMessage);
            }
            catch { }
        }
        public bool LogError(Operator caller, string Message)
        {
            return LogFileError(caller, null, Message);
        }
        string LogDirectory;
        string DailyLogDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LogDirectory))
                    LogDirectory = @"C:\SEIDR.Operator\Logs\";
                return Path.Combine(LogDirectory, DateTime.Now.ToString("yyyy_MM_dd"));
            }
        }
        string LogFileFormat = "SEIDR.{0}_{1}.txt";
        #endregion
        #region File Information helpers
        public long GetFileSize(string FilePath)
        {
            FileInfo f = new FileInfo(FilePath);
            if (f.Exists)
                return f.Length;
            return -1;
        }
        public DateTime ParseNameDate(FileInfo file, string format, int dateOffset = 0)
        {
            DateTime Default = file.CreationTime.AddDays(dateOffset);
            DateTime temp = Default.AddDays(0);
            string fName = Path.GetFileNameWithoutExtension(file.Name);
            if (format == "UNSPECIFIED")
            {
                string f = fName;
                f = System.Text.RegularExpressions.Regex.Replace(f, @"[^0-9]+", "");
                if (DateFormatter.ParseOnce(f, out temp))
                    return CheckSQLDateValid(temp, fName, format) ? temp.AddDays(dateOffset) : Default;
                return Default;
            }
            if (fName.ParseDate(format, out temp))
            {
                temp = temp.AddDays(dateOffset);                
                if (CheckSQLDateValid(temp, fName, format))
                    return temp;
            }
            return Default;
        }
        bool CheckSQLDateValid(DateTime check, string file, string format)
        {
            if (check.CompareTo(new DateTime(1770, 1, 1, 0, 0, 0, 0)) <= 0 || check.CompareTo(new DateTime(9000, 12, 30)) >= 0)            
                return false;            
            return true;
        }
        #endregion
        #region Batch Redistribution
        public void DistributeBatch(Batch nonSpecifiedThreadBatch)
        {
            var leastWork = (from o in MyOperators
                             where o.MyType == OperatorType.Execution
                             select o).OrderBy(o => o.Workload).FirstOrDefault();
            if (leastWork != null)
                leastWork.AddBatch(nonSpecifiedThreadBatch);            
        }
        public void DistributeBatches(IEnumerable<Batch> noThreadBatches)
        {
            noThreadBatches.Where(b => b.ThreadID == null || b.ThreadID >= ExecutorCount ).ForEach(b => DistributeBatch(b));
            noThreadBatches.Where(b => b.ThreadID != null && b.ThreadID < ExecutorCount).ForEach(b =>
            {
                var o = GetOperator(OperatorType.Execution, b.ThreadID.Value);
                o.AddBatch(b);
            });
        }


        int _Maintenance = -1;
        int _Executor = -1;
        public int MaintenanceCount
        {
            get
            {
                if(_Maintenance < 0)
                    _Maintenance = (from o in MyOperators
                        where o.MyType == OperatorType.Maintenance
                        select o).Count();
                return _Maintenance;
            }
        }
        public int ExecutorCount
        {
            get
            {
                if(_Executor < 0)
                    _Executor = (from o in MyOperators
                        where o.MyType == OperatorType.Execution
                        select o).Count();
                return _Executor;
            }
        }
        public int WorkingCount
        {
            get
            {
                return (from o in MyOperators
                        where o.Working
                        select o).Count();
            }
        }
        public ManualResetEvent PauseEvent
        {
            get
            {
                return _mre;
            }
        }
        byte _BatchSize = 1;
        public byte BatchSize
        {
            get
            {
                return _BatchSize;
            }
        }

        public byte QueueLimitMargin
        {
            get
            {
                return 4;
            }
        }
        #endregion
    }
}
