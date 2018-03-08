using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SEIDR.JobBase.Status
{
    public enum StatusType
    {
        Error,
        Start,
        Finish,
        Sleep,
        Sleep_JobRequest,
        General,
        Unknown
    }
    public class ThreadStatus
    { 
        public StatusType MyStatus { get; set; } = StatusType.Unknown;
        public DateTime startTime { get; set; } = DateTime.Now;
        public ThreadInfo ID { get; set; }
        public ThreadStatus(string Name, string threadType, int id)
        {
            //this.Name = Name;
            //ThreadType = threadType;
            //ID = id;
            ID = new ThreadInfo(Name, threadType, id);
        }
        public ThreadStatus() { }
        public ThreadStatus(ThreadInfo id)
        {
            ID = id;
        }
        public DateTime? LastError { get; set; } = null;
        public DateTime? LastStatus { get; set; } = null;
        public string LastErrorMessage { get; set; } = null;
        public string LastStatusMessage { get; set; } = null;
        public DateTime? GetMostRecentStatusTime(out bool oldStatus)
        {
            oldStatus = false;
            if (LastStatus == null)
                return null;
            if (DateTime.UtcNow.AddMinutes(-20) >= LastStatus)
                oldStatus = true;
            return LastStatus.Value.ToLocalTime();
        }
        public void SetStatus(string Message, StatusType MessageStatusType = StatusType.General)
        {
            DateTime now = DateTime.UtcNow;
            LastStatusMessage = Message;
            LastStatus = now;
            MyStatus = MessageStatusType;
            switch (MessageStatusType)
            {
                case StatusType.Error:
                    {
                        LastError = now;
                        LastErrorMessage = Message;
                        break;
                    }
                default:
                    break;
            }
        }
        public static DataTable GetStatusSet(IEnumerable<ThreadStatus> statusList)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Thread", typeof(ThreadInfo));
            dt.Columns.Add("LastStatus", typeof(string));
            dt.Columns.Add("LastStatusTime", typeof(DateTime?));
            dt.Columns.Add("LastError", typeof(string));
            dt.Columns.Add("LastErrorTime", typeof(DateTime?));
            foreach(var stat in statusList)
            {
                dt.Rows.Add(stat.ID, stat.LastStatusMessage, stat.LastStatus, stat.LastErrorMessage, stat.LastError);
            }
            return dt;
        }
    }
}
