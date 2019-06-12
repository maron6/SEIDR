using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public interface ISingleRecordTypeMetaData
    {
        DocRecordColumnCollection Columns { get; }
        bool FixedWidthMode { get; }
        bool HasHeader { get; }
        int SkipLines { get; }
        bool Valid { get; }
        string FilePath { get; }
        string LineEndDelimiter { get; }
        Encoding FileEncoding { get; }
        char? Delimiter { get; }
    }
}
