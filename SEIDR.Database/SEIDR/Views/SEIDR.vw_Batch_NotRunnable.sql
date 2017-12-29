CREATE VIEW [SEIDR].[vw_Batch_NotRunnable]
AS 
	SELECT b.BatchID, b.BatchDate, b.BatchProfileID, BatchStatusCode, b.BatchStatus, 
		b.FileCount, b.InSequence, qp.*, b.LastQueued,
		IIF(Locked = 1 or Queued = 1, 1, 0) LockExcluded,
		IIF(CanContinue = 0, 1, 0) StatusExcluded,
		IIF(InSequence = 0 AND ForceOperationSequence = 0 
			OR IgnoreParents = 0 AND ParentProfileCount > QualifyingBatchCount, 1, 0) SequenceExcluded,
		IIF(EarliestNextQueueTime > GETDATE(), 1, 0) FutureQueueTime,
		IIF(FileCount < MinFileCount, 1, 0) FileCountExcluded,
		Message = ''
			+ IIF(Locked = 1, 'Locked	', '')
			+ IIF(Queued = 1, 'Queued	', '')
			+ IIF(IgnoreParents = 0 AND ParentProfileCount > [QualifyingBatchCount], 'Missing Parent Batches	', '')
			+ IIF(CanContinue = 0, 'Status cannot Continue	', '')
			+ IIF(ForceOperationSequence = 0 AND InSequence = 0, 'BatchDate out of sequence	', '')
			+ IIF(EarliestNextQueueTime > GETDATE(), 'Queue time is in the future	', '')
			+ IIF(FileCount < MinFileCount, 'FileCount('+ CONVERT(varchar(10), FileCount) 
				+ ') too low for BatchType minimum (' + CONVERT(varchar(10), MinFileCount) + ')', '')
	FROM SEIDR.vw_Batch b
	CROSS APPLY(SELECT	COUNT(p.ParentProfileID) [ParentProfileCount], 
						COUNT(pb.BatchID) [QualifyingBatchCount]
				FROM SEIDR.BatchProfile_Profile p
				LEFT JOIN SEIDR.vw_Batch_ParentBatch pb
					ON pb.BatchID = b.BatchID
					AND pb.ParentBatchProfileID = p.ParentProfileID
				WHERE p.BatchProfileID = b.BatchProfileID)qp	
