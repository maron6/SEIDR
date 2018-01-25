CREATE PROCEDURE SEIDR.usp_JobProfile_CheckSchedule
AS
BEGIN

	DECLARE @RC int = 0 --seconds to delay next call to this procedure

	--Test for missing days of schedules
	DECLARE @Now datetime = GETDATE()
	CREATE TABLE #JobSchedule(JobProfileID int not null, ScheduleID int  not null, ScheduleDate datetime not null, 
			ComparisonDate datetime not null, 
			[MatchingRuleClusterID] int null, 
			[Match] as (CONVERT(bit, CASE WHEN MatchingRuleClusterID is null then 0 else 1 end)),
			PRIMARY KEY(JobProfileID, ScheduleID, ScheduleDate))
	INSERT INTO #JobSchedule(JobProfileID, ScheduleID, ScheduleDate, ComparisonDate)
	SELECT jp.JobProfileID, jp.ScheduleID, d.[Date], LastProcessDate
	FROM SEIDR.JobProfile jp
	JOIN (SELECT JobProfileID, MAX(ProcessingDate) LastProcessDate
			FROM SEIDR.JobExecution 
			WHERE Active = 1
			GROUP BY JobProfileID)je
		ON jp.JobProfileID = je.JobProfileID
	CROSS APPLY SEIDR.ufn_GetDays(jp.ScheduleFromDate, jp.ScheduleThroughDate) d
	WHERE jp.ScheduleValid = 1 AND jp.Active = 1
	AND d.[Date] <= @Now
	AND d.[Date] > je.LastProcessDate

	UPDATE js
	SET [MatchingRuleClusterID] = SEIDR.ufn_CheckSchedule(js.SCheduleID, ScheduleDate, ComparisonDate)
	FROM #JobSchedule js

	
	INSERT INTO SEIDR.JobExecution(JobProfileID, UserKey, UserKey1, UserKey2,
			StepNumber, ExecutionStatusCode, 
			ProcessingDate, ScheduleRuleClusterID)
	SELECT js.JobProfileID, jp.UserKey, jp.UserKey1, jp.UserKey2, 
			1, 'S',
			ScheduleDate, [MatchingRuleClusterID]
	FROM #JobSchedule js
	JOIN SEIDR.JobProfile jp
		ON js.JobProfileID = jp.JobProfileID
	LEFT JOIN SEIDR.JobExecution je
		ON js.JobProfileID = je.JobProfileID
		AND js.ScheduleDate = je.ProcessingDate
	WHERE je.JobExecutionID is null
	AND [Match] = 1
	
	IF @@ROWCOUNT = 0
		SET @RC += 10


	INSERT INTO SEIDR.JobExecution(JobProfileID, UserKey, UserKey1, UserKey2,
			StepNumber, ExecutionStatusCode, 
			ProcessingDate, ProcessingTime, ScheduleRuleClusterID)
	SELECT js.JobProfileID, jp.UserKey, jp.UserKey1, jp.UserKey2, 
			1, 'S',
			ScheduleDate, @Now, [MatchingRuleClusterID]
	FROM #JobSchedule js
	JOIN SEIDR.JobProfile jp
		ON js.JobProfileID = jp.JobProfileID
	LEFT JOIN SEIDR.JobExecution je
		ON js.JobProfileID = je.JobProfileID
		AND js.ScheduleDate = je.ProcessingDate
	WHERE je.JobExecutionID is null
	AND [Match] = 1

	IF @@ROWCOUNT = 0
		SET @RC += 10

	--For Same day execution (hour/minute intervals)
	INSERT INTO SEIDR.JobExecution(JobProfileID, UserKey, UserKey1, UserKey2,
		StepNumber, ExecutionSTatusCode, ScheduleRuleClusterID,
		ProcessingDate, ProcessingTime)
	SELECT js.JobProfileID, UserKey, UserKey1, UserKey2, 
			1, 'S', [MatchingRuleClusterID],
			@Now, @Now
	FROM SEIDR.JobProfile js
	JOIN(SELECT JobProfileID, MAX(ProcessingDateTime) ProcessingDateTime
			FROM SEIDR.JobExecution
			GROUP BY JobProfileID)je
		ON js.JobProfileID = je.JobProfileID
	CROSS APPLY(SELECT SEIDR.ufn_CheckSChedule(js.ScheduleID, @Now, ProcessingDateTime))s(MatchingRuleClusterID)
	WHERE js.ScheduleValid = 1 AND js.Active = 1	
	AND  s.MatchingRuleClusterID is not null
	
	IF @@ROWCOUNT = 0
		SET @RC += 10

	--Initial Jobs for new schedules.
	INSERT INTO SEIDR.JobExecution(JobProfileID, UserKey, UserKey1, UserKey2,
		StepNumber, ExecutionSTatusCode, ScheduleRuleClusterID,
		ProcessingDate, ProcessingTime)
	SELECT JobProfileID, UserKey, UserKey1, UserKey2, 
			1, 'S', [MatchingRuleClusterID],
			js.ScheduleFromDate, @Now
	FROM SEIDR.JobProfile js		
	CROSS APPLY(SELECT SEIDR.ufn_CheckSChedule(js.ScheduleID, @Now, js.ScheduleFromDate))s(MatchingRuleClusterID)
	WHERE js.ScheduleValid = 1 AND js.Active = 1	
	AND NOT EXISTS(SELECT null 
					FROM SEIDR.JobExecution WITH (NOLOCK)
					WHERE JobPRofileID = js.JobProfileID)
	AND  s.MatchingRuleClusterID is not null	

	IF @@ROWCOUNT = 0
		SET @RC += 10

	RETURN @RC
END
