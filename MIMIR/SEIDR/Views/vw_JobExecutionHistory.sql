
CREATE VIEW SEIDR.vw_JobExecutionHistory
as
	SELECT jes.*
	FROM SEIDR.JobExecution_ExecutionStatus jes
	WHERE IsLatestForExecutionStep = 1
	AND EXISTS(SELECT null FROM SEIDR.JobExecution WHERE JobExecutionID = jes.JobExecutionID AND Active = 1)
