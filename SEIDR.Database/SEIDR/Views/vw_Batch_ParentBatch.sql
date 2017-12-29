CREATE VIEW SEIDR.vw_Batch_ParentBatch
AS
SELECT child.BatchProfileID, child.BatchID, 
	child.BatchStatusCode, child.BatchTypeCode, child.BatchDate,
	parent.BatchProfileID [ParentBatchProfileID], parent.BatchID [ParentBatchID], parent.IsError [ParentIsError],
	parent.BatchStatusCode [ParentBatchStatusCode], parent.BatchTypeCode [ParentBatchTypeCode],
	parent.BatchDate [ParentBatchDate]		
FROM SEIDR.vw_Batch child
JOIN SEIDR.BatchProfile_Profile link
	ON child.BatchProfileID = link.BatchProfileID
JOIN SEIDR.vw_Batch parent
	ON link.ParentProfileID = parent.BatchProfileID
	AND DATEDIFF(day, child.BatchDate, parent.BatchDate) = ParentingDayOffset
WHERE parent.IsComplete = 1
AND (parent.IsError = 0 OR link.AllowError = 1)


