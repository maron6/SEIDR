
CREATE PROCEDURE SEIDR.usp_CheckMissingSchedules --Historical. No minute/hour support then.
AS
BEGIN
		
		CREATE TABLE #schedule(ScheduleID int, ScheduleDate date, ScheduleRuleClusterID int, [Match] bit,)
	
		INSERT INTO #Schedule
		SELECT s.ScheduleID, d.Date [ScheduleDate], src.ScheduleRuleClusterID, 
			SEIDR.ufn_CheckScheduleRuleCluster(src.ScheduleRuleClusterID, d.Date, ssrc.FromDate) 
		FROM SEIDR.Schedule s 
		JOIN SEIDR.Schedule_ScheduleRulecluster ssrc
			ON s.ScheduleID = ssrc.ScheduleID --and ssrc.Active = 1
		CROSS APPLY SEIDR.ufn_GetDays(ssrc.FromDate, ssrc.ThroughDate) d --Allow early creation of schedule records?
		JOIN SEIDR.ScheduleRuleCluster src
			ON ssrc.ScheduleRuleClusterID =src.ScheduleRuleClusterID and src.Active = 1		
		WHERE s.Active = 1 
		--AND NOT EXISTS(SELECT null FROM .... WHERE ScheduleID = s.ScheduleID AND ProcessingDate = d.[Date]) --To get records on object x using schedule s that are missing a processing date

		SELECT ScheduleID, ScheduleDate, MIN(ScheduleRuleClusterID) MatchedScheduleRuleClusterID
		FROM #Schedule s1
		WHERE [match] = 1
		AND NOT EXISTS(SELECT null 
						FROM #Schedule 
						WHERE ScheduleID = s1.ScheduleID 
						AND ScheduleRuleClusterID = s1.ScheduleRuleClusterID 
						AND ScheduleDate = s1.ScheduleDate AND [Match] = 0)
		GROUP BY ScheduleID, ScheduleDate
END
