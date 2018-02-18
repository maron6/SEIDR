using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.Dynamics.Configurations;
using System.ComponentModel;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using SEIDR.Dynamics.Windows;
using SEIDR.Dynamics;


namespace SEIDR.WindowMonitor
{
    public class MiscSetting
    {
        public const string APP_NAME = "SEIDR.Window";
        public const string SETTING_SUBFOLDER = "Settings";        
        public const string FileName = "MiscSettings.xml";
        [EditableObjectInfo(0), DefaultValue(120), 
            Description("Default timeout for DBConnections when setting up a new connection")]
        public int DefaultQueryTimeout { get; set; } = 120;

        ///<summary>
        /// Should update when deploying to match your network location.
        /// <para>Update: Set by meta data.</para>
        ///</summary>
        [EditableObjectInfo(canUpdate: false)]
        public string NetworkFolder { get; set; } = null; // ConfigFolder.GetSafePath(APP_NAME, @"\Fake_Network\");

        [DefaultValue(BasicUserSessionManager.DEFAULT_LOGOUT_TIME_MINUTES), 
            EditableObjectInfo(10, 3 * BasicUserSessionManager.DEFAULT_LOGOUT_TIME_MINUTES)]
        public int LogoutTime { get; set; } = BasicUserSessionManager.DEFAULT_LOGOUT_TIME_MINUTES;

        [EditableObjectInfo(canUpdate:false)]
        public string ErrorLog { get; set; } = "Error_<YYYY><MM><DD>.Log";
        [DefaultValue(500), EditableObjectInfo(75), Description("Splits Query results into 'pages' for performance.")]
        public uint DataTablePageSize { get; set; } = 500;
        int _FileRefresh = 7;


        //[DefaultValue(false)]
        //public bool SkipLogin { get; set; } = false;
        ExceptionLevel _ExceptionAlert = ExceptionLevel.UI;        
        [DefaultValue(ExceptionLevel.UI), Description("Minimum exception level for displaying an alert when an Exception is logged")]
        public ExceptionLevel MyExceptionAlertLevel
        {
            get { return _ExceptionAlert; }
            set
            {
                //sExceptionManager.alertLevel = value;
                _ExceptionAlert = value;
                BasicSessionWindow.SessionManager.Broker.MyExceptionManager.alertLevel = value;
            }
        }        
        [EditableObjectInfo(7)]
        public int FileRefresh
        {
            get { return _FileRefresh; }
            set {
                if (value < 7)
                    _FileRefresh = 7;
                else
                    _FileRefresh = value;

            }
        }
        //[DefaultValue(UserAccessMode.Team)]
        //public UserAccessMode MyAccessMode { get; set; } = UserAccessMode.Team;
        public static MiscSetting LoadFromFile()
        {
            var m = ConfigFolder.DeSerializeFile<MiscSetting>(
                ConfigFolder.GetSafePath(APP_NAME, 
                    SETTING_SUBFOLDER, 
                    FileName));
            Models.ContextActionQueue.QueueLimit = m.QueueLimit;
            Models.ContextActionQueue.BatchSize = m.QueueBatchSize;
            return m;
        }
        [DefaultValue(12), 
            EditableObjectInfo(2, 40), 
            Description("Limits the number of records that can have a query run via multi-select without queue-ing")]
        public int? MultiSelectContextSprocLimit { get; set; } = 8;
        public void Save()
        {
            this.SerializeToFile(ConfigFolder.GetSafePath(APP_NAME, SETTING_SUBFOLDER, FileName));
        }
        [DefaultValue(5), EditableObjectInfo(1, 10), Description("Max Number of queued actions to perform per Batch")]
        public int QueueBatchSize { get; set; } = 5;
        [DefaultValue(16), EditableObjectInfo(16, Models.ContextActionQueue.MaxQueueSize), Description("Limit for items in the queue.")]
        public int QueueLimit { get; set; } = 16;
    }
}
