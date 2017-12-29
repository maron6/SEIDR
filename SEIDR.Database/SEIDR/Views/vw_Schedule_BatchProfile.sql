CREATE VIEW SEIDR.vw_Schedule_BatchProfile
	AS
		SELECT s.ScheduleID, s.Description [Schedule], p.BatchProfileID, p.LastRegistration, StartDate, StartHour, 
		v.RepetitionGroupID [FirstValidRepetitionGroupID], v.RepetitionGroup [FirstValidRepetitionGroup],
			v.PartialDay,
			CONVERT(bit, IIF(v.RepetitionGroupID IS NOT NULL, 1, 0) ) [Eligible],
			HourInterval, MinuteInterval, Late, PartialDayMatch, CanUseForPartialDay
		FROM SEIDR.Schedule s
		JOIN SEIDR.BatchProfile p
			ON s.ScheduleID = p.ScheduleID
			AND p.Active = 1
		OUTER APPLY(SELECT TOP 1 *, 
					CONVERT(bit, CASE WHEN CurrentRepetitions < Repetitions then 1 else 0 end) [Late] --Has to match on LateRepetitions then
					FROM SEIDR.vw_Schedule_BatchProfile_RepetitionGroup
					WHERE ScheduleID = s.ScheduleID
					AND BatchProfileID = p.BatchProfileID
					AND (Repetitions = CurrentRepetitions  
						OR Repetitions = LateRepetitions 
						)
					AND (LastRegistration < CAST(GETDATE() as date) OR PartialDayMatch = 1)
					--AND CanUseToday = 1 --Partial day or last registration was at least one day ago
					--Some repetitions might qualify for both valid and late... e.g. every 1 day
					--AND (HourInterval is null 
					--	OR LastRegistration is null 
					--	OR DATEDIFF(hour, LastRegistration, GETDATE()) > HourInterval)
					--AND (MinInterval is null 
					--	OR LastRegistration is null
					--	OR DATEDIFF(minute, LastRegistration, GETDATE()) > MinInterval)					
					--AND CanUse = 1 --Only partial days are eligible on same day as the last registration						
					ORDER BY CurrentRepetitions DESC, RepetitionGroupID asc  --Prioritize on-time repetitions over late ones
					) v		
		WHERE s.DateFrom <= CONVERT(date, GETDATE())
		AND (s.DateThrough is null OR s.DateThrough > GETDATE())
		AND s.Active = 1