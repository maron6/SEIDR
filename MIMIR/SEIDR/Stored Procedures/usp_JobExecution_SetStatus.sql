CREATE PROCEDURE SEIDR.usp_JobExecution_SetStatus
	@JobExecutionID bigint,
	@JobProfileID int,
	@FilePath varchar(250),
	@FileSize bigint,
	@FileHash varchar(88),	
	@Success bit,
	@Working bit,
	@ExecutionStatusCode varchar(5) = null,
	@ExecutionStatusNameSpace varchar(128) = null,
	@ExecutionTimeSeconds int = null,
	@Complete bit = null output
AS
BEGIN	
	DECLARE @CanRetry bit = 0 
	set @Complete = 0

	DECLARE @StepNumber smallint
	SELECT @StepNumber = StepNumber
	FROM SEIDR.JobExecution WHERE JobExecutionID = @JobExecutionID
	
	IF @ExecutionStatusCode = 'W' AND @ExecutionStatusNameSpace = 'SEIDR'
		SET @Working = 1


	IF @Working = 1
	BEGIN
		SET @Success = 0 --Bit Flag instead? Probably better.
		if NOT EXISTS(SELECT null 
					FROM SEIDR.ExecutionStatus 
					WHERE ExecutionStatusCode = @ExecutionStatusCode 
					AND [NameSpace] = @ExecutionStatusNameSpace 
					AND IsComplete = 0 AND IsError = 0) --Just return if there's nothing here.
		BEGIN
			RETURN 0
		END
		--SET @ExecutionStatusCode = 'W'
		--SET @ExecutionStatusNameSpace = 'SEIDR'
		
	END
	ELSE IF @Success = 0
	BEGIN
		
		SELECT 			
			@ExecutionStatusCode = ExecutionStatusCode, 
			@ExecutionStatusNameSpace = ExecutionStatusNameSpace,
			@StepNumber = StepNumber
		FROM SEIDR.vw_JobExecution 
		WHERE JobExecutionID = @JobExecutionID
		AND CanRetry = 1
		
		IF @@ROWCOUNT > 0
			SET @CanRetry = 1 
		ELSE IF @ExecutionStatusCode is null
		OR @ExecutionStatusNameSpace is null
		OR NOT EXISTS(SELECT null 
					FROM SEIDR.ExecutionStatus
					WHERE IsError = 1
					AND ExecutionSTatusCode = @ExecutionStatusCode
					AND [NameSpace] = @ExecutionStatusNameSpace)			
		BEGIN
			SET @ExecutionStatusCode = 'F'
			SET @ExecutionStatusNameSpace = 'SEIDR'
		END					
	END
	ELSE IF @Success = 1
	BEGIN
		IF @ExecutionStatusCode is null
		OR @ExecutionStatusNameSpace is null
		OR NOT EXISTS(SELECT null 
					FROM SEIDR.ExecutionStatus
					WHERE IsError = 0
					AND ExecutionSTatusCode = @ExecutionStatusCode
					AND [NameSpace] = @ExecutionStatusNameSpace)
		BEGIN
			SET @ExecutionStatusCode = 'SC' --StepComplete
			SET @ExecutionStatusNameSpace = 'SEIDR'
		END					

		IF NOT EXISTS(SELECT null
						FROM SEIDR.JobProfile_Job jpj
						WHERE JobProfileID = @JobProfileID
						AND StepNumber = @StepNumber + 1
						AND Active = 1
						AND (TriggerExecutionStatusCode is null or TriggerExecutionSTatusCode = @ExecutionStatusCode) 
						AND(TriggerExecutionNameSpace is null or TriggerExecutionNameSpace = @ExecutionStatusNameSpace)
						)
		BEGIN			
			set @Complete = 1--If there's no step work that's going to match up, complete.

			IF 0 = (SELECT IsComplete
					FROM SEIDR.ExecutionStatus
					WHERE ExecutionSTatusCode = @ExecutionStatusCode AND [NameSpace] = @ExecutionStatusNameSpace)
			BEGIN
				SET @ExecutionStatusCode = 'C'
				SET @ExecutionStatusNameSpace = 'SEIDR'
			END
		END						
		ELSE
			SET @StepNumber += 1
	END

	IF @Working = 1
	BEGIN
		UPDATE SEIDR.JobExecution
		SET FilePath = @FilePath,
			FileSize = @FileSize,
			FileHash = @FileHash,
			IsWorking = 1,
			InWorkQueue = 0,
			ExecutionStatusCode = @ExecutionStatusCode,
			ExecutionStatusNameSpace = @ExecutionStatusNameSpace,
			LastExecutionStatusCode = ExecutionStatusCode,
			LastExecutionStatusNameSpace = ExecutionStatusNameSpace
		WHERE JobExecutionID = @JobExecutionID

		RETURN
	END
	ELSE
	BEGIN
	
		UPDATE SEIDR.JobExecution
		SET FilePath = @FilePath,
			FileSize = @FileSize,
			FileHash = @FileHash,
			IsWorking = 0,
			InWorkQueue = CASE WHEN @CanRetry = 1 then 1 --@CanRetry is only set to 1 when success is 0 (failure) and the JobProfile_Job allows retry.
								WHEN @Success = 1 then 1 - @Complete
								else 0 end,
			StepNumber = @StepNumber,
			RetryCount = CASE 
							WHEN @StepNumber <> StepNumber then 0 -- new step Number, reset retry count
							WHEN @CanRetry = 0 then RetryCount --going to an error status.
							else RetryCount + 1 --Will retry with the same status.
							end,
			ExecutionStatusCode = @ExecutionStatusCode,
			ExecutionStatusNameSpace = @ExecutionStatusNameSpace,
			LastExecutionStatusCode = ExecutionStatusCode,
			LastExecutionStatusNameSpace = ExecutionStatusNameSpace,
			ExecutionTimeSeconds = @ExecutionTimeSeconds
		WHERE JobExecutionID = @JobExecutionID
	END
	

	IF @Success =1 AND @Complete = 0
	BEGIN
		SELECT * 
		FROM SEIDR.vw_JobExecution 
		WHERE JobExecutionID = @JobExecutionID
		AND InSequence = 1
	END
	else if @CanRetry = 1
	BEGIN
		SELECT *, DATEADD(minute, RetryDelay, GETDATE()) [DelayStart]
		FROM SEIDR.vw_JobExecution WHERE JobExecutionID = @JobExecutionID
	END

	IF @@ROWCOUNT = 0
		UPDATE SEIDR.JobExecution
		SET InWorkQueue = 0
		WHERE JobExecutionID = @JobExecutionID AND InWorkQueue = 1

END
