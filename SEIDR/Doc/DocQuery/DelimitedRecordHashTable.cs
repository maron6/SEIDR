using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    /// <summary>
    /// For eventually performing delimited record hash joins
    /// </summary>
    public class DelimitedRecordHashTable
    {
        /// <summary>
        /// Creates an object for storing delimited records and accessing them based on their partial hash.
        /// </summary>
        /// <param name="Columns"></param>
        public DelimitedRecordHashTable(params string[] Columns)
        {
            this.Columns = Columns;
            if (Columns.UnderMaximumCount(1))
                throw new ArgumentOutOfRangeException(nameof(Columns), "Must have at least one columns specified");
            Content = new Dictionary<ulong, List<DelimitedRecord>>();
        }
        /// <summary>
        /// Creates an object for storing delimited records using an IEnumerable of strings.
        /// </summary>
        /// <param name="Columns"></param>
        public DelimitedRecordHashTable(IEnumerable<string> Columns)
            : this(Columns.ToArray()) { }
        Dictionary<ulong, List<DelimitedRecord>> Content;
        string[] Columns;
        /// <summary>
        /// Returns an enumerable of records that match on the hash
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public IEnumerable<DelimitedRecord> this[DelimitedRecord record]
        {
            get
            {
                List<DelimitedRecord> rv;
                var x = CheckHash(record);
                if (x == null)
                    rv = new List<DelimitedRecord>();
                else if (!Content.TryGetValue(x.Value, out rv))
                    rv = new List<DelimitedRecord>();
                return rv.ToArray();
            }
        }
        /// <summary>
        /// Calculates the hash for this record that would be used by this hash table.
        /// <para>Note that the table only uses records that have a non null value</para>
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public ulong? CheckHash(DelimitedRecord record)
        {
            return record.GetPartialHash(Columns);
        }
        /// <summary>
        /// Returns an enumerable of records that match on the hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public IEnumerable<DelimitedRecord> this[ulong hash]
        {
            get
            {
                List<DelimitedRecord> rv;
                if (!Content.TryGetValue(hash, out rv))
                    rv = new List<DelimitedRecord>();
                return rv.ToArray();
            }
        }
        /// <summary>
        /// Gets an enumerable of records that have the same hash
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public IEnumerable<DelimitedRecord> GetMatchingRecords(DelimitedRecord record)
        {
            var x = CheckHash(record);
            if (!x.HasValue)
                yield break;
            foreach (var f in this[x.Value])
            {
                yield return f;
            }
        }
        /// <summary>
        /// Gets the delimited record at the specified index
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public DelimitedRecord this[ulong hashValue, int index]
        {
            get
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), "Value cannot be less than zero");
                List<DelimitedRecord> f;
                if (!Content.TryGetValue(hashValue, out f))
                    return null;
                if (!f.HasMinimumCount(index + 1))
                    throw new ArgumentOutOfRangeException(nameof(index), "Value is too large");
                return f[index];
            }
        }
        /// <summary>
        /// Gets count of records associated with the provided hash
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int CheckCount(ulong Value)
        {
            List<DelimitedRecord> rv;
            if (!Content.TryGetValue(Value, out rv))
                return 0;
            return rv.Count;
        }
        /// <summary>
        /// Gets the number of records with the same hash as the delimited record
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int CheckCount(DelimitedRecord Value)
        {
            var hash = CheckHash(Value);
            if (!hash.HasValue)
                return 0;
            List<DelimitedRecord> rv;
            if (!Content.TryGetValue(hash.Value, out rv))
                return 0;
            return rv.Count;
        }
        /// <summary>
        /// Adds the delimited record to the underlying data structure, if the hash has a value.
        /// </summary>
        /// <param name="newRecord"></param>
        public void Add(DelimitedRecord newRecord)
        {
            if (newRecord == null)
                throw new ArgumentNullException(nameof(newRecord));
            var h = CheckHash(newRecord);
            if (!h.HasValue)
                return;
            List<DelimitedRecord> c;
            if (!Content.TryGetValue(h.Value, out c))
            {
                c = new List<DelimitedRecord>();                
                Content[h.Value] = c;
            }
            c.Add(newRecord);            
        }
        /// <summary>
        /// Adds all records to the underlying data structure
        /// </summary>
        /// <param name="newRecordList"></param>
        public void Add(params DelimitedRecord[] newRecordList)
        {
            newRecordList.ForEach(r => Add(r));
        }
        /// <summary>
        /// Adds all records to the underlying data structure
        /// </summary>
        /// <param name="newRecordList"></param>
        public void Add(IEnumerable<DelimitedRecord> newRecordList)
        {
            foreach (var record in newRecordList)
                Add(record);
        }
        /// <summary>
        /// Completely clears the underlying data structure
        /// </summary>
        public void Clear()
            => Content.Clear();
        /// <summary>
        /// Removes all records associated with the hash
        /// </summary>
        /// <param name="hash"></param>
        public void Clear(ulong hash)
        {
            if (Content.ContainsKey(hash))  Content.Remove(hash);
        }
        /// <summary>
        /// Removes all records with the same hash as the record
        /// </summary>
        /// <param name="record"></param>
        public void ClearSameHash(DelimitedRecord record)
        {
            var h = CheckHash(record);
            if (h.HasValue && Content.ContainsKey(h.Value))
                Content.Remove(h.Value);            
        }
    }
}
