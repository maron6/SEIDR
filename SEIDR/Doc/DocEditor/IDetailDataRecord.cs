using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocEditor
{
    internal interface IDetailDataRecord: IDataRecord
    {
        void SetID(long ID);
        void ClearID();
    }
}
