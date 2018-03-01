
CREATE FUNCTION SEIDR.ufn_CheckScheduleRule(@ScheduleRuleID int, @DateToCheck datetime, @FromDate datetime)
RETURNS BIT
AS
BEGIN
	DECLARE @RET bit = 0
	IF EXISTS(SELECT null
				FROM SEIDR.ScheduleRule sr					
				WHERE @DateToCheck > @FromDate 
				AND (PartOfDateType is null 
					OR PartOfDate = 
						CASE PartOfDateType
							WHEN 'yy'   then DATEPART(yy, @DateToCheck)
							WHEN 'yyyy' then DATEPART(yy, @DateToCheck)
							WHEN 'year' then DATEPART(yy, @DateToCheck)
							WHEN 'wk' then  DATEPART(wk, @DateToCheck)
							WHEN 'ww' then  DATEPART(wk, @DateToCheck)
							WHEN 'week' then  DATEPART(wk, @DateToCheck)
							WHEN 'dy' then  DATEPART(dy, @DateToCheck)
							WHEN 'y' then  DATEPART(dy, @DateToCheck)
							WHEN 'day' then  DATEPART(dd, @DateToCheck)
							WHEN 'dd' then  DATEPART(dd, @DateToCheck)
							WHEN 'd' then  DATEPART(dd, @DateToCheck)
							WHEN 'm' then DATEPART(mm, @DateToCheck)
							WHEN 'mm' then DATEPART(mm, @DateToCheck)
							WHEN 'q' then DATEPART(qq, @DateToCheck)
							WHEN 'qq' then  DATEPART(qq, @DateToCheck)
							WHEN 'dw' then DATEPART(dw, @DateToCheck)
							WHEN 'hour' then DATEPART(hh, @DateToCheck)
							WHEN 'hr' then  DATEPART(hh, @DateToCheck)
							WHEN 'min' then DATEPART(mi, @DateToCheck)
							WHEN 'mi' then DATEPART(mi, @DateToCheck)
						END					
					)
				AND (IntervalType is null 
					OR IntervalValue = --0 =
						CASE IntervalType
							WHEN 'year' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'yyyy' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'yy' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'qq' then DATEDIFF(quarter, @FromDate, @DateToCheck)
							WHEN 'q' then DATEDIFF(quarter, @FromDate, @DateToCheck)
							WHEN 'mm' then DATEDIFF(month, @FromDate, @DateToCheck)
							WHEN 'm' then DATEDIFF(month, @FromDate, @DateTOCheck)/* 
							--If job should poll multiple times per day, should have the job ReQueue instead of completing. 
							--Higher steps should be started by creating registration files with step number specified
							WHEN 'hh' then DATEDIFF(hour, @FromDate, @DateToCheck)
							WHEN 'hour' then DATEDIFF(hour, @FromDate, @DateToCheck)
							WHEN 'min' then DATEDIFF(minute, @FromDate, @DateToCheck)
							WHEN 'mi' then DATEDIFF(minute, @FromDate, @DateToCheck)
							WHEN 'n' then DATEDIFF(minute, @FromDate, @DateToCheck)*/
						end 
						--% IntervalValue
						)
				)
		SET @Ret = 1
	RETURN @RET
END
