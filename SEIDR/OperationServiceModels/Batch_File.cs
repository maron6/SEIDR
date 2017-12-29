using SEIDR.Doc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.OperationServiceModels
{
    public class Batch_File
    {        
        public int? Batch_FileID { get; private set; } = null;
        public int BatchID { get; set; }
        
        string _FilePath;
        /// <summary>
        /// Just the fileName of the Batch_File, rather than full path
        /// </summary>
        public string FileName { get; private set; }
        public string Directory { get; private set; }
        /// <summary>
        /// Full path of file.
        /// </summary>
        public string FilePath
        {
            get { return _FilePath; }
            set
            {
                _FilePath = value;
                FileName = System.IO.Path.GetFileName(value);
                Directory = System.IO.Path.GetDirectoryName(value);
            }
        }        
        public string FileHash { get; set; }
        public long? FileSize { get; set; }
        public DateTime FileDate { get; set; }
        
        public bool OperationSuccess { get; set; } = false;

        public string OriginalFilePath { get; private set; }
        public string OriginalDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(OriginalFilePath);
            }
        }
        /// <summary>
        /// Gets whether or not a file exists at the current FilePath specified
        /// </summary>
        public bool Exists
        {
            get { return System.IO.File.Exists(FilePath); }
        }
        public static Batch_File FromFileInfo(FileInfo file, DateTime? fileDate)
        {
            string Hash = file.GetFileHash();
            Batch_File f = new Batch_File
            {                
                FileDate = fileDate ?? file.CreationTime.Date,
                FileHash = Hash,
                FileSize = file.Length,
                FilePath = file.FullName,
                OriginalFilePath = file.FullName
            };         
            return f;
        }

        public static string ToXML(IEnumerable<Batch_File> fileList)
        {
            if (fileList.UnderMaximumCount(1))
                return null;

            string XML = "<BatchFiles>";
            foreach (var file in fileList)
            {
                string success = file.OperationSuccess ? "1" : "0";
                XML += $"<File Batch_FileID=\"{file.Batch_FileID}\" BatchID=\"{file.BatchID}\" FilePath=\"{file.FilePath}\" FileHash=\"{file.FileHash}\" FileSize=\"{file.FileSize}\" FileDate=\"{file.FileDate.ToString("MM/dd/yyyy")}\" OperationSuccess=\"{success}\" />";
            }
            XML += "</BatchFiles>";
            return XML;
        }


        public bool MoveTo(string FullPath)
        {
            if (!Exists)
                return false;            
            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(FullPath));
                File.Move(FilePath, FullPath);
            }
            catch (IOException)
            {
                return false;
            }
            FilePath = FullPath;
            return true;
        }
        public bool CopyTo(string FullPath, bool UpdatePath)
        {
            if (!Exists)
                return false;
            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(FullPath));
                File.Copy(FilePath, FullPath);
            }
            catch (IOException)
            {
                return false;
            }
            if(UpdatePath)
                FilePath = FullPath;
            return true;
        }
        public void CheckHash()
        {
            if(Exists)
                FileHash = Doc.DocExtensions.GetFileHash(new FileInfo(FilePath));
        }        
    }
}
