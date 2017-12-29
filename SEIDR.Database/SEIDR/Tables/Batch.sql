CREATE TABLE [SEIDR].[Batch] (
    [BatchID]                INT           IDENTITY (1, 1) NOT NULL,
    [BatchTypeCode]          VARCHAR (5)   NOT NULL,
    [BatchProfileID]         INT           NOT NULL,
    [BatchStatusCode]        VARCHAR (2)   NOT NULL,
    [BatchDate]              DATE          DEFAULT (getdate()) NOT NULL,
    [Priority]               TINYINT       NULL,
    [FileCount]              SMALLINT      DEFAULT ((0)) NOT NULL,
    [ForceOperationSequence] BIT           DEFAULT ((0)) NOT NULL,
    [CurrentStep]            SMALLINT      DEFAULT ((1)) NOT NULL,
    [Locked]                 BIT           DEFAULT ((1)) NOT NULL,
    [LastQueued]             SMALLDATETIME NULL,
    [OperationStartTime]     SMALLDATETIME NULL,
    [OperationEndTime]       SMALLDATETIME NULL,
    [OperationRunTime]       AS            (case when [OperationStartTime] IS NULL then NULL when [OperationEndTime] IS NULL then datediff(minute,[OperationStartTime],getdate()) else datediff(minute,[OperationStartTime],[OperationEndTime]) end),
    [DD]                     SMALLDATETIME NULL,
    [DC]                     SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [LU]                     SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [CreationHour]           AS            (datepart(hour,[DC])) PERSISTED,
    [CreationMinute]         AS            (datepart(minute,[DC])) PERSISTED,
    [Active]                 AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED,
    [ScheduleID]             INT           NULL,
    [AttemptCount]           SMALLINT      DEFAULT ((0)) NOT NULL,
    [Queued]                 BIT           DEFAULT ((0)) NOT NULL,
    [IgnoreParents]          BIT           DEFAULT ((0)) NOT NULL,
    [RepetitionGroupID]      INT           NULL,
    [LateRegistration]       BIT           NULL,
    PRIMARY KEY CLUSTERED ([BatchID] ASC),
    CHECK ([CurrentStep]>(0)),
    FOREIGN KEY ([BatchProfileID]) REFERENCES [SEIDR].[BatchProfile] ([BatchProfileID]),
    FOREIGN KEY ([BatchStatusCode]) REFERENCES [SEIDR].[BatchStatus] ([BatchStatusCode]),
    FOREIGN KEY ([BatchTypeCode]) REFERENCES [SEIDR].[BatchType] ([BatchTypeCode]),
    FOREIGN KEY ([RepetitionGroupID]) REFERENCES [SEIDR].[RepetitionGroup] ([RepetitionGroupID]),
    FOREIGN KEY ([ScheduleID]) REFERENCES [SEIDR].[Schedule] ([ScheduleID])
);


GO
CREATE TRIGGER SEIDR.trg_Batch_U
   ON  SEIDR.Batch
   AFTER UPDATE
AS 
BEGIN
	IF UPDATE(DD)
	BEGIN
		IF EXISTS(SELECT null
					FROM INSERTED i
					JOIN DELETED d
						ON i.BatchID = d.BatchID
						AND d.DD IS NULL
					WHERE i.DD IS NOT NULL
					AND d.Locked = 1)
		BEGIN
			RAISERROR('Cannot deactivate a locked batch.', 16, 1)
			ROLLBACK
			RETURN
		END
	END

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	IF UPDATE(BatchStatusCode)
	OR UPDATE(CurrentStep)
	BEGIN		
		/*
		--DEPRECATE. Don't use 'W'/'F', so just archive every time the status changes
		INSERT INTO SEIDR.Batch_BatchStatus(BatchID, BatchStatusCode, Step, OperationRunTime, AttemptCount )
		SELECT d.BatchID, d.BatchStatusCode, d.CurrentStep, DATEDIFF(minute, d.OperationStartTime, COALESCE(i.OperationEndTime, GETDATE()) ), d.AttemptCount
		FROM INSERTED i
		JOIN SEIDR.BatchStatus s
			ON i.BatchStatusCode = s.batchStatusCode
		JOIN DELETED d
			ON i.BatchID = d.BatchID
		JOIN SEIDR.BatchStatus ds
			ON d.BatchStatusCode = ds.BatchStatusCode
		WHERE ds.CanContinue <> s.CanContinue
		OR ds.IsComplete <> s.IsComplete
		OR d.CurrentStep <> i.CurrentStep				
		*/
		INSERT INTO SEIDR.Batch_BatchStatus
		(
			BatchID, 
			BatchStatusCode, 
			Step, 
			OperationRunTime, 
			AttemptCount 
		)
		SELECT 
			d.BatchID, 
			d.BatchStatusCode, 
			d.CurrentStep, 
			DATEDIFF(minute, d.OperationStartTime, COALESCE(i.OperationEndTime, GETDATE()) ), 
			d.AttemptCount
		FROM INSERTED i		
		JOIN DELETED d
			ON i.BatchID = d.BatchID
		WHERE i.BatchStatusCode <> d.BatchStatusCode OR i.BatchStatusCode<> d.BatchStatusCode
		--Not using working/failure statuses now, so archive any status/Step change change


		UPDATE b
		SET AttemptCount = 0 --New step/Status, replace the Attempt Counter
		FROM SEIDR.Batch b
		JOIN INSERTED i
			ON b.BatchID = i.BatchID
		JOIN DELETED d
			ON b.BatchID = d.BatchID
		WHERE i.CurrentStep <> d.CurrentStep OR i.BatchStatusCode <> d.BatchStatusCode
	--SELECT * FROM SEIDR.Batchstatus
    -- Insert statements for trigger here
	END

END
