using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    public class TransformedColumnMetaDataCollection
    {
        /// <summary>
        /// Number of meta data items in the collection
        /// </summary>
        public int Count => _collection.Count;
        List<TransformedColumnMetaData> _collection;
        /// <summary>
        /// Gets the Meta Data for the specified column, or null if not exists
        /// </summary>
        /// <param name="alias"></param>        
        /// <param name="Column"></param>
        /// <returns></returns>
        public TransformedColumnMetaData this [string alias, string Column]
            => _collection
                .Where(c => (c.OwnerAlias == alias || alias == null) && c.ColumnName == Column)
                .FirstOrDefault();
        /// <summary>
        /// Gets the column meta data based on Ordinal position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public TransformedColumnMetaData this[int position]=> _collection[position];
        /// <summary>
        /// Adds the meta data to the collection, if the column isn't already in use.
        /// </summary>
        /// <param name="newMetaData"></param>
        /// <returns></returns>
        public bool AddMetaData(TransformedColumnMetaData newMetaData)
        {
            if(!_collection.Exists(m => m.ColumnName == newMetaData.ColumnName 
                && m.OwnerAlias == newMetaData.OwnerAlias))
            {
                _collection.Add(newMetaData);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Removes any meta data for the column name
        /// </summary>
        /// <param name="oldMetaDataColumn"></param>
        /// <param name="OwnerAlias">Specifies the file owner for the column</param>
        /// <returns></returns>
        public bool RemoveMetaData(string oldMetaDataColumn, string OwnerAlias)
        {
            if (!_collection.Exists(m => m.ColumnName == oldMetaDataColumn && m.OwnerAlias == OwnerAlias))
                return false;
            _collection = _collection.Where(m => m.ColumnName != oldMetaDataColumn && m.OwnerAlias == OwnerAlias).ToList();
            return true;
        }
        /// <summary>
        /// Replaces any existing meta data for the parameter's Column with newMetaData
        /// </summary>
        /// <param name="newMetaData"></param>
        public void UpdateMetaData(TransformedColumnMetaData newMetaData)
        {
            RemoveMetaData(newMetaData.ColumnName, newMetaData.OwnerAlias);
            AddMetaData(newMetaData);
        }
        public TransformedColumnMetaDataCollection()
        {
            _collection = new List<TransformedColumnMetaData>();
        }
        public TransformedColumnMetaDataCollection(params TransformedColumnMetaData[] metaData)
        {
            _collection = new List<TransformedColumnMetaData>(metaData);
        }

        /// <summary>
        /// Gets a TransformedColumn for the specified column name using any meta data
        /// <para>for the column.</para>
        /// <para>If no meta data is found, it will be treated as null</para>
        /// </summary>
        /// <param name="alias">File aliasfrom querying</param>
        /// <param name="ColumnName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public TransformedColumn GetColumn(string alias, string ColumnName, IRecord content)
        {
            var md = this[alias, ColumnName];
            if (md == null)
                return new TransformedColumn(null, null);
            return md.GetColumn(content);
        }        
    }

    /// <summary>
    /// Contains information about a column for usage in a Delimited Query's conditions and/or joining
    /// </summary>
    public class TransformedColumnMetaData
    {
        /// <summary>
        /// Name of the column for use in a MetadataCollection
        /// </summary>
        public string ColumnName;
        /// <summary>
        /// Alias of the document containing the column meta data..
        /// </summary>
        public string OwnerAlias;
        /// <summary>
        /// Return true if this is referencing the same content, based on the alias and and column name
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool Matches(DocRecordColumnInfo column)
        {
            return column.OwnerAlias == OwnerAlias && column.ColumnName == ColumnName;
        }        
        /// <summary>
        /// Implicitly convert to Column info
        /// </summary>
        /// <param name="a"></param>
        public static implicit operator DocRecordColumnInfo(TransformedColumnMetaData a)
        {
            return new DocRecordColumnInfo(a.ColumnName, a.OwnerAlias, -1);
        }
        /// <summary>
        /// Used for type validation before performing any comparisons. 
        /// Null is allowed by default
        /// </summary>
        public DataType Type;
        /// <summary>
        /// Transforms the content 
        /// </summary>
        public Func<string, TransformedData> Transform
            = TransformedVarchar.BasicTransform;
        /*
        public TransformedColumn GetColumn(DelimitedRecord row)
        {
            if (!row.ContainsHeader(OwnerAlias, ColumnName))
                throw new InvalidOperationException($"Column '{ColumnName}' was not found for the record from [{OwnerAlias}]!");
            string x = row[OwnerAlias, ColumnName];
            TransformedColumn c = new TransformedColumn(this, Transform(x));
            return c;
        }*/
        public TransformedColumn GetColumn(IRecord row)
        {
            if(!row.HasColumn(OwnerAlias, ColumnName))
                throw new InvalidOperationException($"Column '{ColumnName}' was not found for the record from [{OwnerAlias}]!");
            string x = row[OwnerAlias, ColumnName];
            return new TransformedColumn(this, Transform(x));
        }
    }
}
