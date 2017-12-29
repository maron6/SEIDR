
CREATE VIEW [SEIDR].[vw_Batch]
AS
	SELECT 
	 b.BatchID
	,b.BatchTypeCode
	,b.BatchProfileID
	,b.Priority	
	,b.FileCount
	,bp.MinFileCount	
	,b.BatchDate	
	, InSequence 
		= CASE 
			WHEN bp.Sequenced = 0 then CONVERT(bit, 1)
			WHEN b.ForceOperationSequence = 1 then CONVERT(bit, 1)
			WHEN EXISTS(SELECT null
						FROM SEIDR.Batch b2
						JOIN SEIDR.BatchStatus bs2
							ON b2.BatchStatusCode = bs2.BatchStatusCode
						WHERE BatchProfileID = b.BatchProfileID
						AND Active = 1
						AND bs2.IsComplete = 1
						AND bs2.IsError = 0
						AND DATEDIFF(day, BatchDate, b.BatchDate) = 1) then 1
			else 0
			end			
	, b.IgnoreParents
	, b.ForceOperationSequence
	, b.Locked
	, b.Queued
	--, k1.KeyValue [Key1]
	--, k2.KeyValue [Key2]
	--, SSISPackagePath -- Should be a parameter for the Profile_Operation
	--, ServerInstanceName -- Should be a parameter for the Profile_Operation	
	, COALESCE(o.ThreadID, bp.ThreadID) ThreadID --Operation Thread. (Executor). If null, first come first serve
	, bp.InputFolder	
	, bp.ScheduleID [BatchProfileScheduleID]
	, b.ScheduleID --Populated if batch was created via Schedule as opposed to file registration
	, b.RepetitionGroupID
	, b.LateRegistration
	, po.Step
	, po.Profile_OperationID
	, o.OperationID
	, o.Operation
	, o.OperationSchema
	, o.[Version]
	, o.[Description]		
	, o.ParameterSelect	
	, b.AttemptCount
	, b.LastQueued
	, s.BatchStatusCode
	, s.Description
	+ CASE 
		WHEN s.IsError = 1 
			then '( ERROR, ' + CASE 
								WHEN s.IsComplete = 1 then 'COMPLETE'
								WHEN s.CanContinue =1 then 'INCOMPLETE/CAN CONTINUE'
								ELSE 'INCOMPLETE' end + ' )'
		WHEN s.IsComplete = 1
			then '( COMPLETE )'
		WHEN s.CanContinue =1 then '( INCOMPLETE/CAN CONTINUE )'
		end					
	 [BatchStatus]	
	, s.IsError
	, s.IsComplete
	, s.CanContinue
	, po.FailureDelay
	, EarliestNextQueueTime = CASE 
				WHEN b.LastQueued is null then b.DC --If hasn't been queued, go based on when the batch was created
				WHEN AttemptCount = 0 then b.LastQueued 
				else DATEADD(minute, po.FailureDelay, b.LastQueued) 
				end	
	FROM SEIDR.Batch b			
	JOIN SEIDR.vw_BatchProfile bp
		on b.BatchProfileID = bp.BatchProfileID
	JOIN SEIDR.BatchStatus s
		ON b.BatchStatusCode = s.BatchStatusCode	
		--AND s.IsComplete = 0				
	OUTER APPLY(SELECT TOP 1 * 
				FROM SEIDR.Profile_Operation
				WHERE BatchProfileID = b.BatchProfileID
				AND Active = 1
				AND (QualifyingBatchStatus is null
					OR QualifyingBatchStatus = b.BatchStatusCode --Status matches the explicit status for starting
				)
				AND(
					CanRunOnErrorStatus = 1 AND s.IsError = 1 --General error path for continuing
					OR
					CanRunOnSuccessStatus = 1 AND s.IsError = 0 --Need for...ummm...?		 		
					)
				AND Step = CurrentStep
				ORDER BY QualifyingBatchStatus DESC
				)po
	JOIN SEIDR.Operation o
		ON po.OperationID = o.OperationID
		AND o.Active = 1		
	WHERE b.Active = 1	
