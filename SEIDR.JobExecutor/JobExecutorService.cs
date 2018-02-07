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
        public bool CheckSingleThreadedJobThread(JobExecutionDetail job, int checkID)
        {            
            return(jobList.UnderMaximumCount(executor =>
            {
                if (executor.ThreadID == checkID)
                    return false;
                if (executor.ThreadName == job.JobThreadName)
                    return true;
                return false;
            }, 1)); //True if none of the other executors have the threadName.            
        }        
        public void QueueExecution(JobExecutionDetail newJob)
        {
            if (newJob == null)
                return;
            JobExecutor.Queue(newJob);
        }
        List<JobExecutor> jobList;
        List<Executor> executorList;        
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
        public byte QueueThreadCount = 2;
        public byte ExecutionThreadCount = 4; //Default        
        public int BatchSize = 5;
        public ServiceStatus MyStatus { get; private set; } = new ServiceStatus();        
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

            temp = appSettings["BatchSize"];
            if (!int.TryParse(appSettings["BatchSize"], out BatchSize) || BatchSize < 5)
                BatchSize = 5;

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
        

        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                JobExecutorService om = new JobExecutorService();
                om.Run();
                while (om.ServiceAlive)
                {

                }
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
            JobExecutor.PopulateStatusList(DataManager);
            LogFileMessage("Job Library configured");
            #region Executor Set up            
            executorList = new List<Executor>();
            jobList = new List<JobExecutor>();
            for (byte i = 1; i <= ExecutionThreadCount; i++)
            {
                var je = new JobExecutor(this, DataManager);
                executorList.Add(je);
                jobList.Add(je);                
                //MyOperators.Add(new OperationExecutor(this, i));
            }            
            for (byte i = 1; i <= QueueThreadCount; i++)
            {
                executorList.Add(new Queue(this, DataManager));
                //MyOperators.Add(new Queue(this, i));
            }
            //executorList.Add(new ReDistributor(DataManager, this, jobList));
            executorList.Add(new CancellationExecutor(this, DataManager, jobList));
            executorList.Add(new ScheduleChecker(this, DataManager));
            executorList.Add(new ResetDelayExecutor(this, DataManager));
            //MyOperators.Add(new Queue(this, QUEUE_ID));



            MyStatus.BatchSize = BatchSize;
            MyStatus.JobExecutorCount = Executor.JobExecutorCount;
            MyStatus.QueueExecutorCount = QueueThreadCount;
            MyStatus.MaintenanceCount = Executor.MaintenanceCount;

            DataManager.ExecuteNonQuery("SEIDR.usp_JobExecution_CleanWorking");            

            _ServiceAlive = true;
            
            
            executorList.ForEach(e => e.Call());
            LogFileMessage("START UP DONE, EXECUTORS STARTED");
            #endregion
            bool StatusFileLogged = false;
            while (ServiceAlive)
            {
                _mre.WaitOne();
                try
                {
                    JobExecutor.CheckLibrary(DataManager);

                    int minute = DateTime.Now.Minute;
                    int second = DateTime.Now.Second;
                    if (minute % 5 == 0)
                    {
                        if (!StatusFileLogged)
                        {
                            LogFileMessage(GetOverallStatus(false));
                            StatusFileLogged = true;                     
                        }

                        //if (LibMaintenance.Status.In(TaskStatus.RanToCompletion, TaskStatus.Created, TaskStatus.Canceled))
                        //    LibMaintenance.Start();
                    }
                    else
                        StatusFileLogged = false;

                    if (second % 30 == 0)
                        MyStatus.WriteToFile(LogDirectory);
                }
                finally
                {
                    if(ServiceAlive)
                        Thread.Sleep(5 * MILISECOND_TO_SECOND);
                    //Don't use the daily folder for current status XML file.   
                    //Maybe to do: Delete old log directories? Not something I'd consider especially important, though..
                }
            }            
        }       
        const int MILISECOND_TO_SECOND = 1000;
   
        #region Service control logic
        ManualResetEvent _mre = new ManualResetEvent(true);
        volatile bool _ServiceAlive;
        public bool ServiceAlive
        {
            get
            {
                return _ServiceAlive;
            }
        }
        public ManualResetEvent PauseEvent
        {
            get
            {
                return _mre;
            }
        }

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
        #endregion
        public int WorkingCount
        {
            get
            {
                return executorList.Count(j=> j.IsWorking);
            }
        }
        public int ExecutorCount => jobList.Count;            

        DataBase.DatabaseManager _MGR;
        public DataBase.DatabaseManager DataManager { get { return _MGR; } }
        
        #region Logging
        Mailer _Mailer; //Send success/failure notifications for Executors.

        public bool LogExecutionStartFinish(Executor caller, JobExecutionDetail je, bool start)
            => LogToFile(caller, je, start? "START" : $"FINISH. Duration: {je.ExecutionTimeSeconds.ToString()} seconds.", true);
        
        public bool LogExecutionError(JobExecution execution, string Message, int? ExtraID, int? CallerThread = null)
        {
            const string sproc = "SEIDR.usp_JobExecutionError_i";            
            var m = new
            {
                execution.JobExecutionID,
                ErrorDescription = Message,
                ExtraID = ExtraID,                
                ThreadID = CallerThread
            };
            try
            {
                _MGR.ExecuteNonQuery(sproc, m, RetryDeadlock: true);
            }
            catch
            {
                return false;
            }
            return true;
        }        
        public bool LogExecutionError(Executor caller, JobExecution errBatch, string Message, int? ExtraID)
        {
            caller.Status.SetStatus(Message, ThreadStatus.StatusType.Error);
            bool a = LogExecutionError(errBatch, Message, ExtraID, caller.ThreadID);
            bool b = LogToFile(caller, errBatch, (ExtraID.HasValue? ExtraID.Value + "::":"") + Message + Environment.NewLine);
            return a && b;
        }
        public bool LogToFile(Executor callingOperator, JobExecution b, string Message, bool shared = false)
        {
            string tempMessage = CurrentTimeMessage;
            if (b != null)
                tempMessage += $"JobProfile {b.JobProfileID}, JobExecutionID: [{b.JobExecutionID}], Step: {b.StepNumber}, BatchDate: [{b.ProcessingDate.ToString("MM dd yyyy")}] ";
            tempMessage += Message;
            string File = Path.Combine(DailyLogDirectory, string.Format(LOG_FILE_FORMAT, callingOperator.LogName));
            try
            {
                System.IO.File.AppendAllText(File, tempMessage);
            }
            catch
            {                
                callingOperator.Status.SetStatus("Could not log to File!", ThreadStatus.StatusType.Error);                
                return false;
            }
            if (shared)
            {
                File = Path.Combine(DailyLogDirectory, SHARED_LOG_FILE_FORMAT);
                try
                {
                    System.IO.File.AppendAllText(File, $"[{callingOperator.LogName}] {tempMessage}");
                }
                catch
                {
                    callingOperator.Status.SetStatus("Could not log to File!", ThreadStatus.StatusType.Error);
                    return false;
                }
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
        public bool LogToFile(Executor caller, string Message, bool shared)
        {
            return LogToFile(caller, null, Message, shared);
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
        const string LOG_FILE_FORMAT = "SEIDR.{0}.txt";
        const string SHARED_LOG_FILE_FORMAT = "SEIDR.txt";

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
                PARA_END + PARA +
                "Execution Operator Count: " + ExecutorCount +
                PARA_END + PARA +
                "Queue Count: " + QueueThreadCount +
                PARA_END + BREAK

                ;
            //var orderedOperators = MyOperators.OrderBy(o => ((int)o.MyType * 1000) + o.ID);
            //foreach(Operator o in orderedOperators)
            var orderedJobThreads = executorList.OrderBy(e => (int)e.ExecutorType * 1000 + e.ThreadID);
            foreach (var ex in orderedJobThreads)
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

        #endregion

        public byte QueueLimitMargin
        {
            get
            {
                return 4;
            }
        }
    }
}
