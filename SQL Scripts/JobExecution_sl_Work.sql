USE MIMIR;
GO
CREATE PROCEDURE SEIDR.usp_JobExecution_sl_Work
	@ThreadID int,
	@BatchSize int = 5
as
BEGIN

	SELECT TOP (@BatchSize) *
	INTO #jobs
	FROM SEIDR.vw_JobExecution
	WHERE CanQueue = 1
	AND InSequence = 1
	AND (RequiredThreadID is null or RequiredThreadID = @ThreadID)
	ORDER BY RequiredThreadID desc, WorkQueueAge desc, [ProfilePriority] DESC, ProcessingDate asc, ExecutionPriority DESC


	UPDATE je
	SET InWorkQueue = 1
	FROM SEIDR.JobExecution je
	JOIN #jobs j
		ON je.JobExecutionID = j.JobExecutionID

	SELECT * FROM #jobs
END
