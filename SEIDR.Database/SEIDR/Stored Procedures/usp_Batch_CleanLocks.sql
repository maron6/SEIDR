CREATE PROCEDURE SEIDR.usp_Batch_CleanLocks
AS
BEGIN
	UPDATE SEIDR.Batch WITH (READPAST)
		SET Locked = 0,
			Queued = 0
	WHERE (Locked = 1 OR Queued = 1)
		AND Active = 1 
END
