using System.Collections.Generic;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using System.Data;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public sealed class DBConnections:IEnumerable<DBConnection>, iConfigList
    {
        public bool Cloneable { get { return false; } }
        public List<DBConnection> DBConns = new List<DBConnection>();
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
        public DatabaseConnection this[string idx]
        {
            get
            {
                foreach (var db in DBConns)
                {
                    if (db.Name == idx)
                    {
                        return db.InternalDBConn;
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
                        DBConns[x].InternalDBConn = value;
                        return;
                    }
                }
                DBConns.Add(new DBConnection() { Name = idx, InternalDBConn = value });
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

        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
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
    }
    public sealed class DBConnection
    {
        public DatabaseConnection InternalDBConn { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;            
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
    }
}
