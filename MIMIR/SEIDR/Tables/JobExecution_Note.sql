CREATE TABLE [SEIDR].[JobExecution_Note] (
    [ID]               BIGINT         IDENTITY (1, 1) NOT NULL,
    [JobExecutionID]   BIGINT         NOT NULL,
    [StepNumber]       SMALLINT       NOT NULL,
    [JobProfile_JobID] INT            NULL,
    [NoteText]         VARCHAR (2000) NOT NULL,
    [DC]               DATETIME       DEFAULT (getdate()) NOT NULL,
    [UserName]         VARCHAR (128)  DEFAULT (suser_name()) NOT NULL,
    [SUID]             BIGINT         NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    FOREIGN KEY ([JobExecutionID]) REFERENCES [SEIDR].[JobExecution] ([JobExecutionID]),
    FOREIGN KEY ([JobProfile_JobID]) REFERENCES [SEIDR].[JobProfile_Job] ([JobProfile_JobID])
);

