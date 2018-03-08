using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobBase.Status
{
    public class ThreadInfo
    {
        public string Type { get;  set; }
        public string Name { get;  set; }
        public int ID { get;  set; }
        public ThreadInfo(string Name, string Type, int ID)
        {
            this.Name = Name;
            this.Type = Type;
            this.ID = ID;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(ThreadInfo a, ThreadInfo b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(ThreadInfo a, ThreadInfo b)
        {
            return !a.Equals(b);
        }
        public override string ToString()
        {
            return $"{Name}({Type}, {ID})";
        }
    } 
}
