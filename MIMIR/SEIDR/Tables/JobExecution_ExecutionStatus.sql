CREATE TABLE [SEIDR].[JobExecution_ExecutionStatus] (
    [JobExecution_ExecutionStatusID] BIGINT        IDENTITY (1, 1) NOT NULL,
    [JobExecutionID]                 BIGINT        NOT NULL,
    [JobProfile_JobID]               INT           NULL,
    [StepNumber]                     SMALLINT      NOT NULL,
    [ExecutionStatusCode]            VARCHAR (2)   NOT NULL,
    [ExecutionStatusNameSpace]       VARCHAR (128) NOT NULL,
    [ExecutionStatus]                AS            (([ExecutionStatusNameSpace]+'.')+[ExecutionStatusCode]) PERSISTED NOT NULL,
    [ProcessingDate]                 DATE          NOT NULL,
    [FilePath]                       VARCHAR (250) NULL,
    [FileSize]                       BIGINT        NULL,
    [Success]                        BIT           DEFAULT ((1)) NOT NULL,
    [RetryCount]                     SMALLINT      NOT NULL,
    [DC]                             DATETIME      DEFAULT (getdate()) NOT NULL,
    [FileHash]                       VARCHAR (88)  NULL,
    [ExecutionTimeSeconds]           INT           NULL,
    [IsLatestForExecutionStep]       BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([JobExecution_ExecutionStatusID] ASC),
    FOREIGN KEY ([JobExecutionID]) REFERENCES [SEIDR].[JobExecution] ([JobExecutionID]),
    FOREIGN KEY ([JobProfile_JobID]) REFERENCES [SEIDR].[JobProfile_Job] ([JobProfile_JobID]),
    FOREIGN KEY ([ExecutionStatusNameSpace], [ExecutionStatusCode]) REFERENCES [SEIDR].[ExecutionStatus] ([NameSpace], [ExecutionStatusCode])
);

