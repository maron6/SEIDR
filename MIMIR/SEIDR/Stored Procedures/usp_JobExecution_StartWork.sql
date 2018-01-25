CREATE PROCEDURE SEIDR.usp_JobExecution_StartWork
	@JobExecutionID bigint
as
BEGIN
	UPDATE SEIDR.JobExecution
	SET IsWorking = 1,
		InWorkQueue = 0
	WHERE JobExecutionID = @JobExecutionID
	AND IsWorking = 0

	IF @@ROWCOUNT = 1
		SELECT * FROM SEIDR.vw_JobExecution WHERE JobExecutionID = @JobExecutionID
	else 
		return 50
	
	RETURN 0
END
