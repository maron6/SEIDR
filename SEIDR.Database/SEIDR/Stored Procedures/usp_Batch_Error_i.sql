CREATE PROCEDURE SEIDR.usp_Batch_Error_i
		@BatchID int,
		@Message varchar(6000),
		@Extra varchar(100) = null,
		@ThreadID tinyint = null,
		@Batch_FileID int = null		
	AS
	BEGIN
		DECLARE @Profile_OpID int = null
				, @CurrentStep smallint 
		IF @ThreadID < 1
			SET @ThreadID = null --Shouldn't happen...
		SELECT 
			--@ThreadID = ExecutionThreadID, --Thread of current OperationID, or batch type, or Profile
			@Profile_OpID = Profile_OperationID,-- Profile_Operation link pointed to by current step of batch.
			@CurrentStep = Step
		FROM SEIDR.vw_Batch
		WHERE BatchID = @BatchID
		
		IF @@ROWCOUNT > 0
			INSERT INTO SEIDR.Batch_Error(BatchID, Batch_FileID, Message, Extra, 
			ThreadID, Profile_OperationID, Step)
			VALUES( @BatchID, @Batch_FileID, @Message, @Extra,
			@ThreadID, @Profile_OpID, @CurrentStep)		
	END