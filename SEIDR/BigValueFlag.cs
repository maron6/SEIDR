using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR
{
    /// <summary>
    /// Class for holding a very large number of flags somewhat like a zero based array of bools initialized to false
    /// </summary>
    public class BigValueFlag :IEnumerable<ulong>
    {
        /// <summary>
        /// Enumerate through the flagged values
        /// <para>NOTE: Because of the way values are stored internally, this will probably not be in the same order that the values were flagged.</para>
        /// <para>Ex: See the debug console output of BigValueFlagTest's method 'BigValueFlagEnumeratorTest' from the Unit test project</para>
        /// <para>Values *will* be contiguous in chunks of 64, though. I.e., if 1 and 2 and 63 are flagged, 1 is always before 2 and 2 always before 63 in this method's output.</para>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ulong> GetEnumerator()
        {
            foreach(var kv in internalvalues)
            {
                int offset = 0;
                ulong v = kv.Value;
                while (v != 0)
                {
                    if (v % 2 == 1)
                        yield return kv.Key * mod + (ulong)offset;
                    offset++;
                    v = v >> 1;
                }                
            }
        }
        public double? AverageFlaggedValue()
        {
            if (this.UnderMaximumCount(1))
                return null;
            if(MaxFlagged > long.MaxValue)
                throw new ArgumentOutOfRangeException("There are active flags which are too big for current implementation");
            return (from l in this
                    select (long)l).Average();
            
        }
        Dictionary<ulong, ulong> internalvalues = new Dictionary<ulong, ulong>();
        /// <summary>
        /// Checks if a value has been flagged as true or false.<para>Set: flips/unflips the value.</para>
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool this[ulong check]
        {
            get
            {
                ulong key = check / mod;
                int offset = (int)(check % mod);
                ulong value;
                if (!internalvalues.TryGetValue(key, out value))
                    value = 0ul;
                return (value & (1ul << offset)) != 0;
            }
            set
            {                
                ulong key = check / mod;
                int offset = (int)(check % mod);
                ulong internalval;
                if (!internalvalues.TryGetValue(key, out internalval))
                    internalval = 0ul;
                if (value)
                {
                    internalval |= (1ul << offset);
                    internalvalues[key] = internalval;
                }
                else if(internalval > 0)
                {
                    internalval &= ~(1ul << offset);
                    if (internalval == 0)
                        internalvalues.Remove(key);
                    else
                        internalvalues[key] = internalval;
                }
            }
        }
        /// <summary>
        /// Gets the largest flag value, or null if nothing is flagged
        /// </summary>
        public ulong? MaxFlagged
        {
            get
            {
                if (internalvalues.Count == 0)
                    return null;
                var k = internalvalues.Max(kv => kv.Key);
                ulong v = internalvalues[k];
                int offset = 0;
                while(v > 1)
                {
                    offset++;
                    v = v >> 1;
                }
                return k * mod + (ulong)offset;
            }
        }
        /// <summary>
        /// Gets the minimum value that has been flagged, or null if nothing is flagged
        /// </summary>
        public ulong? MinFlagged
        {
            get
            {
                if (internalvalues.Count == 0)
                    return null;
                var k = internalvalues.Min(kv => kv.Key);
                ulong v = internalvalues[k];
                int offset = 0;
                while (v >0 && v % 2 == 0)
                {
                    offset++;
                    v = v >> 1;
                }
                return k * mod + (ulong)offset;
            }
        }
        /// <summary>
        /// Returns the number of flagged (true) records. Will not overflow
        /// </summary>
        public int Count
        {
            get
            {
                var i = 0;
                foreach(var v in internalvalues.Values)
                {
                    for(int j = 0; j< mod; j++)
                    {
                        if ((v & (1ul << j)) != 0)
                        {
                            i++;
                            if (i == int.MaxValue)
                                return i;
                        }
                    }
                }
                return i;
            }
        }
        /// <summary>
        /// Returns the number of flagged (true) records
        /// </summary>
        public ulong BigCount
        {
            get
            {
                var i = 0ul;
                foreach (var v in internalvalues.Values)
                {
                    for (int j = 0; j < mod; j++)
                    {
                        if ((v & (1ul << j)) != 0)
                        {
                            i++;                            
                        }
                    }
                }
                return i;
            }
        }
        const int mod = sizeof(ulong) * 8;        
        /// <summary>
        /// Resets all values to false.
        /// </summary>
        public void Clear()
        {
            internalvalues.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
