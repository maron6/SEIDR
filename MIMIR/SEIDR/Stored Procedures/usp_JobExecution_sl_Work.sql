CREATE PROCEDURE [SEIDR].[usp_JobExecution_sl_Work]
	@ThreadID int,
	@ThreadCount int,
	@BatchSize int = 5
as
BEGIN

	BEGIN TRAN

	SELECT TOP (@BatchSize) *
	INTO #jobs
	FROM SEIDR.vw_JobExecution
	WHERE CanQueue = 1
	AND InSequence = 1
	AND (
		RequiredThreadID is null 
		or (RequiredThreadID % @ThreadCount) + 1 = @ThreadID  --Mod is 0 based, ThreadID 1 based
		)
	ORDER BY RequiredThreadID desc, WorkPriority desc, ProcessingDate asc, ExecutionPriority DESC

	SET @BatchSize = @@ROWCOUNT

	UPDATE je
	SET InWorkQueue = 1
	FROM SEIDR.JobExecution je
	JOIN #jobs j
		ON je.JobExecutionID = j.JobExecutionID
	WHERE InWorkQueue = 0
	
	IF @@ROWCOUNT <> @BatchSize
	BEGIN		
		ROLLBACK
		RETURN 50
	END

	COMMIT

	SELECT * FROM #jobs
	
END
