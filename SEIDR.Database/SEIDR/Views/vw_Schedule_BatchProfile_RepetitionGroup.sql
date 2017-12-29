CREATE VIEW SEIDR.vw_Schedule_BatchProfile_RepetitionGroup
	AS	
		SELECT s.ScheduleID, p.BatchProfileID, g.RepetitionGroupID, g.Description [RepetitionGroup],
		sg.StartDate, sg.StartHour, sg.EndHour,  s.DateFrom [ScheduleDateFrom], s.DateThrough [ScheduleDateThrough],
		COUNT(r.RepetitionID) [Repetitions], 
		COUNT(CASE 
				WHEN sg.StartHour > DATEPART(hour, GETDATE() ) then null --future hour
				WHEN sg.EndHour < DATEPART(hour, GETDATE() ) then null --Ended earlier today
				WHEN CAST(GETDATE() as date) = CAST(LastRegistration as date) AND PartialDay = 0 then null
					--Beneath start hour, not valid. Ok for late, though
				--WHEN RepetitionIntervalTypeCode IN ('HR', 'MI') then 1 								
				--WHEN RepetitionIntervalTypeCode = 'HR' 
				--	then IIF(DATEDIFF(hour, LastRegistration, GETDATE()) > RepetitionInterval, 1, null)
				--WHEN RepetitionIntervalTypeCode = 'MI'
				--	then IIF(DATEDIFF(minute, LastRegistration, GETDATE()) > RepetitionInterval, 1, null)
					--Hours, minutes will be managed outside view						
				WHEN RepetitionIntervalTypeCode IN ('DW', 'DM', 'DY')
					AND (CASE RepetitionIntervalTypeCode
						WHEN 'DW' then DATEPART(dw, sg.StartDate)
						WHEN 'DM' then DATEPART(day, sg.StartDate)
						WHEN 'DY' then DATEPART(dy, sg.StartDate)
						end) = RepetitionInterval then IIF(r.Inverted = 0, 1, null)								
				WHEN RepetitionIntervalTypeCode IN ('DW', 'DM', 'DY') THEN IIF(r.Inverted = 1, 1, null)
				WHEN 
					(CASE 
					WHEN RepetitionIntervalTypeCode = 'D'
						then DATEDIFF(day, sg.StartDate, CONVERT(date, GETDATE() ) ) 
					WHEN RepetitionIntervalTypeCode = 'W'
						then DATEDIFF(week, sg.StartDate, CONVERT(date, GETDATE() ) )
					WHEN RepetitionIntervalTypeCode = 'M'
						then DATEDIFF(month, sg.StartDate, CONVERT(date, GETDATE() ) )
					WHEN RepetitionIntervalTypeCode = 'Y'
						THEN DATEDIFF(year, sg.StartDate, CONVERT(date, GETDATE() ) )
					end)
					% RepetitionInterval = 0
					then IIF(r.Inverted = 0, 1, null)
				 WHEN r.Inverted = 1 then 1
				 end) CurrentRepetitions, CAST(p.LastRegistration as date) [LastRegistration],
		COUNT(CASE
				WHEN g.AllowLateRegistration = 0 then null
				WHEN sg.StartDate = CAST(GETDATE() as DATE) then null --Cannot be late on start date
				WHEN LastRegistration >= CAST(GETDATE() AS DATE) then null --Profiles that have registered today cannot do a Late registration again				
				WHEN RepetitionIntervalTypeCode IN ('DW', 'DM', 'DY')
					AND (CASE RepetitionIntervalTypeCode
						WHEN 'DW' then DATEPART(dw, sg.StartDate)
						WHEN 'DM' then DATEPART(day, sg.StartDate)
						WHEN 'DY' then DATEPART(dy, sg.StartDate)
						end) = RepetitionInterval then IIF(r.Inverted = 0, 1, null)
				WHEN RepetitionIntervalTypeCode IN ('DW', 'DM', 'DY') THEN IIF(r.Inverted = 1, 1, null)
				WHEN 
					(CASE						
						WHEN RepetitionIntervalTypeCode = 'D'
							then DATEDIFF(day,  CAST(ISNULL(LastRegistration, s.DateFrom) as date), CONVERT(date, GETDATE() ) ) 
						WHEN RepetitionIntervalTypeCode = 'W'
							then DATEDIFF(week,  CAST(ISNULL(LastRegistration, s.DateFrom) as date), CONVERT(date, GETDATE() ) )
						WHEN RepetitionIntervalTypeCode = 'M'
							then DATEDIFF(month,  CAST(ISNULL(LastRegistration, s.DateFrom) as date), CONVERT(date, GETDATE() ) )
						WHEN RepetitionIntervalTypeCode = 'Y'
							THEN DATEDIFF(year, CAST(ISNULL(LastRegistration, s.DateFrom) as date), CONVERT(date, GETDATE() ) )
						end
					)
					> RepetitionInterval 
					then iif(r.Inverted = 0, 1, null)
				WHEN r.Inverted = 1
					then 1 end) LateRepetitions,				
		g.HourInterval, g.MinuteInterval, g.PartialDay,
		CONVERT(bit, CASE 
		WHEN CAST(GETDATE() as date) = CAST(LastRegistration as date) AND PartialDay = 1 then 1 		
		else 0 end) CanUseForPartialDay,
		CONVERT(bit,
		CASE WHEN PartialDay = 0 then 0
		WHEN CAST(GETDATE() as date) > CAST(LastRegistration as date) then 0 --Require same day for repeating		
		WHEN DATEDIFF(minute, LastRegistration, GETDATE()) 
			>= (60 * ISNULL(HourInterval, 0) + ISNULL(MinuteInterval, 0) ) then 1 
		else 0 end) [PartialDayMatch]
		FROM SEIDR.Repetition r				
		RIGHT JOIN SEIDR.RepetitionGroup g
			ON r.RepetitionGroupID = g.RepetitionGroupID
		JOIN SEIDR.Schedule_RepetitionGroup sg
			ON g.RepetitionGroupID = sg.RepetitionGroupID		
			AND sg.Active = 1
		JOIN SEIDR.Schedule s
			ON sg.ScheduleID = s.ScheduleID			
		JOIN SEIDR.BatchProfile p
			ON s.ScheduleID = p.ScheduleID
		WHERE sg.StartDate <= CAST(GETDATE() as DATE)
		GROUP BY s.ScheduleID,  p.BatchProfileID, g.RepetitionGroupID, g.Description, sg.StartDate, sg.StartHour, sg.EndHour,
		g.HourInterval, g.MinuteInterval, p.LastRegistration, PartialDay, s.DateFrom, s.DateThrough