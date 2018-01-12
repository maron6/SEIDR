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
using SEIDR.JobBase;
using SEIDR.JobBase.Status;

namespace SEIDR.JobExecutor
{
    [Export(typeof(IOperatorManager))]
    public class JobExecutorService : ServiceBase//, IOperatorManager
    {
        /// <summary>
        /// Called if job meta data indicates single threaded. Makes sure there isn't another thread running the job.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="checkID"></param>
        /// <returns></returns>
        public bool CheckSingleThreadedJobThread(JobExecution job, int checkID)
        {
            byte? reqThread = job.RequiredThreadID;
            if (string.IsNullOrWhiteSpace(job.JobThreadName))
            {
                reqThread = 1;
                if (checkID == 1)
                    return true;
            }
            var exec = (from ex in executorList
                     where ex.ExecutorType == ExecutorType.Job
                     && ex.ThreadID != checkID
                     && job.JobThreadName == ex.ThreadName                     
                     && (reqThread == null || reqThread == ex.ThreadID)
                     select ex as JobExecutor).FirstOrDefault();
            if (exec != null)
            {
                if (job.RequiredThreadID == null)
                    exec.Queue(job, true);
                return false;
            }
            return true;
        }        
        public void QueueExecution(JobExecution newJob)
        {
            if (newJob == null)
                return;
            JobExecutor jobExecutor;
            if (newJob.RequiredThreadID != null)
            {
                jobExecutor = (from ex in executorList
                                where ex.ExecutorType == ExecutorType.Job
                                && ex.ThreadID % newJob.RequiredThreadID == 0
                                && ex.ThreadID <= newJob.RequiredThreadID
                                orderby ex.ThreadID
                                select ex as JobExecutor
                                ).LastOrDefault();
                jobExecutor.Queue(newJob);
                return;
            }
            if (!string.IsNullOrWhiteSpace(newJob.JobThreadName))
            {
                jobExecutor = (from ex in executorList
                                where ex.ExecutorType == ExecutorType.Job
                                && ex.ThreadName == newJob.JobThreadName
                                orderby ex.Workload
                                select ex as JobExecutor
                                ).FirstOrDefault();
                if(jobExecutor != null)
                {
                    jobExecutor.Queue(newJob);
                    return;
                }
            }
            jobExecutor = (from ex in executorList
                            where ex.ExecutorType == ExecutorType.Job                                       
                            orderby ex.Workload
                            select ex as JobExecutor
                                ).First();
            jobExecutor.Queue(newJob);
        }
        List<Executor> executorList;        
        const string CLEAN_LOCKS = "SEIDR.usp_Batch_CleanLocks";
        public JobExecutorService()
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
        public const byte REDIST_ID = 202;
        //public const int QUEUE_ID = 1;
        public byte QueueThreadCount = 1;
        public byte ExecutionThreadCount = 4; //Default        
        public ServiceStatus MyStatus { get; private set; } = new ServiceStatus();
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
                )
            {
                Timeout = tempInt,
                CommandTimeout = tempInt * 3
            };
            _MGR = new DataBase.DatabaseManager(db, "SEIDR") { RethrowException = false, ProgramName = "SEIDR.JobExecutor"};
            //OperationExecutor.ExecutionManager = _MGR.Clone(reThrowException: true, programName: "SEIDR.JobExecutor Query");
            

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
                JobExecutor.ConfigureLibrary(appSettings["JobLibrary"]);
                //OperationExecutor.SetLibrary(appSettings["JobLibrary"]);
            }
            catch(Exception ex)
            {
                LogFileMessage("Library set up error - " + ex.Message);
                throw;
            }
            Mailer.SMTPServer = appSettings["SmtpServer"];
            Mailer.Domain = appSettings["MailDomain"];

            _Mailer = new Mailer("SEIDR.OperatorManager", appSettings["StatusMailTo"]);
            //OperationExecutor.ExecutionAlerts = new Mailer("SEIDR.Operator");

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
            /*
            temp = appSettings["BatchSize"];
            if (!byte.TryParse(temp, out _BatchSize) || _BatchSize < 1)
                _BatchSize = 1;
            else if (_BatchSize > 10)
                _BatchSize = 10;
            */
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
                JobExecutorService om = new JobExecutorService();
                om.Run();
            }
            else
            {
                Run(new JobExecutorService());
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
            JobExecutor.CheckLibrary(DataManager);
            LogFileMessage("Job Library configured");
            #region Operator Set up
            MyOperators = new List<Operator>();
            executorList = new List<Executor>();
            for (byte i = 1; i <= ExecutionThreadCount; i++)
            {
                executorList.Add(new JobExecutor(i, DataManager, this));
                //MyOperators.Add(new OperationExecutor(this, i));
            }
            for (byte i = 1; i <= QueueThreadCount; i++)
            {
                executorList.Add(new Queue(i, DataManager, this));
                //MyOperators.Add(new Queue(this, i));
            }
            var jeList = (from je in executorList
                          where je is JobExecutor
                          select je as JobExecutor);
            executorList.Add(new ReDistributor(REDIST_ID, DataManager, this, jeList));
            executorList.Add(new CancellationExecutor(CANCEL_ID, this, DataManager, jeList));
            //MyOperators.Add(new Queue(this, QUEUE_ID));
            
            
            
            //MyOperators.Add(new CancellationExecutor(this, CANCEL_ID));

            _ServiceAlive = true;
            #endregion

            //DataManager.Execute(CLEAN_LOCKS); //clean up locks before starting up.
            //MyOperators.ForEach(o => o.Call());
            executorList.ForEach(e => e.Call());
            LogFileMessage("START UP DONE, OPERATORS STARTED");

            //bool EmailSent = false;
            bool StatusFileLogged = false;
            /*
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
            */
            while (ServiceAlive)
            {
                _mre.WaitOne();
                     
                int minute = DateTime.Now.Minute;
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

                    //if (LibMaintenance.Status.In(TaskStatus.RanToCompletion, TaskStatus.Created, TaskStatus.Canceled))
                    //    LibMaintenance.Start();
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
            //var orderedOperators = MyOperators.OrderBy(o => ((int)o.MyType * 1000) + o.ID);
            //foreach(Operator o in orderedOperators)
            var orderedJobThreads = executorList.OrderBy(e => (int)e.ExecutorType * 1000 + e.ThreadID);
            foreach(var ex in orderedJobThreads)
            {
                Message += PARA + ex.ThreadName + $"({ex.LogName})" + PARA_END + PARA
                    + ex.Status.LastStatusMessage 
                    + (ex.Status.LastStatus.HasValue 
                        ? (" - " + ex.Status.LastStatus.Value.ToString("MMM dd, yyyy hh:mm"))
                        : string.Empty)
                    + PARA_END + PARA + "Working ? " + ex.IsWorking.ToString() 
                    + "     Work Load Size: " + ex.Workload 
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
        public bool LogExecutionError(JobExecution execution, string Message, string ExtraInfo, int? CallerThread = null)
        {
            const string sproc = "SEIDR.usp_JobExecution_Error_i";
            var m = new
            {
                execution.JobExecutionID,
                Message,
                Extra = ExtraInfo,                
                Job = execution.JobNameSpace + "." + execution.JobName
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
        public bool LogExecutionError(Executor caller, JobExecution errBatch, string Message, string Extra)
        {
            caller.Status.SetStatus(Message, ThreadStatus.StatusType.Error);
            bool a = LogExecutionError(errBatch, Message, Extra, caller.ThreadID);
            bool b = LogFileError(caller, errBatch, Message + Environment.NewLine + Extra);
            return a && b;
        }
        public bool LogFileError(Executor callingOperator, JobExecution b, string Message)
        {
            string tempMessage = CurrentTimeMessage;
            if (b != null)
                tempMessage += $"JobProfile {b.JobProfileID}, JobExecutionID: [{b.JobExecutionID}], Step: {b.StepNumber}, BatchDate: [{b.ProcessingDate.ToString("MM dd yyyy")}] ";
            tempMessage += Message;
            string File = Path.Combine(DailyLogDirectory, string.Format(LogFileFormat, callingOperator.LogName));
            try
            {
                System.IO.File.AppendAllText(File, tempMessage);
            }
            catch
            {                
                callingOperator.Status.SetStatus("Could not log error!", ThreadStatus.StatusType.Error);                
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
            string File = Path.Combine(DailyLogDirectory, "SEIDR.JobExecutor.txt");
            try
            {
                System.IO.File.AppendAllText(File, tempMessage);
            }
            catch { }
        }
        public bool LogError(Executor caller, string Message)
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
        string LogFileFormat = "SEIDR.{0}.txt";
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
