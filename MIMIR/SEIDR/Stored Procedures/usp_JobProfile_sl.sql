
CREATE PROCEDURE SEIDR.usp_JobProfile_sl
	@Description varchar(256) = null,
	@RequiredThreadID tinyint = null,
	@ScheduleID int = null,
	@UserKey int = null,
	@UserKey1 varchar(50) = null,
	@UserKey2 varchar(50) = null,
	@JobPriority varchar(10) = null,
	@ScheduleFromDate datetime = null,
	@ScheduleThroughDate datetime = null
AS
BEGIN
		

	SELECT * 
	FROM SEIDR.JobProfile
	WHERE Active = 1
	AND (@Description is null or [Description] LIKE '%'+ @Description + '%')
	AND (@JobPriority is null or JobPriority = @JobPriority)
	AND (@ScheduleThroughDate is null or ScheduleThroughDate <= @ScheduleThroughDate)
	AND (@ScheduleFromDate is null or ScheduleFromDate >= @ScheduleFromDate)
	AND (@ScheduleID is null or @ScheduleID = ScheduleID)
	AND (@RequiredThreadID is null or [RequiredThreadID] = @RequiredThreadID) --Note required thread can be overridden by job meta data..
	AND (@UserKey is null or UserKey = @UserKey)
	AND (@UserKey1 is null or UserKey1 = @UserKey1)
	AND (@UserKey2 is null or UserKey2 = @UserKey2)
END