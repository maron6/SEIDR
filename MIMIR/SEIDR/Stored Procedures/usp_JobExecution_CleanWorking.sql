
CREATE PROCEDURE [SEIDR].[usp_JobExecution_CleanWorking]
as
BEGIN
	UPDATE SEIDR.JobExecution
	SET IsWorking = 0,
		InWorkQueue = 0
	WHERE Active = 1 AND (IsWorking = 1 or InWorkQueue = 1)
END

