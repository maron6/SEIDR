CREATE TABLE [SEIDR].[FileSystemJob]
(
	[FileSystemJobId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY, 
    [JobProfile_JobID] INT NOT NULL FOREIGN KEY REFERENCES SEIDR.JobProfile_Job(JobProfile_JobID), 
    [Source] VARCHAR(300) NULL, 
    [Destination] VARCHAR(300) NULL, 
    [IgnoreFileDate] BIT NOT NULL DEFAULT 0, 
    [Filter] VARCHAR(30) NULL, 
    [Operation] VARCHAR(30) NOT NULL DEFAULT ('COPY') FOREIGN KEY REFERENCES SEIDR.FileSystemOperation(FileSystemOperationCode), 
    [RegisterNewFile] BIT NOT NULL DEFAULT 1
)
