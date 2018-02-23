using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public class ScheduleChecker : Executor
    {
        DateTime lastCheck;
        const string CHECK_SCHEDULES = "SEIDR.usp_JobProfile_CheckSchedule";
        public ScheduleChecker(JobExecutorService caller,
            DataBase.DatabaseManager manager)
            :base(manager, caller, ExecutorType.Maintenance)
        {
            _Manager.DefaultRetryOnDeadlock = true;
        }
        volatile int workLoad = 1;
        public override int Workload => workLoad;
        int nextCheck = 0;
        protected override void CheckWorkLoad()
        {
            if (nextCheck == 0)
                workLoad = 1;
            else if (DateTime.Now.Subtract(lastCheck).TotalSeconds > nextCheck)
                workLoad = 1;    
        }
        
        protected override void Work()
        {           
            workLoad = 0;
            lastCheck = DateTime.Now;
            _Manager.ExecuteNonQuery(CHECK_SCHEDULES, ReturnCode: out nextCheck);            
        }
    }
}
