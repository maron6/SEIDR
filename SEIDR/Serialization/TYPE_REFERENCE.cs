using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Serialization
{
    public class TYPE_REFERENCE
    {
        #region Markers
        const char NULL = 'z';
        const char NO_OP = 'N';
        const char TRUE = 'T';
        const char FALSE = 'F';
        
        //Should probably convert to an int in practice.. byte doesn't have negative..
        const char INT8 = 'i'; 
        const char UINT8 = 'U';
        const char INT16 = 'I';
        const char INT32 = 'l';
        const char INT64 = 'L';

        const char FLOAT32 = 'd';
        const char FLOAT64 = 'D';
        const char HIGH_PRECISION = 'H';

        const char CHAR = 'C';
        const char STRING = 'S';
        #endregion        
        //array - surrounded by [, ]
        //non primitive object - surrounded by {, }
    }
}
