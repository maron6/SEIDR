using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.OperationServiceModels.Status
{
    public class ThreadStatus
    {
        public enum StatusType
        {
            Error,
            Start,
            Finish,
            Sleep,
            General,
            Unknown
        }
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
    }
}
