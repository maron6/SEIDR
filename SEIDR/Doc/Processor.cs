using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SEIDR;
using System.IO;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace SEIDR.Doc
{
    /// <summary>
    /// Can be Customized for doing some File Analysis.
    /// <para>By using the addon boolean, you can create multiple processors to check for different things in a given file.</para>
    /// <para>You can also use the addon boolean to combine analysis data from multiple files into one final output file.</para>
    /// <para> </para>
    /// <para>Run Procedure:</para>
    /// <para>Initialize Processor. Set things like Name and the AddOn Boolean or AddAdditionalInformation Boolean.</para>    
    /// <para>Initialize any objects you want to modify during any of the processing methods.</para>
    /// <para>Anything that can be found by using the indexer[string ReferenceName] will appear in the output report, plus any notes from the updateNote(string referenceName, noteText) method.</para>
    /// <para>Call The processor's Run method.</para>
    /// </summary>
    /// <remarks>
    /// Custom objects should be fine to use with the indexer as long as they override ToString().
    /// <para>The first Processor running in a program should have Addon set to false(the Default). An existing file will then be moved so that only data from your processors are in the same file. Header information can be added using the ReportHeader string array.</para>
    /// <para>The quick reader used by Processor will not provide Line Endings to the user. Any line ending analysis would require a custom reading of the file, or a different initiation the quick reader. In both cases, this means that the Processor class would not work for this purpose.</para>
    /// <para>Use the GroupOn class to organize data along with some premade and implicit aggregation functionality.</para>
    /// <para>Running multiple processor threads on the same file should technically be safe and not have lock issues, but is untested.</para>
    /// </remarks>
    public class Processor
    {
        static object LockObj;
        static Processor(){
            LockObj = new object();
        }
        string _OutputFile;
        /// <summary>
        /// Full path of file to be created by processor.
        /// </summary>
        public string OutputFile { get { return _OutputFile; } }
        /// <summary>
        /// Change the output directory of the processor to be the passed directory. Also creates the directory if it doesn't exist already.
        /// </summary>
        /// <param name="directory">Path to directory</param>
        public void ChangeOutputFolder(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _OutputFile = (directory.EndsWith("\\") ? directory : directory + "\\") + Path.GetFileName(_OutputFile);
        }
        /// <summary>
        /// Set to true to override default behavior and append any data from this processor to the end of the file.
        /// <para>Default: An existing file will be renamed so that a new file can be created for this processor</para>
        /// </summary>
        public bool AddOn = false;
        /// <summary>
        /// Used for Identifying Processor Write blocks.
        /// </summary>
        public string ProcessorName = "";

        //public 
        /// <summary>
        /// Used for Identifying Processor Write Blocks. 
        /// <para>Modifies the value of ProcessorName</para>
        /// </summary>
        public string Name { set { ProcessorName = value; } }
        FileReader qr;
        //private List<ValueHolder> _results = new List<ValueHolder>();
        private Dictionary<string, ValueHolder> _results = new Dictionary<string, ValueHolder>();
        /// <summary>
        /// Public indexer. Cast values to object to store them. 
        /// <para>It may be necessary to cast to and from object when modifying values.</para>
        /// </summary>
        /// <remarks>
        /// Custom objects should be fine as long as they override ToString.
        /// </remarks>
        /// <param name="ReferenceName">Column name or parameter for grouping an object.</param>
        /// <returns>Value of object stored or null if it doesn't exist</returns>
        public object this[string ReferenceName]
        {
            get
            {
                ValueHolder stuff;
                if(!_results.TryGetValue(ReferenceName, out stuff)){
                        _results.Add(ReferenceName, new ValueHolder(ReferenceName, null));
                        return null;
                 }
                 return stuff.Value;
                 /*
                for (int i = 0; i < _results.Count; i++)
                {
                    if (_results[i].Name == ReferenceName)
                        return _results[i].Value;
                }
                _results.Add(new ValueHolder(ReferenceName, null));
                return null;*/
            }
            set
            {
                
                 if(_results.ContainsKey(ReferenceName)){
                    _results[ReferenceName].Value = value; 
                 }
                 else{
                    _results.Add(ReferenceName, new ValueHolder(ReferenceName, value));
                 }
                /*
                int i = 0;
                for (; i < _results.Count; i++)
                {
                    if (_results[i].Name == ReferenceName)
                    {
                        _results[i].Value = value;
                        return;
                    }
                    
                }
                ValueHolder temp = new ValueHolder(ReferenceName, value);
                _results.Add(temp); 
                 */
            }
        }
        /// <summary>
        /// String version of getter. Will return null if the reference has not been created.
        /// </summary>
        /// <param name="ReferenceName"></param>
        /// <returns></returns>
        public string GetString(string ReferenceName)
        {
            ValueHolder v;
            _results.TryGetValue(ReferenceName, out v);
            return v== null? null : v.Value.ToString();
            /*
            for (int i = 0; i < _results.Count; i++)
            {
                if (_results[i].Name == ReferenceName)
                    return _results[i].Value.ToString();
            }
            _results.Add(new ValueHolder(ReferenceName, null));
            return null;
             */
        }
        /// <summary>
        /// Returns true if the given reference name has an associated value.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public bool Has(string referenceName)
        {
            return _results.ContainsKey(referenceName);
            /*
            for (int i = 0; i < _results.Count; i++)
            {
                if (_results[i].Name == referenceName)
                    return true;
            }
            return false;*/
        }
        /// <summary>
        /// Attempts to find the reference. returns 0 if the reference doesn't exist or if it's not a double.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public double GetDouble(string referenceName)
        {
            ValueHolder v;
            if(_results.TryGetValue(referenceName, out v)){
                object j = v.Value;
                if(j is double)
                    return (double)j;                
            }
            /*
            for (int i = 0; i < _results.Count; i++)
            {
                if (_results[i].Name == referenceName)
                {
                    object j = _results[i].Value;
                    if (j is double)
                        return (double)j;
                }
            }*/
            return 0;
        }
        /// <summary>
        /// GroupOn version of getter. Will return null if the reference has not been created.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public GroupOn GetGroup(string referenceName)
        {
            Object j = this[referenceName];
            if (j == null)
                return null;
            GroupOn g = this[referenceName] as GroupOn;
            return g;
        }
        /// <summary>
        /// Mark objects in processor as aggregate.
        /// </summary>
        /// <param name="ReferenceNames">List of object names to mark aggregate</param>
        public void SetAggregates(params string[] ReferenceNames)
        {
            foreach (string j in ReferenceNames)
            {
                ValueHolder v;
                if(_results.TryGetValue(j, out v)){
                    v.Aggregate = true;
                }
                /*
                for (int i = 0; i < _results.Count; i++)
                {
                    if (j == _results[i].Name)
                    {
                        _results[i].Aggregate = true;
                        break;
                    }
                }*/
            }
        }
        /// <summary>
        /// Long version of getter. Returns 0 if not set. The value of the object if you try to get it via the indexer will still be null though.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public long GetLong(string referenceName)
        {
            object j = this[referenceName];
            if (j == null)
                return 0;
            else return Convert.ToInt64(j);
        }
        /// <summary>
        /// int version of getter. Returns 0 if not set. The value of the object if you try to get it via the indexer will still be null though.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public int GetInt(string referenceName)
        {
            object j = this[referenceName];
            if (j == null)
                return 0;
            else return Convert.ToInt32(j);
        }
        /// <summary>
        /// Default datetime for GetDate(string)
        /// </summary>
        public static DateTime defaultGet { get { return new DateTime(400, 1, 1); } }
        /// <summary>
        /// Datetime version of getter. Returns the value of defaultGet if the value has not been set yet
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public DateTime GetDate(string referenceName)
        {
            object j = this[referenceName];
            if (j == null)
                return defaultGet;
            if(j is String)
            {
                DateTime res;
                if(DateFormatter.ParseOnce(j.ToString(), out res))
                    return res;
                return defaultGet;
            }
            return Convert.ToDateTime(j);
        }

      
        /// <summary>
        /// Removes a custom object from the results that will be in the output.
        /// </summary>
        /// <param name="referenceName">Column name or parameter for grouping an object.</param>
        public void Remove(string referenceName)
        {
            _results.Remove(referenceName);
            /*foreach (ValueHolder v in _results)
            {
                if (v.Name == referenceName)
                {
                    _results.Remove(v);
                    return;
                }
            }*/
        }
        /// <summary>
        /// Update any notes associated with ReferenceName. It is not necessary to worry about formatting, that is already handled.
        /// <para>**NOTE:You cannot clear or remove values from the notes**</para>
        /// </summary>
        /// <param name="ReferenceName">Reference name. Matches the indexer. Must already exist.</param>
        /// <param name="NoteText">Notes associated with the reference name. Will be included in output</param>
        public void UpdateNote(string ReferenceName, string NoteText)
        {
            /*
            int count = 0;
            var checkHold = from result in _results
                       where result.Name == ReferenceName
                       select result;
            foreach (var hold in checkHold)
            {
                hold.Notes = NoteText;
                count++;
            }
            if (count == 0)
            {
                ValueHolder temp = new ValueHolder(ReferenceName, null);
                temp.Notes = NoteText;
            }*/
            ValueHolder v;
            if (_results.TryGetValue(ReferenceName, out v))
            {
                v.Notes = NoteText;
            }
            else
            {
                ValueHolder temp = new ValueHolder(ReferenceName, null);
                temp.Notes = NoteText;
                _results.Add(ReferenceName, temp);
            }
            
        }

        long _RecordCount;
        /// <summary>
        /// Gets the record count. Headers and empty lines are not included.
        /// </summary>
        public long RecordCount { get { return _RecordCount; } }

        string[] ExtraFiles;// use to extend a single process and increase the available information pool        
        /// <summary>
        /// ReportHeader[0] = File Description
        /// <para>
        /// ReportHeader[...] = Section Names
        /// </para>
        /// </summary>
        public string[] ReportHeader = null;
        string reportHead
        {
            get
            {
                if(ReportHeader == null || ReportHeader.Length < 2)
                    return null;
                string header = "".PadLeft(lineLength, '=') + "\n"
                    + ReportHeader[0].ToUpper().PadRight(lineLength - 60, '=').PadLeft(lineLength, '=') + "\n"
                    + "".PadLeft(lineLength, '=') + "\n\n" + "[SECTIONS]".PadRight(lineLength - 60).PadLeft(lineLength) + "\n\n";
                for (int i = 1; i < ReportHeader.Length; i++)
                {
                    header = header + ReportHeader[i].ToUpper().PadRight(lineLength - 60).PadLeft(lineLength) + "\n";
                }
                header = header + "\n";
                return header;
            }
        }
        /// <summary>
        /// Process function to apply to every line.
        /// <para>Can be used to update the underlying values stored and accessed by the indexer in order to get some automated analysis on a file.</para>
        /// <para>Can be set to point to any void function that takes a string as a parameter</para>
        /// <para>The pointed function can also include running a ProcessEventHolder on the line after splitting by delimiter.</para>
        /// <para>Default: Point to null(Does nothing)</para>
        /// </summary>
        public Action<string> Process = null;
        /// <summary>
        /// Will be called on the first non empty string unless it's null or FileHasHeader is set to false
        /// <para>Default: Point to null</para>
        /// </summary>
        public Action<string> ProcessHeader = null;
        /// <summary>
        /// Will be called at the end of the processor's run. Can be used to add additional information/notes based on analysis of the various objects
        /// <para>Takes no parameters or points to a parameterless void function.</para>
        /// </summary>
        public Action PostProcess = null;
        /// <summary>
        /// Constructor. Allows choice for output file's path
        /// <para>Any existing copy of the output file will be renamed to have its rename time at the end of its name, unless AddOn is set to true</para>
        /// </summary>
        /// <param name="FilePath">Full path to File to be analyzed</param>
        /// <param name="OutputPath">Full path to output analysis file.</param>
        /// <param name="Append">If true, add on to the file if it exists. Else rename any existing copy of a file at OutputPath</param>
        public Processor(string OutputPath, string FilePath, bool Append )
        {
            qr = new FileReader(FilePath);
            qr.ChangeLineEnding = true;
            _OutputFile = OutputPath;
            if (_OutputFile.LastIndexOf('.') < 0)
                _OutputFile = _OutputFile + ".ilxp";
            this.AddOn = Append;
        }
        /// <summary>
        /// Default Constructor. Output File with any analysis will be the given file and added extension of ".ilxp"
        /// <para>Any existing copy of the file will be renamed to have its rename time at the end of its name, unless AddOn is set to true</para>
        /// </summary>
        /// <param name="FilePath">Path to file to be analyzed</param>
        public Processor(string FilePath)
        {
            qr = new FileReader(FilePath);
            qr.ChangeLineEnding = true;
            _OutputFile = FilePath.ToLower() + ".ilxp";
        }
        /// <summary>
        /// Constructor. Requires an output path but allows you to run multiple files through a single Processor
        /// <para>Example construction: Processor p = new Processor("C:/User/Test/Result.ilx", "C:/User/Test/Test1.txt", "C:/User/Test/Test2.txt",...)</para>
        /// </summary>
        /// <param name="outputFile">Path to file that will contain results</param>
        /// <param name="inputFiles">List of filepaths to run the processor on. This is params so it can be passed as a string[] or as a number of strings as separate parameters</param>
        public Processor(string outputFile, params string[] inputFiles)
        {
            qr = new FileReader(inputFiles[0]);
            qr.ChangeLineEnding = true;
            ExtraFiles = new string[inputFiles.Length - 1];
            for (int i = 1; i < inputFiles.Length; i++)
            {
                ExtraFiles[i - 1] = inputFiles[i]; 
            }
            _OutputFile = outputFile;
            if (_OutputFile.LastIndexOf('.') < 0)
                _OutputFile = _OutputFile + ".ilxp";
        }
        /// <summary>
        /// Run on the results of a query instead of a file.
        /// </summary>
        /// <param name="outputFile">File to contain any results</param>
        /// <param name="table">DataSet filled by a query</param>
        public Processor(string outputFile, System.Data.DataTable table)
        {            
            processTable = table;
            _OutputFile = outputFile;
            if (_OutputFile.LastIndexOf('.') < 0)
                _OutputFile = _OutputFile + ".ilxp";
        }
        System.Data.DataTable processTable = null;
        /// <summary>
        /// Restart the file settings in order to be able to do a new run.
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="inputFiles"></param>
        public void ReInit(string outputFile, params string[] inputFiles){
            qr = new FileReader(inputFiles[0]);
            qr.ChangeLineEnding = true;
            ExtraFiles = new string[inputFiles.Length - 1];
            for (int i = 1; i < inputFiles.Length; i++)
            {
                ExtraFiles[i - 1] = inputFiles[i]; 
            }
            _OutputFile = outputFile;
            if (_OutputFile.LastIndexOf('.') < 0)
                _OutputFile = _OutputFile + ".ilxp";
            _RecordCount = 0;
        }
        /// <summary>
        /// Restart using a DataSet as the data source for run
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="table"></param>
        public void Reinit(string outputFile, System.Data.DataTable table)
        {
            processTable = table;
            _RecordCount = 0;
            _OutputFile = outputFile;
            if (_OutputFile.LastIndexOf('.') < 0)
                _OutputFile = _OutputFile + ".ilxp";
        }

        /// <summary>
        /// Set to false if the file does not contain a header.
        /// <para> If true, ProcessHeader will be called on the first line, or it will be skipped if ProcessHeader is null</para>
        /// <para>Default: True.</para>
        /// </summary>
        public bool FileHasHeader = true;
        /// <summary>
        /// Read each line of the file and processes it using the methods pointed to by Process and ProcessHeader
        /// <para>PostProcess is run at the end if it has been set to point to a non null method.</para>
        /// </summary>
        /// <remarks>
        /// An exception will be thrown if Process still points to null when this is run.
        /// </remarks>
        public void Run()
        {
            if (processTable != null)
            {
                RunDataSet();
                return;
            }
            if (Process == null)
                throw new Exception("Process needs to point to a valid method.");            
            startRun = System.DateTime.Now;
            bool appliedHeader = ProcessHeader == null; //if ProcessHeader is null, treat it like header has already been applied
            bool skipHeader = FileHasHeader;
            int extrasInd = 0;
            bool moreWork;
            do
            {
                string[] lines;
                lock (LockObj)
                {
                    lines = qr.Read(out moreWork); //lock in case we want multiple processors running at once in a program
                }
                foreach (string line in lines)
                {
                    if (line.Trim() == "")
                        continue;
                    if (skipHeader && !appliedHeader)
                    {
                        ProcessHeader(line);
                        skipHeader = false;
                        appliedHeader = true; //only run one time for a group of files running on the same processor object.
                        continue;
                    }
                    else if (skipHeader) { skipHeader = false; continue; }
                    _RecordCount++;
                    Process(line);                    
                }
                if (!moreWork && ExtraFiles != null && extrasInd < ExtraFiles.Length)
                {
                    qr = new FileReader(ExtraFiles[extrasInd]);
                    qr.ChangeLineEnding = true;                    
                    moreWork = true;
                    extrasInd++;
                    skipHeader = FileHasHeader;
                }
            } while (moreWork);
            if (PostProcess != null)
                PostProcess();
            lock (LockObj)
            {
                Finish();
            }
        }
        private void RunDataSet()
        {
            if (ProcessData == null)
                throw new Exception("ProcessData does not point to a valid method.");
            startRun = System.DateTime.Now;
            foreach (System.Data.DataRow row in processTable.Rows)
            {
                ProcessData(row);
            }
            if(PostProcessData != null)
                PostProcessData();
            lock (LockObj)
            {
                Finish();
            }
        }
        /// <summary>
        /// Point to a method that processes a data row.
        /// <para>You should be able to either keep data in an object provided by the Processor's value holder(and will appear in the output), or an outside variable for your own methods and post process analysis.</para>
        /// </summary>
        public Action<System.Data.DataRow> ProcessData;
        /// <summary>
        /// Use for post processing analysis on the results of a query.
        /// </summary>
        public Action PostProcessData;
        /// <summary>
        /// Full length of each line in the output file.
        /// </summary>
        public static int lineLength = 180;
        /// <summary>
        /// space taken up by variable name in output file. 
        /// </summary>
        public static int NameLength = 60;
        private void Finish()
        {
            List<ValueHolder> endResults;
            //endResults = new List<ValueHolder>(_results.Values).OrderBy...
            //_results = _results.OrderBy<ValueHolder, string>(i =>
            endResults = (new List<ValueHolder>(_results.Values)).OrderBy<ValueHolder, string>(i =>
            {                
                string t = i.Name;                
                string[] checks = t.Split(' ');
                int numComparison = 0;
                string ncs = ("" + numComparison).PadLeft(10, '0');
                for (int j = 0; j < checks.Length; j++)
                {
                    string c = System.Text.RegularExpressions.Regex.Replace(checks[j], "[^0-9]", "");
                    if (c != "")
                    {
                        numComparison = Int32.Parse(c);
                        ncs = ("" + j).PadLeft(4, '0') + ("" + numComparison).PadLeft(6, '0');
                        break;
                    }
                }
                t = ncs + (""+ checks.Length).PadLeft(6, '0') + t;
                if (!i.Aggregate)
                    t = "_" + t;
                return t;
            }).ToList();
            if (!AddOn && File.Exists(_OutputFile))
            {
                File.Move(_OutputFile, _OutputFile + System.DateTime.Now.ToString(@"\_yyyy\_MM\_dd\_hhmmssfff"));
            }
            else if (File.Exists(_OutputFile))
            {
                FileAttributes at = File.GetAttributes(_OutputFile);
                File.SetAttributes(_OutputFile, at & ~FileAttributes.ReadOnly);
            }
            using(StreamWriter sw = new StreamWriter(_OutputFile, true, Encoding.GetEncoding("Windows-1252"), 10000)){
                string reportTOC = reportHead;
                if (reportTOC != null)
                {
                    sw.Write(reportTOC);
                }
                if (!AddOn)
                    sw.Write("\n" + "".PadLeft(lineLength, '=') + "\n");
                else
                {
                    sw.Write("\n" + "".PadLeft(lineLength, '*') + "\n\n" + "".PadLeft(lineLength, '=') + "\n");
                }
                if (ProcessorName != "")
                {
                    string name = ((ExtraFiles == null ? "" : "** ")
                                + (ProcessorName + (ExtraFiles == null ? "" : " **")).PadRight(lineLength-60, '=')).PadLeft(lineLength, '='); 
                    //sw.Write(ProcessorName.PadRight(100, '=').PadLeft(160, '=') + "\n");
                    sw.Write(name + "\n");
                    sw.Write("".PadLeft(lineLength, '=') + "\n");
                }
                foreach (ValueHolder v in endResults)
                {
                    if (v == null)
                        continue;
                    sw.Write("\n\n" + v.Name.PadRight(NameLength)
                        + FormatValue(v.Value == null ? "".PadLeft(lineLength - NameLength) : v.Value.ToString())
                        //+ v.Value.ToString().PadLeft(60).PadRight(115) 
                        + "\n");
                    if(v.Notes != "")
                        sw.Write("[Notes]:" + v.Notes+"\n");
                    
                }
                if (AddRunTimeInformation)
                {
                    sw.Write(GetRunTimeInfo());
                }
                else
                {
                    sw.Write("\n\n\n");
                }

                sw.Write("".PadLeft(lineLength, '=') + "\n");                  
                
                if (ProcessorName != "")
                {
                    sw.Write(
                        (
                            /*
                             * (ExtraFiles == null ? "" : "** ") 
                                + ProcessorName.PadRight(100, '=') 
                            + (ExtraFiles == null ? "" : "** ")
                            */
                            (   "END   "
                                +(ExtraFiles == null ? "" : "** ")
                                + (ProcessorName + (ExtraFiles == null ? "" : " **")
                                + "   END"
                                ).PadRight(lineLength-60, '='))
                         ).PadLeft(lineLength, '=') 
                         + "\n");
                    sw.Write("".PadLeft(lineLength, '=') + "\n");
                }
                
            }
            FAttModder.AddAttribute(_OutputFile, FileAttributes.ReadOnly);
        }
        private string FormatValue(string value)
        {
            value = value.Replace("\n", "").Replace("\r", ""); //remove existing newlines because they'll mess with format.
            string result = "";
            if (value.Length <= lineLength-NameLength)
            {
                return value.PadLeft(lineLength - NameLength - 60).PadRight(lineLength - NameLength);
            }
            while (value.Length > lineLength - NameLength)
            {
                string check = value.Substring(0, lineLength - NameLength);
                int lastSpace = check.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    result = result + check.Substring(0, lastSpace).PadRight(lineLength - NameLength) + "\n" + "".PadLeft(NameLength);
                    value = value.Substring(lastSpace + 1);
                }
                else
                {
                    result = result + check + "\n" + "".PadLeft(lineLength - NameLength);
                    value = value.Substring(lineLength - NameLength);
                }
            }
            if (value.Length > 0)
            {
                result = result + value.PadRight(lineLength - NameLength);
            }
            return result;
        }

        private DateTime startRun;
        /// <summary>
        /// Adds additional information about the run at the bottom of a section.
        /// </summary>
        public bool AddRunTimeInformation = true;
        private string GetRunTimeInfo()
        {
            int halfLine = lineLength >> 1;
            string runInfo = "\n" 
                + "".PadLeft(lineLength, '_') + "\n"
                + "Run Time Information".PadRight(lineLength - 60, '*').PadLeft(lineLength, '*') + "\n"
                + ("Processor RunTime:= " + startRun.ToString(@"dddd, MMM dd yyyy <hh:mm:ss>")).PadRight(halfLine)
                + ("Run by:= " + System.Security.Principal.WindowsIdentity.GetCurrent().Name).PadLeft(halfLine>>1).PadRight(halfLine)
                + "\n";
            runInfo = runInfo
                + ("Analysis Records Created:= " + _results.Count).PadRight(halfLine)
                + ("Records Processed in File:= " + _RecordCount).PadLeft(halfLine >> 1).PadRight(halfLine) + "\n"
                + ("# Files Processed:= " + (1 + (ExtraFiles == null ? 0 : ExtraFiles.Length))).PadRight(halfLine)
                + "\n";
            runInfo = runInfo
                + ("Approximate Total Run Time:= " + System.DateTime.Now.Subtract(startRun).ToString(@"hh\:mm\:ss\.fff")).PadRight(lineLength)
                + "\n";
            runInfo = runInfo
                + "END [Run Time Information]".PadRight(lineLength - 60, '*').PadLeft(lineLength, '*')
                + "\n\n\n";
            return runInfo;
        }


    }          
}
