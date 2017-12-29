
CREATE PROCEDURE SEIDR.usp_Batch_SL_Work
	@ThreadID tinyint,
	@BatchSize tinyint = 1
AS
BEGIN
	IF OBJECT_ID('tempdb..#BatchList') IS NOT NULL
		DROP TABLE #BatchList

	CREATE TABLE #BatchList (BatchID int primary key)

	;WITH CTE AS
	(
		--DECLARE @BatchSize int = 1, @ThreadID tinyint = 1
		SELECT TOP (@BatchSize) BatchID, LastQueued, Priority
		FROM(
			SELECT 0 [Order], *
			FROM SEIDR.vw_Batch_Runnable
			WHERE ThreadID = @ThreadID
			UNION ALL
			SELECT 1 [Order], *
			FROM SEIDR.vw_Batch_Runnable
			WHERE ThreadID is null			
		)r
		ORDER BY 
			[Order] asc, --Prefer things actually assigned to passed Thread
			CASE WHEN [Priority] is null then 0 else 1 end desc,
			[Priority] ASC, --Tie breaker before queue time
			LastQueued ASC
	)
	UPDATE b
	SET LastQueued = GETDATE(),
		Priority = null, 
		Queued = 1
	OUTPUT inserted.BatchID INTO #BatchList
	FROM SEIDR.Batch b
	JOIN CTE
		ON b.BatchID = cte.BatchID	
	

	SELECT * 
	FROM SEIDR.vw_Batch --Note: vw_Batch_Runnable includes an exclusion on 'Locked' but vw_Batch doesn't
	WHERE BatchID IN (SELECT BatchID FROM #BatchList)
	
	DROP TABLE #BatchList
END
