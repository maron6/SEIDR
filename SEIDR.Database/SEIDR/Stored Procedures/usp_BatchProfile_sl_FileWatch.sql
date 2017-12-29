CREATE PROCEDURE SEIDR.usp_BatchProfile_sl_FileWatch
	@BatchSize tinyint
	,@ThreadID tinyint
AS
BEGIN
	INSERT INTO SEIDR.BatchProfile_Queue (BatchProfileID, QueueThreadID)
	SELECT BatchProfileID, 1
	FROM SEIDR.BatchProfile p
	WHERE Active = 1
	AND NOT EXISTS(SELECT null 
					FROM SEIDR.BatchProfile_Queue 
					WHERE BatchProfileID = p.BatchProfileID)
	
	IF OBJECT_ID('tempdb..#BatchProfileList') IS NOT NULL
		DROP TABLE #BatchProfileList

	CREATE TABLE #BatchProfileList(BatchProfileID int primary key)

	;WITH CTE AS(
		SELECT TOP (@BatchSize) BatchProfileID, LastQueued, [Priority]
		FROM SEIDR.BatchProfile_Queue q
	    WHERE 
			ISNULL(QueueThreadID, 1) = @ThreadID
			AND EXISTS(SELECT null
						FROM SEIDR.vw_BatchProfile
						WHERE BatchProfileID = q.BatchProfileID
						AND InvalidRegistration = 0
						)
		ORDER BY 
		CASE WHEN [Priority] is null then 0 else 1 end desc,
		[Priority] ASC,
		LastQueued asc
	)
	UPDATE CTE
	SET LastQueued = GETDATE(), 
		[Priority] = null
	OUTPUT inserted.BatchProfileID INTO #BatchProfileList	
	

	SELECT * FROM SEIDR.vw_BatchProfile
	WHERE BatchProfileID IN (SELECT BatchProfileID FROM #BatchProfileList)

	DROP TABLE #BatchProfileList
END
