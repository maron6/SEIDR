using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{

    /// <summary>
    /// Handling for duplicates when creating the index file.
    /// </summary>
    public enum DuplicateHandling
    {
        /// <summary>
        /// Do nothing when a duplicate appears
        /// </summary>
        Ignore,
        /// <summary>
        /// Remove duplicates
        /// </summary>
        Delete,
        /// <summary>
        /// Throw an exception if a duplicate is found.
        /// </summary>
        Exception
    }
    /// <summary>
    /// Thrown when an instance of <see cref="DocSorter{G}"/> finds a duplicate when creating an index with mode <see cref="DuplicateHandling.Exception"/> 
    /// </summary>
    public class DuplicateRecordException: Exception
    {
        /// <summary>
        /// Thrown when an instance of <see cref="DocSorter{G}"/> finds a duplicate when creating an index with mode <see cref="DuplicateHandling.Exception"/> 
        /// </summary>
        /// <param name="message"></param>
        public DuplicateRecordException(string message) : base(message)
        {

        }
    }
}
