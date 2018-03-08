
CREATE PROCEDURE [SEIDR].[usp_JobExecution_UnWork]
	@JobExecutionID bigint
as
BEGIN
	UPDATE SEIDR.JobExecution
	SET IsWorking = 0,
		InWorkQueue = 1
	WHERE JobExecutionID = @JobExecutionID	
END