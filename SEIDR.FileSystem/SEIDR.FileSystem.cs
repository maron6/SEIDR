using SEIDR.JobBase;
using SEIDR.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace SEIDR.FileSystem
{
    [ExportMetadata("JobName", "FileSystem"),
        ExportMetadata("NameSpace", "SEIDR_FS"),
        ExportMetadata("Description", "File and directory management"),
        ExportMetadata("SingleThreaded", false)]
    public class FileSystem : IJob
    {
        const string FS_NAMESPACE = "SEIDR_FS";
        const string GET_EXECUTION_INFO = "SEIDR.usp_FileSystem_ss_JobExecution";
        public bool Execute(IJobExecutor jobExecutor, JobExecution execution, ref ExecutionStatus status)
        {
            //throw new NotImplementedException();
            var Manager = new DatabaseManager(jobExecutor.connection, DefaultSchema: "SEIDR");
            using(var h = Manager.GetBasicHelper())
            {
                h.QualifiedProcedure = GET_EXECUTION_INFO;

                var fs = Manager.Execute(h).GetFirstRowOrNull().ToContentRecord<FS>();
                if (fs == null)
                    return false;
                string statCode = null;
                bool success;
                try
                {
                    success = fs.Process(jobExecutor.job, execution, Manager, out statCode);
                    if (!success)
                    {
                        status = new ExecutionStatus
                        {
                            NameSpace = FS_NAMESPACE,
                            IsError = true,
                            ExecutionStatusCode = statCode
                        };
                        switch (statCode)
                        {
                            case "BD":
                                status.Description = "Bad Destination";
                                break;
                            case "NS":
                                status.Description = "No Source File/Directory";
                                break;
                            case "ND":
                                status.Description = "No Destination File/Directory";
                                break;
                        }
                        jobExecutor.LogInfo($"File Processing failure: StatusCode '{statCode}'");
                    }
                    return success;
                }
                catch (System.IO.IOException iex)
                {
                    status = new ExecutionStatus
                    {
                        NameSpace = FS_NAMESPACE,
                        ExecutionStatusCode = "IO",
                        Description = "IO Exception",
                        IsError = true,
                        IsComplete = false,
                        IsWorking = false
                    };
                    jobExecutor.LogError("File Processing, IO Exception", iex);
                    return false;
                }
                catch(Exception ex)
                {
                    jobExecutor.LogError("File Processing Exception", ex);
                    return false;
                }
            }
            //Status Codes: BD (Bad Destination), NS (No Source), ND (No Destination), IO (I/O Exception)
        }
    }
}
