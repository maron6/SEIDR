using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    public class DocQuery
    {
        public DocQuerySettings Settings { get; private set; }
        public List<DocMetaData> Files { get; private set; }
        public DocQuery(DocQuerySettings s)
        {
            Settings = s;
        }
    }
}
