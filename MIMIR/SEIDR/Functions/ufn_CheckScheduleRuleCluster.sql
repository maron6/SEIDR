CREATE FUNCTION SEIDR.ufn_CheckScheduleRuleCluster(@ScheduleRuleClusterID int, @DateToCheck datetime, @FromDate datetime)
RETURNS BIT
AS
BEGIN
	DECLARE @RET bit = 0

	SELECT @RET = MIN(CONVERT(int, CheckValue))
	FROM SEIDR.ScheduleRuleCluster_ScheduleRule srcsr
	CROSS APPLY (SELECT SEIDR.ufn_CheckScheduleRule(srcsr.ScheduleRuleID, @DateToCheck, @FromDate)) r(CheckValue)
	WHERE srcsr.ScheduleRuleClusterID = @ScheduleRuleClusterID

	RETURN @RET
END
