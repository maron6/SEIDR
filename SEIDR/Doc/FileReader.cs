using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace SEIDR.Doc
{

    /// <summary>
    /// FileReader is meant to read lines from a streamreader with the results put into string arrays split on standardized newlines.
    /// <para>Expectation is that lines will be split by some character, most likely a newline. If the newline is CRLF, will need to set the ChangeLineEndings property to true.
    /// </para>
    /// <para>Reading will return an array of strings split by a char</para>
    /// <para>Standard use:</para> 
    /// <para>QuickReader qr = new QuickReader(...);</para>
    /// <para>bool working = true;</para>
    /// <para> while(working){</para>
    /// <para> string[] lines = qr.Read(out working);...</para>
    /// <para>}</para>
    /// <para>Alternate, need to do something for first block only:</para> 
    /// <para>string[] lines = qr.Read(out working);</para>
    /// do{<para>...if (!moreWork)break;</para><para> qr.Read(out working);</para>
    /// <para>}while(true);</para>
    /// </summary>
    /// <remarks>
    /// <para>Any static methods will act on a single string and assume that there are no line endings inside the string.</para>
    /// There are also two static methods that are both for splitting lines and keeping any delimiters that are text qualified. 
    /// </remarks>
    public class FileReader:IDisposable
    {
        private static DateTime ParseDate(string fileName, string dateFormat, DateTime create)
        {
            char[] fDel  = new char[]{'<','>'};
            if (dateFormat.IndexOfAny(fDel) < 0)
                return create.Date;
            string[] sl = dateFormat.Split('*');
            //int lastIndex = -1;
            string year  = null;
            string month = null;
            string day   = null;
            foreach (string s in sl)
            {
                if (s == "")
                    continue;
                if(s.IndexOfAny(fDel) < 0){
                    int x = fileName.IndexOf(s);
                    if(x < 0 || x + s.Length > fileName.Length)
                        return create.Date; //No match. Don't trust the rest
                    fileName = fileName.Substring(x + s.Length);
                    continue;
                }
                
                string[] tokens = s.Split('<');
                if (tokens[0] != "")
                {
                    int x = fileName.IndexOf(tokens[0]);
                    if (x < 0)
                        return create;
                    fileName = fileName.Substring(x + tokens[0].Length);
                }
                for (int i = 1; i < tokens.Length; i++ )
                {
                    string token = tokens[i];
                    int check = token.IndexOf('>');
                    if (check < 0)
                        return create;
                    string search = token.Substring(0, check).ToUpper();
                    check = fileName.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                    if (check < 0)
                        return create;
                    fileName = fileName.Substring(check);
                    if (search == "YYYY")
                    {
                        year = fileName.Substring(0, 4);
                        fileName = fileName.Substring(4);
                    }
                    else if (search == "YY")
                    {
                        year = "20" + fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "MM")
                    {
                        month = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "DD")
                    {
                        day = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else
                        return create;                    
                    //int x = fileName.IndexOf(token);                    

                    if (year != null && month != null && day != null)
                    {
                        try
                        {
                            return DateTime.ParseExact(year + month + day, "yyyyMMdd", new System.Globalization.CultureInfo("EN-US"));
                        }
                        catch { return create; }
                    }
                }                
            }
            return create.Date;
        }
        /// <summary>
        /// Splits a string so that delimiters inside quotes do not create extra fields. Use '"' as Text Qualifier
        /// </summary>
        /// <remarks>Assumes that there is no line ending in the string.</remarks>
        /// <param name="line">Line to be split</param>
        /// <param name="delimiter">Delimiter to split the file</param>
        /// <returns>Array of strings split by delimiter except where the delimiter is between text qualifiers</returns>
        private static string[] SplitOutsideQuotes(string line, char delimiter)
        {
            string[] switcher = line.Split('"');
            for (int i = 0; i < switcher.Length; i += 2)
            {
                switcher[i] = switcher[i].Replace(delimiter, (char)12);
            }
            line = string.Join("\"", switcher);
            return line.Split((char)12);

        }
        /// <summary>
        /// Removes invisible characters from a line of text. Also replaces u0092 with single quote.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string CleanLine(string line)
        {
            string fastInvalids = @"[\u0000-\u0008\u000B-\u0019\u00A0]";
            line = System.Text.RegularExpressions.Regex.Replace(line, fastInvalids, "");
            line = System.Text.RegularExpressions.Regex.Replace(line, @"[\u0092]", "'");
            return line;
        }
        /// <summary>
        /// Splits a string so that delimiters inside quotes do not create extra fields.
        /// </summary>
        /// <remarks>Assumes that there is no line ending in the string.</remarks>
        /// <param name="line">Line to be split</param>
        /// <param name="delimiter">Delimiter to split the file</param>
        /// <param name="TextQual">Text qualifier. Delimiters between text qualifiers will be kept</param>
        /// <returns>Array of strings split by delimiter except where the delimiter is between text qualifiers</returns>
        private static string[] SplitOutsideQuotes(string line, char delimiter, char TextQual)
        {
            string[] switcher = line.Split(TextQual);
            for (int i = 0; i < switcher.Length; i += 2)
            {
                switcher[i] = switcher[i].Replace(delimiter, (char)12);
            }
            line = string.Join("" + TextQual, switcher);
            return line.Split((char)12);
        }
        /// <summary>
        /// Value used by newly created quickreader objects as the block size
        /// </summary>
        public static int defaultBlock = 10000000;
        int? _block = null;
        /// <summary>
        /// Sets the number of characters to try to read per call of Read.
        /// <para>
        /// Minimum value: 1000. An error will not be thrown if you try to set it to less, but the value will just be set to 1000 instead.
        /// </para>
        /// <remarks>
        /// Note that reading less characters than the size of the block is not a problem. 
        /// <para>Rather, reading fewer characters than the block size is the way to know that we're done with the file.
        /// </para>
        /// </remarks>
        /// </summary>
        public int block
        {
            get
            {
                if (_block == null)
                    _block = FileReader.defaultBlock;
                return (int)_block;
            }
            set
            {
                if (_block < 1000)
                    return;
                _block = value;
            }
        }
        string path;
        string hold;
        char _splitter;

        StreamReader sr;
        /// <summary>
        /// Set to true to remove all quotes.
        /// </summary>
        public bool TrimQuotes = false;
        /// <summary>
        /// Set to true to remove invisible characters like form feed or null
        /// </summary>
        public bool CleanInvisibles = false;
        /// <summary>
        /// Character to split lines on.         
        /// </summary>
        public char splitter{get{return _splitter;} set{ _splitter = value;}}
        bool _changeEnd;
        /// <summary>
        /// If true, change all groups of line endings to a single LF. Else try to read as is.
        /// </summary>
        public bool ChangeLineEnding { get { return _changeEnd; } set { _changeEnd = value; } }
        /// <summary>
        /// Constructor with custom splitter
        /// </summary>
        /// <param name="path">File path to read</param>
        /// <param name="separator">Character to split lines on. If you want to split on line endings and need more than one character, you'll need to set ChangeLineEnding and then use '\n'.
        /// <para>If you want to split manually, I would suggest splitting by line endings and using '\n', then joining on '\n' and then doing your custom split.</para></param>
        public FileReader(string path, char separator)
        {
            _changeEnd = false;
            FilePath = path;
            CleanInvisibles = false;
            TrimQuotes = false;
            hold = "";
            this.path = path;
            _splitter = separator;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            sr = new System.IO.StreamReader(fs);
            //sr = new System.IO.StreamReader(path, Encoding.GetEncoding("Windows-1252"));
        }
        /// <summary>
        /// Full Path to file being read.
        /// </summary>
        public string FilePath { get; private set; }
        /// <summary>
        /// Constructor with option to set whether to lock on read
        /// </summary>
        /// <param name="path"></param>
        /// <param name="separator"></param>
        /// <param name="readlock">If true, lock the file while open.</param>
        public FileReader(string path, char separator, bool readlock)
        {
            FilePath = path;
            _changeEnd = false;
            CleanInvisibles = false;
            TrimQuotes = false;
            hold = "";
            this.path = path;
            _splitter = separator;
            FileStream fs = new FileStream(path, FileMode.Open, readlock? FileAccess.ReadWrite : FileAccess.Read);
            sr = new System.IO.StreamReader(fs);
        }

        /// <summary>
        /// QuickReader with a Write lockif the boolean is true.
        /// </summary>
        /// <param name="path">Path of file to read</param>
        /// <param name="WriteLock">If true, prevent the file from being modified by taking the write lock.</param>
        public FileReader(string path, bool WriteLock)
        {
            _changeEnd = false;
            CleanInvisibles = false;
            TrimQuotes = false;
            hold = "";
            FilePath = path;
            this.path = path;
            _splitter = '\n';
            FileStream fs = new FileStream(path, FileMode.Open, WriteLock ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);
            sr = new System.IO.StreamReader(fs);
        }
        /// <summary>
        /// Constructor. Default splitter is '\n'.
        /// </summary>
        /// <param name="path">File path to read</param>
        public FileReader(string path)
        {
            _changeEnd = false;
            CleanInvisibles = false;
            TrimQuotes = false;
            hold = "";
            _splitter = '\n';
            this.path = path;
            FilePath = path;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);            
            sr = new System.IO.StreamReader(fs);
            //sr = new System.IO.StreamReader(path, Encoding.GetEncoding("Windows-1252"));
        }
        /// <summary>
        /// Deconstructor.
        /// </summary>
        ~FileReader()
        {            
            Dispose(false);
        }
        /// <summary>
        /// Reads up to (default) 1000000 characters from the file and returns an array of strings split by the separator. Will remove the final string unless a non full block was read.
        /// <para>If this method is called after the file finishes reading, an Exception will be thrown.</para>
        /// </summary>
        /// <param name="work">Number of characters read from the file. Does not necessarily match the number of characters in the joined string[]</param>
        /// <param name="moreWork">Whether or not there are more characters in the file to read. Equivalent to checking if work is less than the block size.</param>
        /// <returns>Split lines from the file.</returns>
        public string[] Read(out int work, out bool moreWork)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("FileReader");
            }
            if (sr == null)
            {                
                //moreWork = false;
                //return null;
                throw new Exception("File reading is finished. No More Work.");
            }
            string crlf = "" + (char)13 + (char)10;
            string lf = "" + (char)10;
            string ff = "" + (char)12;
            //block;
            //int added = hold.Length;
            //string line;
            char[] line = new char[block];
            string temp;
            //string write = "";
            //string hold = "";
            int pos = 0;            
            
            int t = sr.ReadBlock(line, pos, block);
            int start = 0;
            if (t > 0 && line[0] == '\n')
                start = 1;
            temp = new string(line);
            temp = hold + temp.Substring(0 /*start*/, t);
            hold = "";
            if (TrimQuotes)
            {
                temp = temp.Replace("\"", "");
            }
            if (_changeEnd)
            {
                string subset;
                string restore = string.Empty;
                if (t < block)
                    subset = temp;
                else
                {
                    int idx = temp.LastIndexOf(_splitter);
                    if (idx + 1 < temp.Length)
                        idx++;
                    subset = temp.Substring(0, idx);
                    restore = temp.Substring(idx);
                }
                /*
                     * Bug Fix: 
                     * When change end is only checked here and if we end on an \r from an \r\n, 
                     * the \r will be cleaned up separately from the \n and then we get an extra newline
                     * 
                     * Solution: if doing a full block, only clean up until the last splitter, so that everything after is cleaned in the next block
                     */
                //string subset = temp.Substring(0, temp.Length - 1);//dont change the last character so that it can be cleaned with the next batch
                subset = System.Text.RegularExpressions.Regex.Replace(subset, @"[" + crlf + ff + "]+", lf);
                temp = subset + restore; // temp[temp.Length - 1];
                //temp = System.Text.RegularExpressions.Regex.Replace(temp, @"[" + crlf + ff + "]+", lf);
            }
            if (CleanInvisibles)
            {
                temp = CleanLine(temp);
            }
            string[] fields = temp.Split(_splitter);
            //This might do weird stuff if fields.Length == 1...
            if (t == block && fields.Length > 1)
            {
                hold = fields[fields.Length - 1];
                temp = string.Join("" + _splitter, fields, 0, fields.Length - 1);                
                fields = temp.Split(_splitter); 
            }    
            work = t;
            moreWork = t == block;
            if (!moreWork)
            {
                sr.Close();
                sr.Dispose();
                sr = null;
                //if (hold != "")
                //{
                //    fields = (string.Join("" + _splitter, fields) + splitter + hold).Split(_splitter);
                //}//UPDATE: Realized that because it checks t == block, hold should never be populated
            }
            return fields;
        }
        /// <summary>
        /// Reads up to (default) 1000000 characters from the file and returns an array of strings split by the separator. Will remove the final string unless a non full block was read.
        /// <para>If this method is called after the file finishes reading, an Exception will be thrown.</para>
        /// </summary>
        /// <param name="moreWork">True if there is (probably) more content to read from the file. It would also be set to true if the file happened to end filling up a block.</param>
        /// <returns>Split string of lines containing the read characters. Split default is '\n' but can be changed with one of the constructors.</returns>
        public string[] Read(out bool moreWork)
        {
            int noUse;
            return Read(out noUse, out moreWork);
        }
        /// <summary>
        /// Reads up to (default) 1000000 characters from the file and returns an array of strings split by the separator. Will remove the final string unless a non full block was read.
        /// <para>If this method is called after the file finishes reading, an Exception will be thrown.</para>
        /// </summary>
        /// <param name="work">The number of characters read. There's more to read in the file as long as this is equal to the size of the block(1000000).
        /// <para>Does not account for characters added or subtracted from removing quotes or holding to keep only full lines in the strings contained in the array</para>
        /// <para>If the value is less than the QuickReader's block size, there is no more work to do.
        /// </para>
        /// </param>
        /// <returns>Split string of lines from the file.</returns>
        public string[] Read(out int work)
        {
            bool noUse;
            return Read(out work, out noUse);
        }

        /// <summary>
        /// Tries to guess the delimiter of a line string from the following characters: |,\t;:
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static char GuessDelimiter(string line)
        {
            string Delimiter = "|" + ",\t;:" ;
            char current = '\0';
            foreach (char i in Delimiter)
            {
                if (line.Split(i).Length > 1)
                {
                    current = i;
                    break;
                }
            }
            return current;
        }
        /// <summary>
        /// Cleans a single line using the method from validate clean.
        /// </summary>
        /// <param name="line">Line to clean</param>
        /// <param name="combineNewlines">If true, all groups of newline characters will be replaced by a single LF</param>
        /// <returns>Cleaned line</returns>
        public static string CleanLine(string line, bool combineNewlines)
        {
            string fastInvalids = @"[\u0000-\u0008\u000B-\u0019\u00A0]";
            string crlf = "" + (char)13 + (char)10;
            string lf = "" + (char)10;
            string ff = "" + (char)12;
            string cr = "" + (char)13;
            string newlines = "[" + lf + crlf + ff + cr + "]+";
            line = System.Text.RegularExpressions.Regex.Replace(line, fastInvalids, "");
            line = System.Text.RegularExpressions.Regex.Replace(line, @"[\u0092]", "'");
            if (combineNewlines) 
                line = System.Text.RegularExpressions.Regex.Replace(line, newlines, lf);            
            return line;
        }
        public bool Disposed { get; private set; } = false;
        /// <summary>
        /// Disposes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (sr != null)
                {
                    sr.Close();
                    sr.Dispose(); //Note that this disposes the underlying stream as well                                
                    sr = null;
                }
                Disposed = true;
            }
        }
    }
}
