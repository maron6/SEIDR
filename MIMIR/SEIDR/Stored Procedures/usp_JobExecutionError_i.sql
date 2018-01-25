CREATE PROCEDURE SEIDR.usp_JobExecutionError_i
	@JobExecutionID bigint,
	@ThreadID smallint,
	@ErrorDescription varchar(4000),
	@ExtraID int
AS
BEGIN
	DECLARE @StepNumber smallint, @JobProfile_JobID int
	SELECT @StepNumber = StepNumber, 
			@JobProfile_JobID = JobProfile_JobID
	FROM SEIDR.vw_JobExecution
	WHERE JobExecutionID = @JobExecutionID

	INSERT INTO SEIDR.JobExecutionError(JobExecutionID, StepNumber, JobProfile_JobID,
		ThreadID, ErrorDescription, ExtraID)
	VALUES(@JobExecutionID, @StepNumber, @JobProfile_JobID,
		@ThreadID, @ErrorDescription, @ExtraID)

END