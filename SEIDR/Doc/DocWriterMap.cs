using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{        
    /// <summary>
    /// Helper class for mapping data from one column collection to another for a DocWriter
    /// </summary>
    public class DocWriterMap
    {
        /// <summary>
        /// Underlying column mapping dictionary
        /// </summary>
        public readonly Dictionary<int, DocRecordColumnInfo> MapData;
        readonly DocRecordColumnCollection _mapTo;
        readonly DocRecordColumnCollection _mapFrom;
        /// <summary>
        /// Helper class for mapping data from one column collection to another for a DocWriter
        /// </summary>
        /// <param name="mapTo"></param>
        /// <param name="mapFrom"></param>
        public DocWriterMap(DocWriter mapTo, DocRecordColumnCollection mapFrom)
        {
            MapData = new Dictionary<int, DocRecordColumnInfo>();
            _mapFrom = mapFrom;
            _mapTo = mapTo;
        }
        /// <summary>
        /// Adds a column mapping to the undedrlying dictionary
        /// </summary>
        /// <param name="fromColumnPosition"></param>
        /// <param name="toColumnPosition"></param>
        /// <returns></returns>
        public DocWriterMap AddMapping(int fromColumnPosition, int toColumnPosition)
        {
            MapData.Add(toColumnPosition, _mapFrom[fromColumnPosition]);
            return this;
        }
        /// <summary>
        /// Adds a new mapping to the underlying dictionary
        /// </summary>
        /// <param name="from"></param>
        /// <param name="toColumnPosition"></param>
        /// <returns></returns>
        public DocWriterMap AddMapping(DocRecordColumnInfo from, int toColumnPosition)
        {
            MapData.Add(toColumnPosition, from);
            return this;
        }
        /// <summary>
        /// Sets a column mapping in the underlying dictionary
        /// </summary>
        /// <param name="fromColumnPosition"></param>
        /// <param name="toColumnPosition"></param>
        /// <returns></returns>
        public DocWriterMap SetMapping(int fromColumnPosition, int toColumnPosition)
        {
            MapData[toColumnPosition] = _mapFrom[fromColumnPosition];
            return this;
        }
        /// <summary>
        /// Sets a column mapping in the underlying dictionary
        /// </summary>
        /// <param name="from"></param>
        /// <param name="toColumnPosition"></param>
        /// <returns></returns>
        public DocWriterMap SetMapping(DocRecordColumnInfo from, int toColumnPosition)
        {
            MapData[toColumnPosition] = from;
            return this;
        }
        /// <summary>
        /// Adds a Column mapping to the underlying dictionary
        /// </summary>
        /// <param name="fromColumnName"></param>
        /// <param name="ToColumnName"></param>
        /// <param name="fromAlias"></param>
        /// <param name="toAlias"></param>
        /// <returns></returns>
        public DocWriterMap AddMapping(string fromColumnName, string ToColumnName, string fromAlias = null, string toAlias = null)
        {
            MapData.Add(_mapTo.GetBestMatch(ToColumnName, toAlias), _mapFrom.GetBestMatch(fromColumnName, fromAlias));
            return this;
        }
        /// <summary>
        /// Sets a mapping in the underlying dictionary
        /// </summary>
        /// <param name="fromColumnName"></param>
        /// <param name="ToColumnName"></param>
        /// <param name="fromAlias"></param>
        /// <param name="toAlias"></param>
        /// <returns></returns>
        public DocWriterMap SetMapping(string fromColumnName, string ToColumnName, string fromAlias = null, string toAlias = null)
        {
            MapData[_mapTo.GetBestMatch(ToColumnName, toAlias)] = _mapFrom.GetBestMatch(fromColumnName, fromAlias);
            return this;
        }
    }
}
