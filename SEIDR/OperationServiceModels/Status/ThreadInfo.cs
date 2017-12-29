using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.OperationServiceModels.Status
{
    public struct ThreadInfo
    {
        public string Type { get; private set; }
        public string Name { get; private set; }
        public int ID { get; private set; }
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
    } 
}
