CREATE TABLE [SEIDR].[JobExecution] (
    [JobExecutionID]               BIGINT        IDENTITY (1, 1) NOT NULL,
    [JobProfileID]                 INT           NOT NULL,
    [UserKey]                      INT           NULL,
    [UserKey1]                     VARCHAR (50)  NULL,
    [UserKey2]                     VARCHAR (50)  NULL,
    [StepNumber]                   SMALLINT      DEFAULT ((1)) NOT NULL,
    [ExecutionStatusCode]          VARCHAR (2)   NOT NULL,
    [ExecutionStatusNameSpace]     VARCHAR (128) DEFAULT ('SEIDR') NOT NULL,
    [ExecutionStatus]              AS            (([ExecutionStatusNameSpace]+'.')+[ExecutionStatusCode]) PERSISTED NOT NULL,
    [FilePath]                     VARCHAR (250) NULL,
    [FileSize]                     BIGINT        NULL,
    [ProcessingDate]               DATE          DEFAULT (getdate()) NOT NULL,
    [ForceSequence]                BIT           DEFAULT ((0)) NOT NULL,
    [RetryCount]                   SMALLINT      DEFAULT ((0)) NOT NULL,
    [LastExecutionStatusCode]      VARCHAR (2)   NULL,
    [LastExecutionStatusNameSpace] VARCHAR (128) NULL,
    [LastExecutionStatus]          AS            (([LastExecutionStatusNameSpace]+'.')+[LastExecutionStatusCode]) PERSISTED,
    [DC]                           DATETIME      DEFAULT (getdate()) NOT NULL,
    [LU]                           DATETIME      DEFAULT (getdate()) NOT NULL,
    [DD]                           DATETIME      NULL,
    [Active]                       AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)),
    [IsWorking]                    BIT           DEFAULT ((0)) NOT NULL,
    [FileHash]                     VARCHAR (88)  NULL,
    [JobPriority]                  VARCHAR (10)  DEFAULT ('NORMAL') NOT NULL,
    [InWorkQueue]                  BIT           DEFAULT ((0)) NOT NULL,
    [ProcessingTime]               TIME (7)      NULL,
    [ProcessingDateTime]           AS            (coalesce(CONVERT([datetime],[ProcessingDate])+CONVERT([datetime],[ProcessingTime]),[ProcessingDate])) PERSISTED NOT NULL,
    [ExecutionTimeSeconds]         INT           NULL,
    [ScheduleRuleClusterID]        INT           NULL,
    [PrioritizeNow] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([JobExecutionID] ASC),
    CHECK ([StepNumber]>(0)),
    FOREIGN KEY ([JobPriority]) REFERENCES [SEIDR].[Priority] ([PriorityCode]),
    FOREIGN KEY ([JobProfileID]) REFERENCES [SEIDR].[JobProfile] ([JobProfileID]),
    FOREIGN KEY ([ScheduleRuleClusterID]) REFERENCES [SEIDR].[ScheduleRuleCluster] ([ScheduleRuleClusterID]),
    FOREIGN KEY ([ExecutionStatusNameSpace], [ExecutionStatusCode]) REFERENCES [SEIDR].[ExecutionStatus] ([NameSpace], [ExecutionStatusCode]),
    FOREIGN KEY ([LastExecutionStatusNameSpace], [LastExecutionStatusCode]) REFERENCES [SEIDR].[ExecutionStatus] ([NameSpace], [ExecutionStatusCode])
);


