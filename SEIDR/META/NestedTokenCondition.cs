using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.META
{
    public class NestedTokenCondition
    {
        public string NestToken { get; set; }
        public string UnNestToken { get; set; }
        public NestDirection Match(string Token)
        {
            if (Token == NestToken)
                return NestDirection.Nest;
            else if (Token == UnNestToken)
                return NestDirection.UnNest;
            return NestDirection.None;
        }
        public enum NestDirection
        {
            None,
            Nest,
            UnNest
        }
    }
    
}
