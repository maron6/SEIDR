CREATE VIEW SEIDR.vw_Batch_Runnable
AS
	SELECT *
	FROM SEIDR.vw_Batch b
	CROSS APPLY(SELECT	COUNT(p.ParentProfileID) [ParentProfileCount], 
						COUNT(pb.BatchID) [QualifyingBatchCount]
				FROM SEIDR.BatchProfile_Profile p
				LEFT JOIN SEIDR.vw_Batch_ParentBatch pb
					ON pb.BatchID = b.BatchID
					AND pb.ParentBatchProfileID = p.ParentProfileID
				WHERE p.BatchProfileID = b.BatchProfileID)qp	
	WHERE Locked = 0
	AND Queued = 0 --Already applied to a thread then. (Note that this lock is cleaned up when the service starts up.)
	AND CanContinue = 1	
	AND (InSequence = 1	OR ForceOperationSequence = 1)
	AND EarliestNextQueueTime < GETDATE()
	AND (b.IgnoreParents = 1 OR qp.ParentProfileCount = qp.QualifyingBatchCount )
	AND FileCount >= MinFileCount	
