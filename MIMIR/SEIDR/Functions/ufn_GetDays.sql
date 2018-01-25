CREATE FUNCTION SEIDR.[ufn_GetDays] 
( 
 -- Add the parameters for the function here
 @StartDate datetime,
 @EndDate datetime
)
RETURNS @Dates TABLE (Date Date primary key)
AS
BEGIN

 --Get a date range by subtracting up to 999 days
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

 WHILE @StartDate < @EndDate - 999
 BEGIN
	SET @EndDate -= 1000
	INSERT INTO @Dates
	SELECT a.Date
	from(
	select CONVERT(date, @EndDate -  (a.a + (10 * b.a) + (100 * c.a))) as Date 
	 from (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as a
	 cross join (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as b
	 cross join (select 0 as a union all select 1 union all select 2 union all select 3 union all select 4 union all select 5 union all select 6 union all select 7 union all select 8 union all select 9) as c
	 ) a
	 WHERE a.Date >= @StartDate 
 END

 RETURN
END
