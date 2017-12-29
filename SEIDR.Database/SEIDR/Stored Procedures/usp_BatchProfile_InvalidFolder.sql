CREATE PROCEDURE SEIDR.usp_BatchProfile_InvalidFolder
	@BatchProfileID int
AS
	UPDATE SEIDR.BatchProfile
	SET InputFolder = '*INVALID*' + InputFolder
	WHERE BatchProfileID = @BatchProfileID