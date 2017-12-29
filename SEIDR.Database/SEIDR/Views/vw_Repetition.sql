CREATE VIEW SEIDR.vw_Repetition
	AS
		SELECT g.RepetitionGroupID, g.Description [RepetitionGroup], c.RepetitionGroupSize,
		g.AllowLateRegistration, g.HourInterval, g.MinuteInterval,
		r.RepetitionID, i.Description [RepetitionIntervalType], RepetitionInterval
		FROM SEIDR.RepetitionGroup g
		CROSS APPLY(SELECT COUNT(*) RepetitionGroupSize FROM SEIDR.Repetition WHERE RepetitionGroupID = g.RepetitionGroupID)c
		LEFT JOIN SEIDR.Repetition r
			ON g.RepetitionGroupID = r.RepetitionGroupID
		LEFT JOIN SEIDR.RepetitionIntervalType i
			ON r.RepetitionIntervalTypeCode = i.RepetitionIntervalTypeCode
		