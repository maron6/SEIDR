using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace SEIDR.Doc
{
    /// <summary>
    /// Note this has not been tested yet
    /// </summary>
    public class FileSorter
    {        
        string _SortFile;        
        string _TempFolder;
        public string TempFolder { get { return _TempFolder; } }
        public string SortFile
        {
            get
            {
                return _SortFile;
            }
            set
            {

                _SortFile = value;
                _TempFolder = Path.GetFileNameWithoutExtension(_SortFile) + "_Temp\\";
            }
        }
        readonly string TempFileTemplate;
        
        List<UInt64[]> Placements;


        char delim = '\0';
        ColumnDateFormatter df;
        int[] sortIndices;
        string[] indexTypes;
        
        int _SortThreadCount = 15;
        public int SortThreadCount
        {
            get { return _SortThreadCount; }
            set 
            { 
                if (_SortThreadCount < 1)
                    return;
                _SortThreadCount = value;
            }
        }
        
        //It's internal so it doesn't really matter as long as they aren't matching...
        const string FIELDTYPE_STRING   = "S";
        const string FIELDTYPE_DATE     = "D";
        const string FIELDTYPE_NUMBER   = "N";
        
        /// <summary>
        /// Sort FileInput based on sortIndexes. Ignore the first line when sorting if the System.IO.File has a header.<para>
        /// Any lines that do NOT match the standard line length may be dropped or treated in an unpredictable way.
        /// </para>
        /// </summary>
        /// <param name="FileInput"></param>
        /// <param name="sortIndexes"></param>
        /// <param name="hasHeader"></param>
        public FileSorter(string FileInput, int[] sortIndexes, bool hasHeader)
        {
            Placements = new List<UInt64[]>();
            SortFile = FileInput;
            if (!System.IO.File.Exists(FileInput))
                throw new FileSorterException("FileInput does not exist.");
            sortIndices = sortIndexes;
            indexTypes = new string[sortIndexes.Length];
            df = new ColumnDateFormatter(sortIndexes.Length);
            SkipFirst = hasHeader;
            TempFileTemplate = TempFolder + "WORKING_";
        }
        bool SkipFirst;
        public void Sort()
        {
            FileReader qr = new FileReader(SortFile);
            qr.ChangeLineEnding = true;
            int work;
            string[] lines = qr.Read(out work);
            if(work == 0)
                return;//nothing to do.
            string check = lines[SkipFirst? 1: 0];
            delim = check.GuessDelimiter(); //.GuessDelimiter(check);
            string[] typeChecks = check.Split(delim);
            int dfIdx = 0; //actual index within the types and date formatter
            foreach (int idx in sortIndices)
            {
                int index = Math.Abs(idx);
                decimal d;
                if (this.df.ParseFormat(typeChecks[index], dfIdx)) { indexTypes[dfIdx] = FIELDTYPE_DATE; }
                else if (Decimal.TryParse(typeChecks[index], out d)) { indexTypes[dfIdx] = FIELDTYPE_NUMBER; }
                else { indexTypes[dfIdx] = FIELDTYPE_STRING; }
                dfIdx++;
            }
            dfIdx = 0; //Use as index within full System.IO.File now.
            do
            {
                bool t;
                Sort(lines, dfIdx);
                lines = qr.Read(out work, out t);
                dfIdx++;
                if (!t)
                    work = 0;
            } while (work > 0);
            Merge();
        }
        private void Sort(string[] lines, int Phys)
        {
            FileReader qr = new FileReader(SortFile, true);
            qr.ChangeLineEnding = true;
            int work;
            int TempFile = 0;
            Placements.Clear();
            object tempLock = new object();
            do
            {                
                string WorkFile = TempFileTemplate + TempFile;
                string[] compLines = qr.Read(out work);                
                //if(Phys == 0)
                {
                    //SetUp Placements to account for this block...
                    Placements.Add(new ulong[compLines.Length]);
                }
                int threads = SortThreadCount;
                //If tempFile == 0, compLines[0] should just be treated as if sort returned true and inc the count.
                for (int i = 0; i < SortThreadCount; i++)
                {
                    int v = i;
                    System.Threading.Thread t = new System.Threading.Thread(()=>
                    {
                        for (int j = v; j < lines.Length; j += SortThreadCount)
                        {
                            bool s1 = TempFile == 0 && SkipFirst;
                            if (s1)
                            {
                                if (j == 0)
                                    continue;
                                Placements[Phys][j]++;
                            }
                            for (int comp = s1 ? 1 : 0; comp < compLines.Length; comp++)
                            {
                                
                                bool physEarly = Phys < TempFile;
                                if (!physEarly && Phys == TempFile)
                                {
                                    if (j == comp)
                                        continue; //Don't compare against self because it does no good.
                                    if (j < comp)
                                        physEarly = true;                                    
                                }
                                if(!Sort(lines[j], compLines[comp], physEarly))
                                {
                                    //True if string a comes before string b, so we want to increment when false.
                                    Placements[Phys][j]++;
                                }
                            }
                        }
                        lock (tempLock)
                        {
                            threads--;
                        }
                    }
                    );
                    t.IsBackground = true;
                    t.Start();
                }
                while (threads > 0) { }
                TempFile++;
                if (work < qr.block)
                    work = 0;
            } while (work > 0);            
            for (; work < TempFile; work++)
            {
                if (Phys == 0)
                {
                    string line = String.Empty;
                    for (int i = 0; i < Placements[work].Length-1; i++)
                    {
                        line += '\n';
                    }
                    using (StreamWriter sw = new StreamWriter(TempFileTemplate + work, false))
                    {
                        sw.Write(line);
                    }
                }
                Merge(lines, TempFileTemplate + work, Phys);
            }
        }
        private void Merge()
        {
            string result = Path.GetFileNameWithoutExtension(SortFile) + ".SORTED" + Path.GetExtension(SortFile);
            using (StreamWriter sw = new StreamWriter(result, false))
            {
                for (int fnum = 0; fnum < Placements.Count; fnum++)
                {
                    string t = System.IO.File.ReadAllText(TempFileTemplate + fnum);
                    sw.Write(t + '\n'); //Need to add the newline for the last record in each System.IO.File.
                    System.IO.File.Delete(TempFileTemplate + fnum);
                }
            }
            Directory.Delete(TempFolder);
        }
        private void Merge(string[] lines, string FileName, int idx)
        {
            string[] merge = System.IO.File.ReadAllText(FileName).Split('\n');
            for (int i = 0; i < Placements[idx].Length; i++ )
            {
                ulong count = Placements[idx][i];
                int filePos;
                if (GetFileIndex(count, out filePos) != idx)
                {
                    continue;
                }
                merge[filePos] = lines[i];
            }
            using (StreamWriter sw = new StreamWriter(FileName, false))
            {
                for(int i = 0; i <merge.Length; i++)
                {
                    string line = merge[i] +( i == merge.Length - 1 ? "" : "\n");
                    sw.Write(line);
                }//Number of lines in the actual array needs to be the count of '\n' + 1
            }
        }
        private int GetFileIndex(ulong line, out int pos)
        {
            if (line < (ulong)Placements[0].Length)
            {
                pos = (int)line;
                return 0;
            }
            return GetFileIndex(line, 1, (ulong)Placements[0].Length, out pos);
        }
        private int GetFileIndex(ulong line, int PlaceIdx, ulong tally, out int pos)
        {
            pos = (int)(line - tally);
            if (PlaceIdx == Placements.Count)
                return PlaceIdx - 1;
            ulong temp =  tally + (ulong)Placements[PlaceIdx].Length;
            if (line < tally + temp)
            {
                return PlaceIdx;
            }
            return GetFileIndex(line, PlaceIdx + 1, temp, out pos);
        }
        bool? Sortvarchar(string a, string b, bool Asc)
        {
            int x = string.Compare(a, b, false);
            if (x == 0)
                return null;
            if (x > 0)
                return Asc;
            return !Asc;            
        }
        /// <summary>
        /// Return true if a comes before b in the sort
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="physEarly">Set to true if a comes before b in the physical System.IO.File</param>
        /// <returns></returns>
        bool Sort(string a, string b, bool physEarly)
        {
            string[] splitA = a.Split(delim);
            string[] splitB = b.Split(delim);
            for (int dfIdx = 0; dfIdx < sortIndices.Length; dfIdx++ )
            {
                int idx = sortIndices[dfIdx];
                bool Asc = idx > 0;
                int index = Math.Abs(idx);
                if (splitA.Length < index || splitB.Length < index){
                    if(splitA.Length >= index)
                        return true; //B is missing comparison fields, say that A comes first.
                    if(splitB.Length >= index)
                        return false; //A is missing comparison fields but B isn't. A does not come before B
                    break; //Both less than index, settle by hash.
                }                    
                if (indexTypes[dfIdx] == FIELDTYPE_STRING)
                {
                    int x = string.Compare(splitA[index], splitB[index], false);
                    if (x == 0)
                        continue;
                    if (x > 0)
                        return Asc;
                    return !Asc;
                    //if (Asc)
                    //{
                    //    if (x > 0)
                    //    {
                    //        return true;
                    //    }
                    //    else return false;
                    //}
                    //if (x > 0)
                    //    return false;
                    //return true;
                }
                else if (indexTypes[dfIdx] == FIELDTYPE_NUMBER)
                {
                    
                    decimal x;
                    decimal y;
                    if(!Decimal.TryParse(splitA[index], out x)){
                        if (!Decimal.TryParse(splitB[index], out y))
                        {
                            //continue;
                            bool? temp = Sortvarchar(splitA[index], splitB[index], Asc);
                            if (temp == null)
                                continue;
                            return (bool)temp;
                        }
                        return !Asc;
                    }
                    if (!Decimal.TryParse(splitB[index], out y))
                    {
                        return Asc;
                    }
                    decimal z = x - y;
                    if (z == 0)
                        continue;
                    if (z > 0)
                        return Asc;
                    return !Asc;
                    //if (Asc)
                    //{
                    //    if (z > 0)
                    //        return true;
                    //    return false;
                    //}
                    //if (z > 0)
                    //    return false;
                    //return true;
                }
                else
                {
                    //date
                    DateTime x;
                    DateTime y;
                    if (!df.ParseString(dfIdx, splitA[index], out x))
                    {
                        if (df.ParseString(dfIdx, splitB[index], out y))                        
                            return !Asc;                            
                        
                        bool? temp = Sortvarchar(splitA[index], splitB[index], Asc);
                        if (temp == null)
                            continue;
                        return (bool)temp;
                    }
                    if (!df.ParseString(dfIdx, splitB[index], out y))
                    {
                        return Asc;
                    }
                    int z = DateTime.Compare(x, y);
                    if (z == 0)
                        continue;
                    if (z > 0)
                        return Asc;
                    return !Asc;
                    //if (Asc)
                    //{
                    //    if (z > 0)
                    //        return true; //x is later than y and count should be increased
                    //    return false;
                    //}
                    //if (z > 0)
                    //    return false; //x is later than y and count should not be increased because we are descending.
                    //return true;
                }

            }
            {
                //Only thing mattering here is consistency.
                //TODO: Implement hasher... but rethink its place..
#if !DEBUG
                int x = Hasher.Hash(a);
                int y = Hasher.Hash(b);
#else
                int x = 0;
                int y = 1;
#endif

                if (x == y)
                    return physEarly;
                return x > y;
            }
        }
    }
    [Serializable]
    public class FileSorterException : Exception
    {
        public FileSorterException() { }
        public FileSorterException(string message) : base(message) { }
        public FileSorterException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
