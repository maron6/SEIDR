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
    [Export(typeof(IJob))]
    [ExportMetadata(nameof(IJobMetaData.JobName), nameof(FileSystemJob)),
        ExportMetadata(nameof(IJobMetaData.NameSpace), FS_NAMESPACE),
        ExportMetadata(nameof(IJobMetaData.Description), "File and directory management"),
        ExportMetadata(nameof(IJobMetaData.SingleThreaded), false)]
    public class FileSystemJob : IJob
    {
        const string FS_NAMESPACE = nameof(SEIDR.FileSystem);
        const string GET_EXECUTION_INFO = "SEIDR.usp_FileSystem_ss_JobExecution";

        public bool CheckThread(JobExecution jobCheck, int passedThreadID, out int NewThreadID)
        {
            NewThreadID = -1;
            return true;
        }

        public bool Execute(IJobExecutor jobExecutor, JobExecution execution, ref ExecutionStatus status)
        {
            //throw new NotImplementedException();
            var Manager = new DatabaseManager(jobExecutor.connection, DefaultSchema: "SEIDR");
            using(var h = Manager.GetBasicHelper())
            {
                h.QualifiedProcedure = GET_EXECUTION_INFO;
                h["JobProfile_JobID"] = execution.JobProfile_JobID;

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
                    else if (statCode != null)
                    {
                        status = new ExecutionStatus
                        {
                            NameSpace = FS_NAMESPACE,
                            IsError = false,
                            ExecutionStatusCode = statCode
                        };
                        switch (statCode)
                        {
                            case "AE":
                                status.Description = "Target File Already Exists.";
                                break;
                        }
                        if(status.Description != null)
                            jobExecutor.LogInfo("File Processing default success - " + status.Description);
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
