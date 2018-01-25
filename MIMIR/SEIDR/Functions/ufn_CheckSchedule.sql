CREATE FUNCTION SEIDR.ufn_CheckSchedule(@ScheduleID int, @DateToCheck datetime, @FromDate datetime)
RETURNS int
AS
BEGIN
	DECLARE @RET int = null
	IF @DateToCheck < @FromDate
		RETURN @RET

	SELECT TOP 1 @RET =  ScheduleRuleClusterID				
				FROM SEIDR.Schedule_ScheduleRuleCluster ssrc
				WHERE ssrc.ScheduleID = @ScheduleID				
				--AND @DateToCheck > ssrc.FromDate
				--AND (ssrc.ThroughDate is null or ssrc.ThroughDate > @DateToCheck)
				AND SEIDR.ufn_CheckScheduleRuleCluster(ScheduleRuleClusterID, @DateToCheck, @FromDate) = 1						
	RETURN @RET
END
