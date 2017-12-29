CREATE PROCEDURE SEIDR.usp_Profile_CheckSchedules
AS
BEGIN	
	SET XACT_ABORT ON;
	DECLARE @TranCount int = @@TRANCOUNT
	DECLARE @Savepoint varchar(128) ='usp_Profile_CheckSchedules'
	BEGIN TRY

		IF @TranCount = 0
			BEGIN TRAN
		ELSE
			SAVE TRANSACTION @Savepoint

		DECLARE @ProfileList table (BatchProfileID int primary key)
		DECLARE @BatchDate date = GETDATE(), @Error int
		DECLARE @MinuteNow int = DATEPART(minute, GETDATE())
	
		
		INSERT INTO SEIDR.Batch(BatchProfileID, BatchTypeCode,
			--UserKey1, UserKey2, 
			BatchStatusCode, BatchDate, Priority, Locked, ScheduleID, RepetitionGroupID, LateRegistration)
		OUTPUT INSERTED.BatchProfileID INTO @ProfileList
		SELECT p.BatchProfileID, BatchTypeCode,
			--Key1, Key2, 
			'RS', @BatchDate,  DefaultPriority, 0, p.ScheduleID, s.FirstValidRepetitionGroupID, s.Late
		FROM SEIDR.vw_BatchProfile p
		JOIN SEIDR.vw_Schedule_BatchProfile s
			ON p.ScheduleID = s.ScheduleID		
			AND p.BatchProfileID = s.BatchProfileID
		WHERE s.Eligible = 1	
		AND 
		(
		PartialDayMatch = 1
		OR NOT EXISTS(SELECT null 
						FROM SEIDR.Batch
						WHERE BatchProfileID = p.BatchProfileID 
						AND Active = 1 
						AND 
							( 
							BatchDate = @BatchDate 							
								--HasPartialDayInterval = 1 
								--AND DATEDIFF(minute, DC, GETDATE()) >= (PartialHour * 60 + PartialMinute)														
							)
						) 
		)						
		--	SELECT * FROM SEIDR.BatchStatus
	
		UPDATE p
		SET LastRegistration = GETDATE()
		FROM SEIDR.BatchProfile p
		JOIN @ProfileList l
		ON p.BatchProfileID = l.BatchProfileID 
		SELECT @Error = @@ERROR
		
		IF @ERROR <> 0
			RAISERROR('Error Registering batches from Schedule! ErrorCode %d', 16, 1, @error)

		IF @TranCount = 0
			COMMIT TRAN
	END TRY
	BEGIN CATCH
		IF @TranCount = 0
			ROLLBACK TRAN
		ELSE
			ROLLBACK TRAN @Savepoint
	END CATCH
END
