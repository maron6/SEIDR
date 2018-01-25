CREATE TABLE [SEIDR].[JobExecutionError] (
    [LogID]            INT            IDENTITY (1, 1) NOT NULL,
    [JobExecutionID]   BIGINT         NOT NULL,
    [StepNumber]       SMALLINT       NOT NULL,
    [JobProfile_JobID] INT            NULL,
    [ThreadID]         SMALLINT       NULL,
    [ErrorDescription] VARCHAR (4000) NOT NULL,
    [ExtraID]          INT            NULL,
    [DC]               SMALLDATETIME  DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([LogID] ASC),
    FOREIGN KEY ([JobExecutionID]) REFERENCES [SEIDR].[JobExecution] ([JobExecutionID]),
    FOREIGN KEY ([JobProfile_JobID]) REFERENCES [SEIDR].[JobProfile_Job] ([JobProfile_JobID])
);

