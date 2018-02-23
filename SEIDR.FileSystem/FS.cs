using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SEIDR.OperationServiceModels;
using System.ComponentModel.Composition;
using SEIDR.DataBase;
using System.IO;
using SEIDR.JobBase;

namespace SEIDR.FileSystem
{
    
    public partial class FS 
    {

        internal FileOperation Operation { get; set; }
        public string Source { get; set; }
        public string Filter { get; set; }
        public string Destination { get; set; }   
        public bool IgnoreFileDate { get; set; }
        string ReplaceStar(string fullPath, string sourceFileName)
        {
            if (fullPath.EndsWith(@"\"))
                return fullPath + sourceFileName;

            string name = Path.GetFileName(fullPath);            
            if (name.Contains("."))
            {
                string[] nameParts = name.Split('.');
                nameParts[0] = nameParts[0].Replace("*", Path.GetFileNameWithoutExtension(sourceFileName));
                nameParts[1] = nameParts[1].Replace("*", Path.GetExtension(sourceFileName));
                fullPath = Path.Combine(Path.GetDirectoryName(fullPath), nameParts[0], nameParts[1]);
            }
            else
            {
                fullPath = fullPath.Replace("*", sourceFileName);
            }
            return fullPath;
        }
        /// <summary>
        /// Call this in a try/catch. Set status code differently depending on error...
        /// </summary>
        /// <param name="jobExecution"></param>
        /// <param name="StatusCode"></param>
        /// <returns></returns>
        public bool Process(JobProfile profile, 
            JobExecution jobExecution, DatabaseManager manager, 
            out string StatusCode)
        {
            StatusCode = null;
            if (string.IsNullOrWhiteSpace(Filter))
                Filter = "*.*";
            DateTime processingDate = jobExecution.ProcessingDate;            
            Source = ApplyDateMask(Source, processingDate);
            Destination = ApplyDateMask(Destination, processingDate);
            switch (Operation)
            {
                case FileOperation.CREATEDIR:
                    {
                        DirectoryInfo di = new DirectoryInfo(Source);
                        if (!di.Root.Exists)
                            return false;
                        else if (!di.Exists)
                        {
                            Directory.CreateDirectory(Source);
                        }
                        break;
                    }
                case FileOperation.GRAB:
                case FileOperation.MOVE:
                case FileOperation.COPY:
                case FileOperation.TAG:
                    {
                        FileInfo fi = new FileInfo(Source);
                        if (fi.Exists)
                        {
                            RegistrationFile r = new RegistrationFile(profile, fi)
                            {
                                StepNumber = jobExecution.StepNumber
                            };
                            string dest = Source;
                            if(Destination != null)
                                dest = ReplaceStar(Destination, fi.Name);

                            if (Operation.In(FileOperation.GRAB, FileOperation.MOVE))
                                r.Register(manager, dest, Source);
                            else
                            {
                                r.CopyRegister(manager, dest, Source);
                                if (Operation == FileOperation.TAG)
                                    File.AppendAllText(dest, Environment.NewLine + fi.Name);
                            }

                            break;
                        }
                        return false;
                    }
                case FileOperation.GRAB_ALL:                    
                    {
                        DirectoryInfo di = new DirectoryInfo(Source);
                        if (!di.Exists)
                        {
                            StatusCode = "NS";
                            break;
                        }
                        var files = di.GetFiles(Filter);
                        foreach(var file in files)
                        {
                            string dest = Path.Combine(Destination, file.Name);
                            RegistrationFile r = new RegistrationFile(profile, file)
                            {
                                StepNumber = jobExecution.StepNumber
                            };
                            r.Register(manager, dest, file.FullName);
                        }
                        break;
                    }
                case FileOperation.CHECK:
                case FileOperation.EXIST:
                case FileOperation.DELETE:                
                    {
                        if (!File.Exists(Source))
                            return false;
                        if (Operation == FileOperation.DELETE)
                            File.Delete(Source);
                        break;
                    }
                case FileOperation.MOVEDIR:
                case FileOperation.COPYDIR:
                    {                        
                        DirectoryInfo di = new DirectoryInfo(Source);
                        if (!di.Exists)
                        {
                            StatusCode = "ND";
                            break;
                        }
                        if (string.IsNullOrWhiteSpace(Destination))
                        {
                            StatusCode = "BD";
                            return false;
                        }
                        DirectoryInfo dest = new DirectoryInfo(Destination);
                        if (dest.Exists)
                        {
                            var f = di.GetFiles();                            
                            foreach(var file in f)
                            {
                                string fileDest = Path.Combine(Destination, file.Name);
                                if (Operation == FileOperation.MOVEDIR)
                                {
                                    file.MoveTo(fileDest);
                                }
                                else
                                {
                                    file.CopyTo(fileDest, true);
                                }
                            }
                            break;
                        }
                        else if(Operation == FileOperation.MOVEDIR)
                        {
                            Directory.Move(Source, Destination);
                            break;
                        }
                        else
                        {
                            Directory.CreateDirectory(Destination);
                            var f = di.GetFiles();
                            foreach(var file in f)
                            {                                
                                file.CopyTo(Destination, true);
                            }
                            break;
                        }
                    }
            }
            
            return true;
        }        
    }
}
