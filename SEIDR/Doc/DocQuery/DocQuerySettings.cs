using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    public class DocQuerySettings
    {
        /// <summary>
        /// If true, will throw an exception when failing to parse a non varchar.
        /// <para>Otherwise, will treat as null.</para>
        /// </summary>
        public bool ExceptionOnParseFailure { get; set; } = false;
        /// <summary>
        /// Sets value for ExceptionOnParseFailure
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public DocQuerySettings SetExceptionOnParseFailure(bool exception = true)
        {
            ExceptionOnParseFailure = exception;
            return this;
        }
    }
}
