
CREATE PROCEDURE SEIDR.usp_Batch_StartWork
	@BatchID int
AS
BEGIN
	UPDATE SEIDR.Batch
	--SET BatchStatusCode = 'W', LU = GETDATE()
	SET Locked = 1, --Don't use 'W'/ 'F', use locked, retry counter
		Queued = 0, --NoLonger Queued - starting work.
		AttemptCount += 1,
		OperationStartTime = GETDATE(), 
		OperationEndTime = null,
		LU = GETDATE() ,
		Priority = null
	WHERE BatchID = @BatchID
	AND Locked = 0
	--This record is probably already Queued = 1, but we don't really care if it's Queued = 0
END
