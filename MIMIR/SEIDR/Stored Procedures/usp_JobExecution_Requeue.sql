CREATE PROCEDURE SEIDR.usp_JobExecution_Requeue
	@JobExecutionID bigint
AS
BEGIN
	
	UPDATE SEIDR.JobExecution
	SET IsWorking =0 
	WHERE JobExecutionID = @JobExecutionID
	/*
	UPDATE je
	SET ExecutionStatusCode = n.ExecutionStatusCode,
		ExecutionStatusNameSpace = n.[NameSpace],
		LastExecutionStatusCode = l.ExecutionStatusCode,
		LastExecutionStatusNameSpace = l.[NameSpace],
	FROM SEIDR.JobExecution je
	JOIN SEIDR.ExecutionStatus n
		ON je.LastExecutionStatusCode = n.ExecutionStatusCode
		AND je.LastExecutionStatusNameSpace = n.[NameSpace]
	JOIN SEIDR.ExecutionStatus l
		ON je.ExecutionStatusCode = l.ExecutionStatusCode
		AND je.ExecutionSTatusNameSpace = l.[NameSpace]
	WHERE je.JobExecutionID = @JobExecutionID
	AND n.IsWorking = 0 AND n.IsError = 0 and n.IsComplete = 0
	AND l.IsWorking = 1
	*/
	
	SELECT * FROM SEIDR.vw_JobExecution WHERE JobExecutionID = @JobExecutionID AND CanQueue = 1

END