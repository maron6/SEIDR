


CREATE VIEW SEIDR.vw_JobExecution
AS
SELECT JobExecutionID, je.JobProfileID, jpj.JobProfile_JobID, je.StepNumber,
		je.ExecutionStatusCode,
		je.ExecutionStatusNameSpace, je.ExecutionStatus, je.RetryCount, jpj.CanRetry, jpj.RetryDelay,
		s.IsComplete,
		je.IsWorking,
		s.IsError,
		[CanQueue] = CONVERT(bit, 
				CASE 
					--WHEN jpj.JobProfile_JobID is null then 0
					WHEN s.IsComplete = 1 then 0 
					WHEN je.IsWorking = 1 then 0 
					WHEN je.InWorkQueue = 1 then 0 --already queued, skip.		
					--WHEN s.IsError = 1 then 0 --even if it's an error, if it matches up with a job, can queue.
				ELSE 1 
				end),
		je.FilePath, je.FileSize,
		je.LastExecutionStatusCode, je.LastExecutionStatusNameSpace,
		je.UserKey, je.UserKey1, je.UserKey2,
		je.ForceSequence, ProcessingDate, ProcessingTime, ProcessingDateTime,
		CONVERT(bit, CASE WHEN je.ForceSequence = 1 or jpj.SequenceScheduleID is null then 1 
					WHEN s.IsComplete = 1 then 1 --lazy. If complete, don't worry about sequence
					WHEN je.IsWorking = 1 or InWorkQueue = 1 then 1 --If it has already been picked up for work on this step, then be lazy.
				else (SELECT CASE WHEN SEIDR.ufn_CheckSchedule(jpj.SequenceScheduleID, je.ProcessingDate, MAX(jes.ProcessingDate)) is null then 0 else 1 end
						FROM SEIDR.JobExecution_ExecutionStatus jes
						JOIN SEIDR.JobExecution je2
							ON jes.JobExecutionID = je2.JObExecutionID
							AND je2.Active = 1
						WHERE JobProfile_JobID = jpj.JobProfile_JobID
						AND jes.Success = 1 AND jes.IsLatestForExecutionStep = 1
						) 
				end) InSequence, --need to check for CanQueue and InSequence
		 -- Schedule function checking existence of a JobExecution_ExecutionStatus record lining up with the processing date 
		 -- and Success = 1
		jpj.SequenceScheduleID, 
		j.SingleThreaded [JobSingleThreaded],
		pje.PriorityValue as ExecutionPriority,
		pjp.PriorityValue as ProfilePriority,
		DATEDIFF(hour, LU, GETDATE()) WorkQueueAge,
		(DATEDIFF(hour, LU, GETDATE()) * 3) + pje.PriorityValue + pjp.PriorityValue + (DATEDIFF(day, je.ProcessingDate, GETDATE())/ 3) [WorkPriority],
		j.JobName, j.JobNameSpace, j.ThreadName [JobThreadName],
		COALESCE(jpj.RequiredThreadID, jp.RequiredThreadID) RequiredThreadID,
		jp.SuccessNotificationMail, jpj.[FailureNotificationMail]
FROM SEIDR.JobExecution je
JOIN SEIDR.[Priority] pje
	ON je.[JobPriority] = pje.PriorityCode
JOIN SEIDR.ExecutionStatus s
	ON je.ExecutionStatusNameSpace = s.[NameSpace]
	AND je.ExecutionStatusCode = s.ExecutionStatusCode
--LEFT 
CROSS APPLY(SELECT TOP 1 *
			FROM SEIDR.JobProfile_Job
			WHERE je.JobProfileID = JobProfileID
			AND Active = 1
			AND je.StepNumber = StepNumber
			AND (TriggerExecutionNameSpace is null AND s.IsError = 0 or je.ExecutionStatusNameSpace = TriggerExecutionNameSpace)
			AND (TriggerExecutionStatusCode is null AND s.IsError = 0  or je.ExecutionStatusCode = TriggerExecutionStatusCode)
			ORDER BY TriggerExecutionNameSpace desc, TriggerExecutionStatusCode desc)jpj
JOIN SEIDR.JobProfile jp
	ON je.JobProfileiD = jp.JobProfileID
JOIN SEIDR.[Priority] pjp
	ON jp.JobPriority = pjp.PriorityCode
--LEFT 
JOIN SEIDR.Job j
	ON jpj.JobID = j.JobID
WHERE je.Active = 1 AND jp.Active = 1

