CREATE TABLE [SEIDR].[Batch_Error] (
    [Batch_ErrorID]       INT            IDENTITY (1, 1) NOT NULL,
    [BatchID]             INT            NOT NULL,
    [Batch_FileID]        INT            NULL,
    [Profile_OperationID] INT            NOT NULL,
    [Step]                SMALLINT       NOT NULL,
    [Message]             VARCHAR (6000) NOT NULL,
    [Extra]               VARCHAR (300)  NULL,
    [ThreadID]            TINYINT        NULL,
    PRIMARY KEY CLUSTERED ([Batch_ErrorID] ASC),
    CHECK ([ThreadID]>(0)),
    FOREIGN KEY ([Batch_FileID]) REFERENCES [SEIDR].[Batch_File] ([Batch_FileID]),
    FOREIGN KEY ([BatchID]) REFERENCES [SEIDR].[Batch] ([BatchID]),
    FOREIGN KEY ([Profile_OperationID]) REFERENCES [SEIDR].[Profile_Operation] ([Profile_OperationID])
);

