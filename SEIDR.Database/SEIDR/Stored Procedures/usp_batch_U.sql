
CREATE PROCEDURE [SEIDR].[usp_batch_U]
	@BatchID int,
	@BatchProfileID int,	
	@Step smallint, --Step is a validation... CurrentStep on the batch will be incremented if operation success and there are more batches.
	@BatchStatusCode varchar(2),
	@OperationSuccess bit,
	@FileXML XML = null
AS
BEGIN	
	DECLARE @TranCount int = @@TRANCOUNT
	DECLARE @SavePoint varchar(128) = 'SEIDR.usp_Batch_U'
	IF @TranCount > 0
		SAVE TRANSACTION @SavePoint
	ELSE
		BEGIN TRAN

	BEGIN TRY
		DECLARE @Done bit = 0, 
				@IsErrorStatus bit = 0, 
				@ValidatedStatus varchar(2) = null, 
				@UpdateFiles bit = 1


		IF @FileXML is null
			SET @UpdateFiles = 0

		SELECT 
			@ValidatedStatus = BatchStatusCode, 
			@Done = IsComplete, 
			@IsErrorStatus = IsError
		FROM SEIDR.BatchStatus
		WHERE BatchStatusCode = @BatchStatusCode
		AND IsError <> @OperationSuccess -- = (1 - @OperationSuccess)
		IF @@ROWCOUNT = 0	
			SELECT @ValidatedStatus = IIF(@OperationSuccess = 1, 'S', null /*'F'*/) --Done and isError = 0 already

		--If there is no operation for the profile that's a higher step and for the same Error category, mark as done.	
		IF @OperationSuccess = 1
		AND
		(@Done = 1 --Validate C vs CE.
		OR NOT EXISTS(SELECT null 
						FROM SEIDR.Profile_Operation 
						WHERE BatchProfileID = @BatchProfileID 
						AND (@IsErrorStatus = 0 AND CanRunOnSuccessStatus = 1
							OR @IsErrorStatus = 1 AND CanRunOnErrorStatus = 1 --Both could be true.
							)					
						AND (
							QualifyingBatchStatus is null
							OR QualifyingBatchStatus = @ValidatedStatus
						)
						AND Active = 1
						AND Step >= @Step)					
		)
		BEGIN
			SET @Done = 1
			SELECT @IsErrorStatus = CASE --Final status can be changed to account for overall
						WHEN @IsErrorStatus = 1 then 1 --Don't allow C if the passed status was going to be an Error status.
						WHEN EXISTS(SELECT null 
									FROM SEIDR.Batch_Error WITH (NOLOCK)
									WHERE BatchID = @BatchID) then 1 
						else 0 
						end
		
			SELECT @ValidatedStatus = BatchStatusCode
			FROM SEIDR.BatchStatus
			WHERE BatchStatusCode = @BatchStatusCode
				AND IsError = @IsErrorStatus AND IsComplete = 1
				AND CanContinue = 0 --Should really have a check that CanContinue is 0 if IsComplete = 1
			IF @@ROWCOUNT = 0
				SET @ValidatedStatus = IIF(@IsErrorStatus = 1, 'CE', 'C') --Default statuses for done.
		END

		UPDATE SEIDR.Batch
		SET 
			-- @ValidatedStatus should only be null if OperationSuccess = 0 
			-- and no status provided (Keep current status)
			BatchStatusCode = ISNULL(@ValidatedStatus, BatchStatusCode), 		
			CurrentStep = IIF(@OperationSuccess = 1 AND @Done = 0, @Step + 1, @Step),		 
			LU = GETDATE(),
			OperationEndTime = GETDATE(),
			Locked = @UpdateFiles --If no files to update, will leave immediately, so unlock now.
		WHERE BatchID = @BatchID
		AND CurrentStep = @Step
		

		IF @UpdateFiles = 0
			GOTO PROC_END

		CREATE TABLE #FileDoc (Batch_FileID int primary key, InputFilePath varchar(300),
		InputFileDate date, FileHash varbinary(88), InputFileSize bigint, OperationSuccess bit)
	
		INSERT INTO #FileDoc(Batch_FileID, InputFilePath, InputFileDate, FileHash, InputFileSize, OperationSuccess)
		SELECT 
			fileData.value('@Batch_FileID', 'int'),
			fileData.value('@FilePath', 'varchar(300)'),
			fileData.value('@FileDate', 'date'),
			fileData.value('@FileHash', 'varchar(88)'),
			fileData.value('@FileSize', 'bigint'),
			fileData.value('@OperationSuccess', 'bit')
		FROM @FileXML.nodes('/BatchFiles/File') as IFile(fileData)

		UPDATE f
		SET FilePath = InputFilePath,
			FileHash = d.FileHash,
			FileSize = d.InputFileSize,		
			DD = null,
			OperationSuccess = IIF(@OperationSuccess = 1 AND @Done = 0, 0, d.OperationSuccess)
		FROM SEIDR.Batch_File f
		JOIN #FileDoc d
		ON f.Batch_FileID = d.Batch_FileID
	
		--Insert new files. Note that any files created by the batch step will be considered success off the bat 
		--(unless operation success was 1, in which case, force to 0 because the step has changed and it will be a new operation)...
		INSERT INTO SEIDR.Batch_File(BatchID, FilePath, FileSize, FileHash, 
			FileDate, OriginalFilePath, OriginalFileHash, OriginalFileSize, 
			OperationSuccess)
		SELECT @BatchID, f.InputFilePath, InputFileSize, FileHash, 
			f.InputFileDate, InputFilePath, FileHash, InputFileSize, 
			IIF(@OperationSuccess = 1 AND @Done = 0, 0, 1) 
			/*
				Only Set Success to 1 if the overall operation wasn't a success 
				and there are more operations
			*/
		FROM #FileDoc f
		WHERE Batch_FileID is null
	
		--DELETE f
		UPDATE f SET DD = GETDATE()
		FROM SEIDR.Batch_File f
		WHERE BatchID = @BatchID 
		AND Active = 1
		AND NOT EXISTS(SELECT null FROM #FileDoc WHERE Batch_FileID = f.Batch_FileID) 

		UPDATE SEIDR.Batch
		SET Locked = 0, LU = GETDATE()
		WHERE BatchID = @BatchID

		DROP TABLE #FileDoc

		PROC_END:
		IF @TranCount = 0
			COMMIT TRAN

	END TRY
	BEGIN CATCH

		IF @TranCount = 0
			ROLLBACK TRAN
		ELSE 
			ROLLBACK TRAN @SavePoint

	END CATCH
END