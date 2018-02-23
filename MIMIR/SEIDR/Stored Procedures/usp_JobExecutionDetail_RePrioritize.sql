CREATE PROCEDURE [SEIDR].[usp_JobExecutionDetail_RePrioritize]	
	@BatchSize int = 5
AS
BEGIN
	
	CREATE TABLE #ExecutionIDList(JobExecutionID bigint primary key)

	UPDATE TOP (@BatchSize) je
	SET PrioritizeNow = 0,
		JobPriority = np.PriorityCode
	OUTPUT INSERTED.JobExecutionID INTO #ExecutionIDList(JobExecutionID)
	FROM SEIDR.JobExecution je
	JOIN SEIDR.[Priority] p
		ON je.JobPriority = p.PriorityCode
	CROSS APPLY(SELECT TOP 1 *
				FROM SEIDR.[Priority]
				WHERE PriorityValue >= p.PriorityValue
				ORDER BY PriorityValue DESC)np
	WHERE InWorkQueue = 1
	AND PrioritizeNow = 1

	SELECT * 
	FROM SEIDR.vw_JobExecution
	WHERE JobExecutionID IN (SELECT JobExecutionID FROM #ExecutionIDList) --Remove any DelayStart, increase workPriority

	RETURN 0
END