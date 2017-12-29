CREATE PROCEDURE SEIDR.usp_Batch_BulkRegister
	@BatchProfileID int,	
	@FileXML XML
AS
BEGIN
	IF @FileXML is null
		RETURN -1
		
	DECLARE @RegisterStatus varchar(2) = 'RL',  --Create batch from files.
			@ScheduleStatus varchar(2) = 'RS', --Batch created from Schedule. Already exists, but also has an input folder
			@MaxFileCount tinyint,
			@MinFileCount tinyint,
			@UseFileDate bit = 1,
			@BatchDate date,
			@Error int = 0, @RowCount int = 0
	SELECT 		
		@MaxFileCount = t.MaxFileCount,-- Max number of files that the FileWatch can REGISTER to a Batch. Operations can add/remove as needed...
		@MinFileCount = t.MinFileCount,
		@UseFileDate = t.BatchDateFromFileDate
		FROM SEIDR.BatchProfile p
		JOIN SEIDR.[BatchType] t
			ON p.BatchTypeCode = t.BatchTypeCode		
		WHERE BatchProfileID = @BatchProfileID			

	IF OBJECT_ID('tempdb..#FileDoc') IS NOT NULL
		DROP TABLE #FileDoc
	IF OBJECT_ID('tempdb..#BatchFile_Deleted') IS NOT NULL
		DROP TABLE #BatchFile_Deleted
	IF OBJECT_ID('tempdb..#BatchList') IS NOT NULL
		DROP TABLE #BatchList

	CREATE TABLE #FileDoc (RowID int primary key identity(1,1), InputFilePath varchar(300),
		InputFileDate date, FileHash varbinary(88), InputFileSize bigint, BatchID int null)
	CREATE TABLE #BatchFile_Deleted (FilePath varchar(260) not null)
	CREATE TABLE #BatchList (BatchID int not null primary key, FileCount int not null default(0))

	
	INSERT INTO #FileDoc(InputFilePath, InputFileDate, FileHash, InputFileSize)
	SELECT 
		fileData.value('@FilePath', 'varchar(300)'),
		fileData.value('@FileDate', 'date'),
		fileData.value('@FileHash', 'varchar(88)'),
		fileData.value('@FileSize', 'bigint')
	FROM @FileXML.nodes('/BatchFiles/File') as IFile(fileData)
	SELECT @Error = @@ERROR, @RowCount = @@ROWCOUNT
	IF @ERROR <> 0 OR @RowCount = 0
	BEGIN
		RAISERROR('Could not insert any file information into #FileDoc', 16, 1)
		RETURN @ERROR
	END

	DELETE d 
	OUTPUT DELETED.InputFilePath INTO #BatchFile_Deleted(FilePath)	
	FROM #FileDoc d
	WHERE EXISTS(SELECT null 
				 FROM SEIDR.Batch b
				 JOIN SEIDR.Batch_File f
					ON b.BatchID = f.BatchID
					--AND f.Active = 1 --If it was already deleted for the same profile... Reject.
				 WHERE b.BatchProfileID = @BatchProfileID
				 AND b.Active = 1
				 AND (
					f.OriginalFilePath = d.InputFilePath
					OR f.OriginalFileHash = d.FileHash
					AND f.FileDate = d.InputFileDate --Reject for same hash if the file date matches
					) 
				)	
	IF @@ROWCOUNT = @RowCount --All records in the #FileDoc were deleted, no files to register
	GOTO PROC_RETURN
	
						
	DECLARE @RowID int, 
			@BatchID int

	IF @UseFileDate = 1
	BEGIN
		DECLARE FileDate_Cursor CURSOR 
		--STATIC --Might decide to remove records from #fileDoc while working?
		FOR SELECT distinct InputFileDate FROM #FileDoc
		DECLARE @FileDate date

		OPEN FileDate_Cursor

		FETCH NEXT FROM FileDate_Cursor INTO @FileDate
		WHILE @@FETCH_STATUS = 0
		BEGIN			
			
			SET @BatchDate = @FileDate

			UPDATE b WITH (READPAST)
			SET Locked = 1 
			OUTPUT INSERTED.BatchID, INSERTED.FileCount INTO #BatchList(BatchID, FileCount)
			FROM SEIDR.Batch b				
			WHERE BatchProfileID = @BatchProfileID	
			AND BatchStatusCode IN (@ScheduleStatus, @RegisterStatus)
			AND b.Active = 1
			--AND b.LastQueued is null --Make sure operations haven't started if it's a 
			--Should be fine to ignore as long as it's not locked and has room for more files
			AND b.BatchDate = @BatchDate
			AND FileCount < @MaxFileCount --Batch_File trigger should keep the count correct
			AND Locked = 0
			--Ok to pick up where Queued = 1
			--Note that Files list on the Batch object in c# is Lazy grabbed, should not be populated
			--Ok to leave Queued alone if it's already 1, since usp_Batch_StartWork will try to get lock after
			-- it's popped from the queue/error if still locked

			WHILE EXISTS(SELECT null FROM #FileDoc WHERE InputFileDate = @FileDate AND BatchID is null)
			BEGIN
				SELECT TOP 1 @RowID = RowID
				FROM #FileDoc
				WHERE InputFileDate = @FileDate
				AND BatchID is null
			
				SET @BatchID = null

				SELECT TOP 1 @BatchID = BatchID 
				FROM #batchList 
				WHERE FileCount < @MaxFileCount
				--batch list is populated for given @BatchDate, 
				--so only need to check that we haven't exceeded file count

				IF @@ROWCOUNT = 0 --@batchID is null
				BEGIN
					INSERT INTO SEIDR.Batch(BatchProfileID, BatchStatusCode, Locked,						
						BatchTypeCode, Priority, BatchDate)
					SELECT @BatchProfileID, @RegisterStatus, 1,						
						BatchTypeCode, DefaultPriority, @BatchDate --@BatchDate=@FileDate					
					FROM SEIDR.BatchProfile
					WHERE BatchProfileID = @BatchProfileID

					SELECT @BatchID = SCOPE_IDENTITY()
					INSERT INTO #BatchList(BatchID, FileCount)
					VALUES(@BatchID, 0)
				END

				UPDATE #FileDoc
				SET BatchID = @BatchID 
				WHERE RowID = @RowID
			
				INSERT INTO SEIDR.Batch_File(BatchID, FilePath, FileSize, 
					FileHash, FileDate, OriginalFilePath, OriginalFileHash, OriginalFileSize)
				SELECT @BatchID, f.InputFilePath, InputFileSize, 
					FileHash, @FileDate, f.InputFilePath, f.FileHash, f.InputFileSize
				FROM #FileDoc f
				WHERE RowID = @RowID

				UPDATE #batchList
				SET FileCount += 1
				WHERE BatchID = @BatchID
			END

			UPDATE bs WITH (ROWLOCK)
			SET Locked = 0
			FROM SEIDR.Batch bs
			JOIN #BatchList l
				ON l.BatchID = bs.BatchID
			WHERE bs.FileCount < @MaxFileCount	--Done with the current @FileDate. 
												--Truncate BatchList and move onto the next batch date
						
			TRUNCATE TABLE #BatchList
			FETCH NEXT FROM FileDate_Cursor into @FileDate
		END
		CLOSE FileDate_Cursor;
		DEALLOCATE FileDate_Cursor;
				
	END --@UseFileDate = 1
	ELSE 
	BEGIN --@UseFileDate = 0
		SET @BatchDate = GETDATE() --BatchDate is from creation date, separate from FileDates
		
		UPDATE b WITH (READPAST)
		SET Locked = 1 
		OUTPUT INSERTED.BatchID, INSERTED.FileCount INTO #BatchList(BatchID, FileCount)
		FROM SEIDR.Batch b			
		WHERE BatchProfileID = @BatchProfileID	
		AND b.BatchStatusCode IN (@ScheduleStatus, @RegisterStatus)
		AND b.Active = 1
		--AND b.LastQueued is null --Make sure operations haven't started if it's a
		AND b.BatchDate = @BatchDate
		AND FileCount < @MaxFileCount --Batch_File trigger should keep the count correct
		AND Locked = 0 --In use somewhere else already
		--AND Queued = 0 -- should be ok if it's Queued, Locking will prevent operation from starting work.
		--Note that Files list on the Batch object in c# is Lazy grabbed, should not be populated

		WHILE EXISTS(SELECT null FROM #FileDoc WHERE BatchID is null)
		BEGIN
			SELECT TOP 1 @RowID = RowID
			FROM #FileDoc
			WHERE BatchID is null
			
			--SET @BatchID = null --Shouldn't really be necessary since we check for @@ROWCOUNT = 0

			SELECT TOP 1 @BatchID = BatchID 
			FROM #batchList
			WHERE FileCount < @MaxFileCount

			IF @@ROWCOUNT = 0
			BEGIN
				INSERT INTO SEIDR.Batch(BatchProfileID, BatchStatusCode, Locked,
					BatchTypeCode, Priority, BatchDate)
				SELECT @BatchProfileID, @RegisterStatus, 1,
					BatchTypeCode, DefaultPriority, @BatchDate --Current Date (Creation)					
				FROM SEIDR.BatchProfile
				WHERE BatchProfileID = @BatchProfileID

				SELECT @BatchID = SCOPE_IDENTITY()
				INSERT INTO #BatchList(BatchID, FileCount)
				VALUES(@BatchID, 0)
			END

			UPDATE #FileDoc
			SET BatchID = @BatchID 
			WHERE RowID = @RowID
			
			INSERT INTO SEIDR.Batch_File(BatchID, FilePath, FileSize, FileHash, 
				FileDate, OriginalFilePath, OriginalFileHash, OriginalFileSize)
			SELECT @BatchID, f.InputFilePath, InputFileSize, FileHash, 
				@FileDate, InputFilePath, FileHash, InputFileSize
			FROM #FileDoc f
			WHERE RowID = @RowID

			UPDATE #batchList
			SET FileCount += 1
			WHERE BatchID = @BatchID
		END

		UPDATE b WITH (ROWLOCK)
		SET Locked = 0
		FROM SEIDR.Batch b
		JOIN #BatchList l
			ON l.BatchID = b.BatchID
		WHERE b.FileCount < @MaxFileCount -- Done, unlock the batches that can still register more files.
	END -- @UseFileDate = 0
	
	DELETE #BatchList

	UPDATE b
	SET 
		LastQueued = GETDATE(), 	
		Queued = 1, 
		Locked = 0, --Remove lock now, replace with 'Queued'
		Priority = null
	OUTPUT inserted.BatchID, inserted.FileCount INTO #BatchList(BatchID, FileCount)
	FROM SEIDR.Batch b	
	JOIN #FileDoc f
		ON f.BatchID = b.BatchID
		AND Locked = 1
	CROSS APPLY(SELECT	COUNT(p.ParentProfileID) [ParentProfileCount], 
						COUNT(pb.BatchID) [QualifyingBatchCount]
				FROM SEIDR.BatchProfile_Profile p
				LEFT JOIN SEIDR.vw_Batch_ParentBatch pb 
					ON pb.BatchID = b.BatchID
					AND pb.ParentBatchProfileID = p.ParentProfileID
				WHERE p.BatchProfileID = b.BatchProfileID)qp --Modified version of the Batch_Runnable view..
	WHERE CanContinue = 1	
	AND InSequence = 1		
	AND (b.IgnoreParents = 1 OR qp.ParentProfileCount = qp.QualifyingBatchCount )	
	--AND FileCount >= @MaxFileCount --Max must be > Min, so only need to check >= max

	-- Above sections set Locked to 0 for Batches with FileCount < @Max, 
	-- so Locked = 1 is a more efficient check due to being able to use an equality column from indexes
	
	PROC_RETURN:

	SELECT b.* 
	FROM SEIDR.vw_Batch b
	JOIN #BatchList l --If nothing was in #FileDoc, will select nothing.
	ON b.BatchID = l.BatchID
	
	SELECT f.* 
	FROM SEIDR.Batch_File f
	JOIN #BatchList l
	ON f.BatchID = l.BatchID

	SELECT * 
	FROM #BatchFile_Deleted --Not allowed

	DROP TABLE #FileDoc
	DROP TABLE #BatchList
	DROP TABLE #BatchFile_Deleted
END
