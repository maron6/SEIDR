CREATE TABLE [SEIDR].[Batch_BatchStatus] (
    [Batch_BatchStatusID] INT           IDENTITY (1, 1) NOT NULL,
    [BatchID]             INT           NOT NULL,
    [BatchStatusCode]     VARCHAR (2)   NOT NULL,
    [OperationRunTime]    INT           NULL,
    [Step]                SMALLINT      NOT NULL,
    [DC]                  SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [AttemptCount]        SMALLINT      NOT NULL,
    PRIMARY KEY CLUSTERED ([Batch_BatchStatusID] ASC),
    FOREIGN KEY ([BatchID]) REFERENCES [SEIDR].[Batch] ([BatchID]),
    FOREIGN KEY ([BatchStatusCode]) REFERENCES [SEIDR].[BatchStatus] ([BatchStatusCode])
);

