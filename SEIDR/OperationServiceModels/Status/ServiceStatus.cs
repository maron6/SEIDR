using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using SEIDR.Doc;

namespace SEIDR.OperationServiceModels.Status
{
    [XmlRoot]
    public class ServiceStatus
    {
        public void WriteToFile(string Directory)
        {
            string f = Path.Combine(Directory, FILE_NAME);
            using(StreamWriter sw = new StreamWriter(f))            
            {
                XmlSerializer x = new XmlSerializer(typeof(ServiceStatus));
                x.Serialize(sw, this);
            }
        }
        [XmlIgnore]
        public const string FILE_NAME = "SEIDR.STATUS.XML";
        public List<ThreadStatus> StatusList { get; set; }
        [XmlIgnore]
        public ThreadStatus this[ThreadInfo id]
        {
            get
            {
                foreach (ThreadStatus s in StatusList)
                {
                    if (s.ID == id)
                    {
                        return s;
                    }
                }
                return null;
            }
        }
        public bool Contains(ThreadInfo id)
        {
            return this[id] != null;
        }
        public bool Add(ThreadStatus ts)
        {
            if (this[ts.ID] == null)
                StatusList.Add(ts);
            else
                return false;
            return true;
        }
        public bool Remove(ThreadInfo id)
        {
            foreach (var s in StatusList)
            {
                if (s.ID == id)
                {
                    StatusList.Remove(s);
                    return true;
                }
            }
            return false;
        }
        public ServiceStatus()
        {
            StatusList = new List<ThreadStatus>();
        }
        public DataTable GetStatuses(string Error, string ErrorLate, string Start, string StartLate, string Finish, string FinishLate,
            string Sleep, string SleepLate)
        {
            DataTable dt = new DataTable();
            Action<string, Type, bool> AddColS = (string name, Type t, bool allowNull) =>
            { dt.Columns.Add(new DataColumn { ColumnName = name, DataType = t, AllowDBNull = allowNull }); };
            Action<string, Type> AddCol = (string name, Type t) => AddColS(name, t, false);
            AddCol("ThreadType", typeof(string));
            AddCol("ThreadName", typeof(string));
            AddCol("LastStatusType", typeof(string));
            AddCol("LastStatusMessage", typeof(string));
            AddColS("LastStatusTime", typeof(DateTime), true);
            AddCol("Color", typeof(string));
            AddCol("StartupTime", typeof(DateTime));
            AddCol("LastError", typeof(string));
            foreach (ThreadStatus st in StatusList)
            {
                DataRow r = dt.NewRow();
                r["ThreadType"] = st.ID.Type;
                r["ThreadName"] = st.ID.Name;
                r["LastStatusType"] = st.MyStatus.ToString();
                bool oldStatus;
                r["LastStatusTime"] = st.GetMostRecentStatusTime(out oldStatus); //If last status is not null, converts to local time as well
                r["LastStatusMessage"] = st.LastStatusMessage;
                r["StartupTime"] = st.startTime;
                string color = null;
                switch (st.MyStatus)
                {
                    case ThreadStatus.StatusType.Error:
                        {
                            if (oldStatus)
                                color = ErrorLate;
                            else
                                color = Error;
                            break;
                        }
                    case ThreadStatus.StatusType.Finish:
                        {
                            if (oldStatus)
                                color = FinishLate;
                            else
                                color = Finish;
                            break;
                        }
                    case ThreadStatus.StatusType.Start:
                        {
                            if (oldStatus)
                                color = StartLate;
                            else
                                color = Start;
                            break;
                        }
                    case ThreadStatus.StatusType.Sleep:
                        {
                            if (oldStatus)
                                color = SleepLate;
                            else
                                color = Sleep;
                            break;
                        }
                }
                if (st.MyStatus == ThreadStatus.StatusType.Unknown && DateTime.Now.AddMinutes(-10) >= st.LastStatus)
                    color = "LightGrey";
                r["Color"] = color;
                r["LastError"] = st.LastErrorMessage;
                dt.Rows.Add(r);
            }
            return dt;
        }

    }
}
