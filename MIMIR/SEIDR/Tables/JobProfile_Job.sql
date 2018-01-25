CREATE TABLE [SEIDR].[JobProfile_Job] (
    [JobProfile_JobID]           INT           IDENTITY (1, 1) NOT NULL,
    [JobProfileID]               INT           NOT NULL,
    [StepNumber]                 SMALLINT      NOT NULL,
    [JobID]                      INT           NOT NULL,
    [TriggerExecutionStatusCode] VARCHAR (2)   NULL,
    [TriggerExecutionNameSpace]  VARCHAR (128) NULL,
    [CanRetry]                   BIT           DEFAULT ((0)) NOT NULL,
    [RequiredThreadID]           TINYINT       NULL,
    [FailureNotificationMail]    VARCHAR (500) NULL,
    [RetryDelay]                 SMALLINT      NULL,
    [SequenceScheduleID]         INT           NULL,
    [DD]                         DATETIME      NULL,
    [Active]                     AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED NOT NULL,
    PRIMARY KEY CLUSTERED ([JobProfile_JobID] ASC),
    CHECK ([StepNumber]>(0)),
    CHECK ([TriggerExecutionStatusCode] IS NULL OR [StepNumber]>(1) AND (NOT ([TriggerExecutionStatusCode]='R' OR [TriggerExecutionStatusCode]='S') OR [TriggerExecutionStatusCode] IS NULL OR [TriggerExecutionNameSpace]<>'SEIDR' OR ([TriggerExecutionStatusCode]='R' OR [TriggerExecutionStatusCode]='S') AND [TriggerExecutionNameSpace] IS NULL) OR ([TriggerExecutionStatusCode]='R' OR [TriggerExecutionStatusCode]='S') AND [TriggerExecutionNameSpace]='SEIDR' AND [StepNumber]=(1)),
    FOREIGN KEY ([JobID]) REFERENCES [SEIDR].[Job] ([JobID]),
    FOREIGN KEY ([JobProfileID]) REFERENCES [SEIDR].[JobProfile] ([JobProfileID]),
    FOREIGN KEY ([SequenceScheduleID]) REFERENCES [SEIDR].[Schedule] ([ScheduleID]),
    FOREIGN KEY ([TriggerExecutionNameSpace], [TriggerExecutionStatusCode]) REFERENCES [SEIDR].[ExecutionStatus] ([NameSpace], [ExecutionStatusCode]),
    UNIQUE NONCLUSTERED ([JobProfileID] ASC, [StepNumber] ASC, [TriggerExecutionStatusCode] ASC, [TriggerExecutionNameSpace] ASC)
);

