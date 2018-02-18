using System.Collections.Generic;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using System.Data;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace SEIDR.Dynamics.Configurations
{
    public sealed class DBConnections: iConfigList
    {
        public bool Cloneable { get { return false; } }
        public List<DBConnection> DBConns = new List<DBConnection>();
        public DBConnections()
        {
            Version = new Guid();
        }
        public void Add(DBConnection db)
        {
            if (this[db.Name] != null)
                throw new Exception("Name is already in use.");
            DBConns.Add(db);
        }
        public int GetIndex(DBConnection c)
        {
            return DBConns.IndexOf(c);
        }
        public int GetIndex(string ConnectionName)
        {
            for (int i = 0; i < DBConns.Count; i++)
            {
                if (DBConns[i].Name == ConnectionName)
                    return i;
            }
            return -1;
        }
        public List<string> GetNameList()
        {
            List<string> rl = new List<string>();
            foreach (var q in DBConns)
            {
                rl.Add(q.Name);
            }
            return rl;
        }
        [XmlIgnore]
        public DBConnection this[string idx]
        {
            get
            {
                foreach (var db in DBConns)
                {
                    if (db.Name == idx)
                    {
                        //return db.InternalDBConn;
                        return db;
                    }
                }
                return null;
            }
            set
            {
                for (int x = 0; x < DBConns.Count; x++)
                {
                    var db = DBConns[x];
                    if (db.Name == idx)
                    {
                        DBConns[x]/*.InternalDBConn*/ = value;
                        return;
                    }
                }
                //DBConns.Add(new DBConnection() { Name = idx, InternalDBConn = value });
                DBConns.Add(value);
            }
        }        
        
        public DBConnection Get(string idx)
        {
            foreach (var db in DBConns)
            {
                if (db.Name == idx)
                {
                    return db;
                }
            }
            return null;
        }

        public List<string> ToStringList(bool AddNew = true)
        {
            List<string> ret = new List<string>();
            if (AddNew)
            {
                ret.Add("(New Connection)");
            }
            foreach (var db in DBConns)
            {
                ret.Add(db.Name);
            }
            return ret;
        }
        
        public int GetIndex(string idx, bool IncludeNew = true)
        {
            for (int i = 0; i < DBConns.Count; i++)
            {
                if (DBConns[i].Name == idx)
                {
                    return i + (IncludeNew ? 1 : 0);
                }
            }
            return -1;
        }
        
        public IEnumerator<DBConnection> GetEnumerator()
        {
            return DBConns.GetEnumerator();
        }        
        [XmlIgnore]
        public DataTable MyData
        {
            get
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Server", typeof(string));
                dt.Columns.Add("Catalog", typeof(string));
                foreach (DBConnection db in this.DBConns)
                {
                    dt.Rows.Add(db.Name, db.InternalDBConn.Server, db.InternalDBConn.DefaultCatalog);
                }
                return dt;
            }
        }

        public Guid Version
        {
            get;
            set;
        }
    

        public void Remove(string NameKey)
        {
            for (int k = 0; k < DBConns.Count; k++ )
            {
                var db = DBConns[k];
                if (db.Name == NameKey)
                {
                    DBConns.Remove(db);
                    return;
                }
            }
        }

        public iConfigList cloneSetup()
        {
            DBConnections clone = new DBConnections();
            clone.DBConns = new List<DBConnection>(DBConns);
            return clone;
        }
        public string GetColor(string Name)
        {
            foreach(var db in DBConns)
            {
                if (db.Name == Name)
                {
                    if (db.Color.ToUpper() == "DEFAULT")
                        return null;
                    return db.Color;
                }
            }
            return null;
        }
        public string GetTextColor(string name)
        {
            foreach(var db in DBConns)
            {
                if(db.Name == name)
                {
                    if (db.TextColor.ToUpper() == "DEFAULT")
                        return null;
                    return db.TextColor;
                }
            }
            return null;
        }
    }
    public sealed class DBConnection
    {
        public DatabaseConnection InternalDBConn { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;            
        }
        /// <summary>
        /// Returns the internal db connection's formatted connection string.
        /// </summary>
        [XmlIgnore]
        public string ConnectionString
        {
            get { return InternalDBConn.ConnectionString; }
        }
        [XmlIgnore]
        public string Description
        {
            get
            {
                if (InternalDBConn != null)
                    return Name + ":" + InternalDBConn.Server + "." + InternalDBConn.DefaultCatalog;
                return Name = ":(No Connection setup)";
            }
        }
        [System.ComponentModel.DefaultValue("Default")]
        public string Color { get; set; } = "Default"; //Don't change background colors
        [System.ComponentModel.DefaultValue("Default")]
        public string TextColor { get; set; } = "Default"; //Black.
        public static string[] GetColorList()
        {            
            var temp = (from color in (typeof(System.Windows.Media.Brushes)).GetProperties()
                        where color.Name != "Black"
                        select color.Name).ToArray();
            var temp2 = new List<string>(temp.Length + 1);
            temp2.Add("Default");
            temp2.AddRange(temp);            
            return temp2.ToArray();
        }
    }
}
