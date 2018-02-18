using SEIDR.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SEIDR.Dynamics.Configurations.DatabaseConfiguration
{
    public class Database : iWindowConfiguration
    {
        //Possible ToDo: Provide an overrideable Execute? meh.

        [XmlIgnore]
        public bool Altered { get; set; }

        [WindowConfigurationEditorIgnore]
        public string Description
        { get
            {                            
                if (Connection != null && Connection.IsValid)
                    return Key + ":" + Connection.Server + "." + Connection.DefaultCatalog;
                return Key + ":(No Connection setup)";            
            }
            set { }
        }
        [WindowConfigurationEditorIgnore]
        public int? ID { get; set; }

        public string Key { get; set; }

        //For classes/ non primitives, the configuration editor should add a button for the object editor..
        //Could also just nest it.
        DatabaseConnection _Conn;
        [WindowConfigurationEditorElementInfo("Database Connection", required: true)]
        public DatabaseConnection Connection
        {
            get { return _Conn; }
            set
            {
                _Conn = value;
                if(Key != null && Key.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                    _Conn.ApplicationName = value.ApplicationName ?? "SEIDR CONNECTION:" + Key;
                dbm = null;
            }
        }
        

        [WindowConfigurationEditorElementInfo("Connection Background Color", isColor:true)]
        public string ConnectionColor { get; set; }
        [WindowConfigurationEditorElementInfo("Connection Text Color", isColor:true)]
        public string TextColor { get; set; }

        [WindowConfigurationEditorIgnore]
        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.DB;
            }
        }
        [WindowConfigurationEditorIgnore]
        public int RecordVersion { get; set; }
        [XmlIgnore]
        DatabaseManager dbm = null;
        /// <summary>
        /// Lazy evaluated manager
        /// </summary>
        [XmlIgnore]
        public DatabaseManager Manager
        {
            get
            {
                if (dbm == null)
                    dbm = new DatabaseManager(Connection);
                return dbm;
            }
        }
    }
}
