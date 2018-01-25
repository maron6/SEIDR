CREATE PROCEDURE [SEIDR].[usp_Job_RegisterFile]
        @JobProfileID int,
        @FilePath varchar(255),        
        @FileSize bigint,
        @FileDate date,
		@FileHash varchar(88),
		@StepNumber smallint     
AS
BEGIN
	DECLARE @JobExecutionID bigint
	INSERT INTO SEIDR.JobExecution(JobProfileID, UserKey, UserKey1, UserKey2,
				StepNumber, ExecutionStatusCode, 
				FilePath, FileSize, FileHash, ProcessingDate)
	SELECT @JobProfileID, UserKey, UserKey1, UserKey2, 
				@StepNumber, 'R',
				@FilePath, @FileSize, @FileHash, @FileDate
	FROM SEIDR.JobProfile
	WHERE JobProfileID = @JobProfileID

	SELECT @JobExecutionID = SCOPE_IDENTITY()

	IF @JobExecutionID is null
		RETURN 1

	SELECT * FROM SEIDR.vw_JobExecution WHERE JobExecutionID = @JobExecutionID AND CanQueue = 1 AND InSequence = 1
	IF @@ROWCOUNT > 0
	BEGIN
		UPDATE SEIDR.JobExecution
		SET InWorkQueue = 1
		WHERE JobExecutionID = @JobExecutionID

		return 0
	END
	RETURN 1
END





