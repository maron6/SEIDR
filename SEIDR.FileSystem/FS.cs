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

namespace SEIDR.FileSystem
{
    /*
    [   ExportMetadata("Operation", "FileSystem"),
        ExportMetadata("Description", "Operation for moving files to a destination"),
        ExportMetadata("Version", 1),
        ExportMetadata("ParameterSelect", "usp_SEIDR_FS_Parameter_SL")]*/
    public partial class FS : iOperation
    {
        IOperatorManager mgr;
        public IOperatorManager Manager
        {
            set { mgr = value; }
        }


        public bool Execute(Batch b, DataSet parameters, ref string BatchStatus)
        {

            if (b.Files.Count() == 0)
            {
                BatchStatus = BATCHSTATUS.SKIP_STEP;
                return true;
            }
            #region parameters/validation of set up
            FSParameterModel m = null;
            try
            {                
                m = parameters.ToContentRecord<FSParameterModel>();
            }            
            catch
            {
                mgr.LogBatchError(b, "Unable to set up paramters", "SEIDR.FileSystem");
                BatchStatus = BATCHSTATUS.INVALID_STOP;
                return false;
            }
            if(string.IsNullOrWhiteSpace(m.DestinationFolder))
            {
                mgr.LogBatchError(b, "Invalid DestinationFolder", "SEIDR.FileSystem Parameter Evaluation");
                BatchStatus = BATCHSTATUS.INVALID_STOP;
                return false;
            }
            if(m.FileOperation == null)
            {
                mgr.LogBatchError(b, "Invalid File Operation (Command)", "SEIDR.FileSystem Parameter Evaluation");
                BatchStatus = BATCHSTATUS.INVALID_STOP;
                return false;
            }
            if (string.IsNullOrWhiteSpace(m.DestinationFileNameFormat))
                m.DestinationFileNameFormat = "*";            
            #endregion

            bool GRAB = m.FileOperation.In(FileOperation.GRAB, FileOperation.GRAB_ALL, FileOperation.CREATEDIR);
            if (m.UseBatchDate || GRAB)
            {
                m.DestinationFolder = ApplyDateMask(m.DestinationFolder, b.BatchDate);
                m.DestinationFileNameFormat = ApplyDateMask(m.DestinationFileNameFormat, b.BatchDate);
            }
            #region GRAB, Create Directory
            if(GRAB)
            {
                if(m.FileOperation == FileOperation.CREATEDIR)
                {
                    try
                    {
                        Directory.CreateDirectory(m.DestinationFolder);
                        return true;
                    }
                    catch (IOException ex)
                    {
                        mgr.LogBatchError(b, ex.Message, "CREATE DIR");
                        return false;
                    }
                }   
                if (m.FileOperation == FileOperation.GRAB)
                {
                    FileInfo f = new FileInfo(
                        Path.Combine(m.DestinationFolder, m.DestinationFileNameFormat));
                    if (!f.Exists)
                        return false;

                    DateTime d;
                    if (!string.IsNullOrWhiteSpace(m.DateFormat))
                        d = mgr.ParseNameDate(f, m.DateFormat, 0);
                    else
                        d = b.BatchDate;
                    b.AddFile(f, d);
                    return true;
                }
                else
                {
                    DirectoryInfo di = new DirectoryInfo(m.DestinationFolder);
                    if (string.IsNullOrWhiteSpace(m.GrabAllFilter))
                        m.GrabAllFilter = "*.*";
                    var fs = di.EnumerateFiles(m.GrabAllFilter, SearchOption.TopDirectoryOnly);
                    foreach(var f in fs)
                    {
                        DateTime d;
                        if (!string.IsNullOrWhiteSpace(m.DateFormat))
                            d = mgr.ParseNameDate(f, m.DateFormat, 0);
                        else
                            d = b.BatchDate;
                        b.AddFile(f, d);                        
                    }
                    return true;
                }
            }
            #endregion

            bool result = true;
            b.Files
                .Where(f=> f.OperationSuccess == false)
                .ForEach(f =>
            {
                bool r = false;
                try
                {
                    if(m.FileOperation == FileOperation.DELETE)
                    {
                        if (f.Exists)
                            File.Delete(f.FilePath);
                        b.DeleteFile(f.FilePath);
                        r = true;
                    }
                    else
                        r = HandleBatchFile(f, m);
                }
                catch(IOException) { r = false; }
                if (!r)
                    result = false;
            });
            return result;
        }
        bool HandleBatchFile(Batch_File f, FSParameterModel parameters)
        {
            string pathNow = f.FilePath;
            string DirNow = f.Directory;
            string Name = f.FileName;

            string temp = Path.GetTempFileName();
            string fn = ApplyDateMask(parameters.DestinationFileNameFormat, f.FileDate)
                .Replace("*", f.FileName);
            string fd = ApplyDateMask(parameters.DestinationFolder, f.FileDate);
            FileOperation op = parameters.FileOperation.Value;
            switch (op)
            {
                case FileOperation.CHECK:
                case FileOperation.EXIST:
                    {
                        f.OperationSuccess = f.Exists;
                        break;
                    }
                case FileOperation.TAG:
                    {                        
                        f.FilePath = Path.Combine(fd, fn);
                        File.Copy(pathNow, temp);
                        File.AppendAllText(temp, Name);
                        File.Move(temp, f.FilePath);
                        f.CheckHash();
                        f.OperationSuccess = true;                        
                        return true;
                    }                
                case FileOperation.COPYDIR:
                case FileOperation.MOVEDIR:
                    {
                        op -= 2;
                        fn = Name;
                        break;                        
                    }                                        
            }
            switch (op)
            {
                case FileOperation.COPY:
                    {
                        bool x = f.CopyTo(Path.Combine(fd, fn), UpdatePath: true);
                        f.OperationSuccess = x;
                        return x;
                    }
                case FileOperation.MOVE:
                    {                        
                        bool x = f.MoveTo(Path.Combine(fd, Name));
                        f.OperationSuccess = x;
                        return x;                        
                    }
            }            
            //If unable to move file or operation somehow wasn't accounted for... return false.
            //Note that this method is called after already filtering out Successful file operations
            return f.OperationSuccess;
        }

        public string GetResultNotification(bool ExecuteResult, string BatchStatus)
        {
            if (BatchStatus == BATCHSTATUS.SKIP_STEP)
                return "No Work found";
            if (BatchStatus == BATCHSTATUS.INVALID_STOP)
                return "Invalid Parameter Set up. BatchErrors should be reviewed and the settings corrected";
            return null;
        }
    }
}
