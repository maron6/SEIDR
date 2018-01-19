CREATE TABLE SEIDR.ScheduleRule(
	ScheduleRuleID int not null identity(1,1) primary key,
	Description varchar(250),
	PartOfDateType varchar(4) null, --DATEPART(xx)
	PartOfDate int null,
	IntervalType varchar(4) null, --Datediff(xx)
	IntervalValue int null,
	DC smalldatetime not null default (GETDATE()),
	Creator varchar(128) not null default (SUSER_NAME()),
	DD smalldatetime null,
	Active as CONVERT(bit, CASE WHEN dd is null then 1 else 0 end) PERSISTED NOT NULL,
	CHECK (PartOfDateType is not null or IntervalType is not null),
	CHECK(PartOfDateType is null or PartOfDate is not null), --Ignore PartOfDate if PartofDateType is null, but don't allow PartOfDateType without PartOfDate
	CHECK (IntervalType is null or IntervalValue > 0) --Ignore Interval Value if IntervalType is null, but don't allow IntervalType without Interval Value. Also ensure valid Interval Value.
	)
GO
 

CREATE TABLE SEIDR.Schedule
(
	ScheduleID int not null identity(1,1) primary key,
	Description varchar(250),
	DC smalldatetime not null default (GETDATE()),
	Creator varchar(128) not null default (SUSER_NAME()),
	DD smalldatetime null,
	Active as CONVERT(bit, CASE WHEN dd is null then 1 else 0 end) PERSISTED NOT NULL
)
GO

CREATE TABLE SEIDR.ScheduleRuleCluster
(
	ScheduleRuleClusterID int not null identity(1,1) primary key,
	Description varchar(250),
	DC smalldatetime not null default (GETDATE()),
	Creator varchar(128) not null default (SUSER_NAME()),
	DD smalldatetime null,
	Active as CONVERT(bit, CASE WHEN dd is null then 1 else 0 end) PERSISTED NOT NULL
)
GO
CREATE TABLE SEIDR.ScheduleRuleCluster_ScheduleRule --AND rules
(	ScheduleRuleClusterID int not null foreign key references SEIDR.ScheduleRuleCluster(ScheduleRuleClusterID), 
	ScheduleRuleID int not null foreign key references SEIDR.ScheduleRule(ScheduleRuleID),
	PRIMARY KEY(ScheduleRuleClusterID, ScheduleRuleID)
)
GO

CREATE TABLE SEIDR.Schedule_ScheduleRuleCluster --OR clusters of rules
(
	ScheduleID int not null FOREIGN KEY REFERENCES SEIDR.Schedule(ScheduleID),
	ScheduleRuleClusterID int not null FOREIGN KEY REFERENCES SEIDR.ScheduleRuleCluster(ScheduleRuleClusterID), 
	FromDate datetime not null default(CONVERT(date, GETDATE())),
	ThroughDate datetime null,
	PRIMARY KEY(ScheduleID, ScheduleRuleClusterID),
	CHECK(ThroughDate is null or ThroughDate > FromDate)
)
GO

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
GO

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
					OR 0 =
						CASE IntervalType
							WHEN 'year' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'yyyy' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'yy' then DATEDIFF(year, @FromDate, @DateToCheck)
							WHEN 'qq' then DATEDIFF(quarter, @FromDate, @DateToCheck)
							WHEN 'q' then DATEDIFF(quarter, @FromDate, @DateToCheck)
							WHEN 'mm' then DATEDIFF(month, @FromDate, @DateToCheck)
							WHEN 'm' then DATEDIFF(month, @FromDate, @DateTOCheck)
							WHEN 'hh' then DATEDIFF(hour, @FromDate, @DateToCheck)
							WHEN 'hour' then DATEDIFF(hour, @FromDate, @DateToCheck)
							WHEN 'min' then DATEDIFF(minute, @FromDate, @DateToCheck)
							WHEN 'mi' then DATEDIFF(minute, @FromDate, @DateToCheck)
							WHEN 'n' then DATEDIFF(minute, @FromDate, @DateToCheck)
						end % IntervalValue)
				)
		SET @Ret = 1
	RETURN @RET
END
GO


-- =============================================
-- Author:		Ryan
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE FUNCTION SEIDR.[ufn_GetDays] 
(	
	-- Add the parameters for the function here
	@StartDate datetime,
	@EndDate datetime
)
RETURNS @Dates TABLE (Date Date primary key)
AS
BEGIN

	SET @EndDate = COALESCE(@EndDate, GETDATE())
	INSERT INTO @Dates			
	select a.Date	
	from (
	select CONVERT(date, @EndDate -  (a.a + (10 * b.a) + (100 * c.a))) as Date
	from (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as a
	cross join (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as b
	cross join (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as c
	) a
	WHERE a.Date >= @StartDate 
	RETURN
END
GO

SELECT * FROM SEIDR.ufn_GetDays( GETDATE() - 30, null)


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
GO	


CREATE PROCEDURE SEIDR.usp_CheckSchedules
	@ScheduleID int = null, 
	@DateToCheck datetime, 
	@Valid bit output
AS
BEGIN
	IF @DateToCheck is null
		SET @DateToCheck = GETDATE()

	CREATE TABLE #schedule(ScheduleID int, ScheduleRuleClusterID int, ScheduleRuleID int, [Match] bit,)

	INSERT INTO #Schedule
	SELECT src.ScheduleRuleClusterID, s.ScheduleID, 
		SEIDR.ufn_CheckScheduleRuleCluster(src.ScheduleRuleClusterID, @DateToCheck, FromDate) 
	FROM SEIDR.Schedule s
	JOIN SEIDR.Schedule_ScheduleRuleCluster ssr
		oN s.ScheduleID = ssr.ScheduleID
		--AND ssr.Active = 1
	JOIN SEIDR.ScheduleRuleCluster src
		On ssr.ScheduleRuleClusterID = src.ScheduleRuleClusterID
		and src.Active = 1
	WHERE @DateToCheck > FromDate
	AND (@ScheduleID = s.ScheduleID or s.Active = 1 AND @ScheduleID is null)
	AND (ThroughDate is null or ThroughDate > @DateToCheck)



	SELECT s.ScheduleID, @DateToCheck, MIN(s.ScheduleRuleClusterID) MatchedScheduleRuleClusterID
	FROM #Schedule s
	LEFT JOIN #Schedule s2
		ON s.ScheduleRuleClusterID = s2.ScheduleRuleClusterID
		AND s2.[Match] = 0
	WHERE s.[Match] = 1
	AND s2.ScheduleRuleID IS NULL
	GROUP by s.ScheduleID

	IF @@ROWCOUNT > 0
		SET @Valid = 1
END
GO
	