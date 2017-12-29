CREATE TABLE [SEIDR].[Batch_File_History] (
    [Batch_File_HistoryID] INT            IDENTITY (1, 1) NOT NULL,
    [Batch_FileID]         INT            NOT NULL,
    [FilePath]             VARCHAR (260)  NOT NULL,
    [FileName]             VARCHAR (260)  NULL,
    [FileHash]             VARCHAR(88) NULL,
    [DeleteDate]           SMALLDATETIME  NULL,
    PRIMARY KEY CLUSTERED ([Batch_File_HistoryID] ASC),
    FOREIGN KEY ([Batch_FileID]) REFERENCES [SEIDR].[Batch_File] ([Batch_FileID])
);

