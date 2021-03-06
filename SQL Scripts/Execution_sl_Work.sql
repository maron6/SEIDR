/*    ==Scripting Parameters==

    Source Server Version : SQL Server 2016 (13.0.4001)
    Source Database Engine Edition : Microsoft SQL Server Express Edition
    Source Database Engine Type : Standalone SQL Server

    Target Server Version : SQL Server 2017
    Target Database Engine Edition : Microsoft SQL Server Standard Edition
    Target Database Engine Type : Standalone SQL Server
*/

USE [MIMIR]
GO
/****** Object:  StoredProcedure [SEIDR].[usp_JobExecution_sl_Work]    Script Date: 1/20/2018 9:19:08 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [SEIDR].[usp_JobExecution_sl_Work]
	@ThreadID int,
	@ThreadCount int,
	@BatchSize int = 5
as
BEGIN

	BEGIN TRAN

	SELECT TOP (@BatchSize) *
	INTO #jobs
	FROM SEIDR.vw_JobExecution
	WHERE CanQueue = 1
	AND InSequence = 1
	AND (RequiredThreadID is null or RequiredThreadID % @ThreadCount = @ThreadID)
	ORDER BY RequiredThreadID desc, WorkPriority desc, ProcessingDate asc, ExecutionPriority DESC

	SET @BatchSize = @@ROWCOUNT

	UPDATE je
	SET InWorkQueue = 1
	FROM SEIDR.JobExecution je
	JOIN #jobs j
		ON je.JobExecutionID = j.JobExecutionID
	WHERE InWorkQueue = 0
	
	IF @@ROWCOUNT <> @BatchSize
	BEGIN		
		ROLLBACK
		RETURN 50
	END

	COMMIT

	SELECT * FROM #jobs
	
END
GO

SELECT * FROM SEIDR.vw_JobExecution

exec [SEIDR].[usp_JobExecution_sl_Work] 1, 1