GO
CREATE TRIGGER SEIDR.trg_JobExecution_iu
ON SEIDR.JobExecution after INSERT, UPDATE
AS
BEGIN	

	IF UPDATE(ExecutionStatusCode) OR UPDATE(ExecutionStatusNameSpace) OR UPDATE(FilePath) OR UPDATE(StepNumber)
	BEGIN

		INSERT INTO SEIDR.JobExecution_ExecutionStatus(JobExecutionID, JobProfile_JobID, 
			StepNumber, ExecutionStatusCode, ExecutionStatusNameSpace, ExecutionTimeSeconds,
			FilePath, FileSize, FileHash, RetryCount, Success, ProcessingDate)
		SELECT d.JobExecutionID, jpj.JobProfile_JobID, 
			d.StepNumber, d.ExecutionStatusCode, d.ExecutionStatusNameSpace, d.ExecutionTimeSeconds,
			d.FilePath, d.FileSize, d.FileHash, d.RetryCount, 1 - s.IsError, d.ProcessingDate
		FROM DELETED d
		LEFT JOIN SEIDR.JobProfile_Job jpj	
			ON d.JobProfileID = jpj.JobProfileID
			AND d.StepNumber = jpj.StepNumber
			AND (jpj.TriggerExecutionNameSpace is null or d.ExecutionStatusNameSpace = jpj.TriggerExecutionNameSpace)
			AND (jpj.TriggerExecutionStatusCode is null or d.ExecutionStatusCode = jpj.TriggerExecutionStatusCode)
		JOIN INSERTED i
			ON d.JobExecutionID = i.JobExecutionID					
		JOIN SEIDR.ExecutionStatus s		 
			ON i.ExecutionStatusNameSpace = s.[NameSpace]
			AND i.ExecutionStatusCode = s.ExecutionStatusCode
			--AND s.IsWorking = 0
		WHERE d.StepNumber <> i.StepNumber --If going from  x -> Working -> x, step number or FilePath needs to have changed as well. Caught by not being 'AND' relations
		OR d.ExecutionStatus <> i.ExecutionStatus and (d.LastExecutionStatus is null or i.ExecutionStatus <> d.LastExecutionStatus) --d.ExecutionStatusCode <> i.ExecutionStatusCode OR d.ExecutionStatusNameSpace <> i.ExecutionStatusNameSpace
		OR d.FilePath <> i.FilePath --don't care about going to/from null unless the status/step is also changing.
		

		INSERT INTO SEIDR.JobExecution_ExecutionStatus(JobExecutionID, JobProfile_JobID, 
			StepNumber, ExecutionStatusCode, ExecutionStatusNameSpace, ExecutionTimeSeconds,
			FilePath, FileSize, FileHash, RetryCount, Success, ProcessingDate)
		SELECT i.JobExecutionID, jpj.JobProfile_JobID, 
			i.StepNumber, i.ExecutionStatusCode, i.ExecutionSTatusNameSpace, i.ExecutionTimeSeconds,
			i.FilePath, i.FileSize, i.FileHash, i.RetryCount, 1 - s.IsError, i.ProcessingDate
		FROM INSERTED i
		JOIN SEIDR.ExecutionStatus s
			ON i.ExecutionStatusNameSpace = s.[NameSpace]
			AND i.ExecutionStatusCode = s.ExecutionStatusCode
		LEFT JOIN SEIDR.JobProfile_Job jpj	
			ON i.JobProfileID = jpj.JobProfileID
			AND i.StepNumber = jpj.StepNumber
			AND (jpj.TriggerExecutionNameSpace is null or i.ExecutionStatusNameSpace = jpj.TriggerExecutionNameSpace)
			AND (jpj.TriggerExecutionStatusCode is null or i.ExecutionStatusCode = jpj.TriggerExecutionStatusCode)
		JOIN DELETED d
			ON d.JobExecutionID = i.JobExecutionID			
		WHERE s.IsComplete = 1 
		AND (d.ExecutionStatusCode <> i.ExecutionStatusCode 
			OR d.ExecutionStatusNameSpace <> i.ExecutionStatusNameSpace 
			or d.StepNumber <> i.StepNumber --Shouldn't really happen if the status is the same and Complete...Check anyway, though, since it affects the jpj join
			) 

		;WITH CTE AS(SELECT IsLatestForExecutionStep, ROW_NUMBER() OVER (PARTITION BY JobExecutionID, StepNumber ORDER BY DC DESC) rn
					FROM SEIDR.JobExecution_ExecutionStatus 
					WHERE JobExecutionID IN (SElECT JobExecutionID FROM INSERTED)
					AND IsLatestForExecutionStep = 1
		)
		UPDATE cte
		SET IsLatestForExecutionStep= 0
		WHERE rn > 1

	END	

	IF NOT UPDATE(LU)
		UPDATE je
		SET LU = GETDATE()
		FROM SEIDR.JobExecution je
		JOIN DELETED d
			ON je.JobExecutionID = d.JobExecutionID
		WHERE je.ExecutionStatus <> d.ExecutionStatus
		OR je.IsWorking <> d.IsWorking
		OR je.InWorkQueue <> d.InWorkQueue
		or je.StepNumber <> d.StepNumber
		or je.FilePath <> d.FilePath
		or je.ForceSequence <> d.ForceSequence
END
