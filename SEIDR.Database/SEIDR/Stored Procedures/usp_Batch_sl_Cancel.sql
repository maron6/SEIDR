CREATE PROCEDURE SEIDR.usp_Batch_sl_Cancel
	@batchSize int = 1
AS
BEGIN
	DECLARE @BatchList table(BatchID int)

	UPDATE TOP(@batchSize) b
	SET BatchStatusCode = 'SA', Locked = 1 --Ensure locked (Might already be locked if started)
	OUTPUT Inserted.BatchID into @BatchList
	FROM SEIDR.Batch b
	WHERE b.BatchStatusCode = 'SR'

	SELECT * FROM SEIDR.vw_Batch
	WHERE BatchID IN (SELECT BatchID FROM @BatchList)
END