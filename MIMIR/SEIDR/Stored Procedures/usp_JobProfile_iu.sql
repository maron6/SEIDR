
CREATE PROCEDURE SEIDR.usp_JobProfile_iu
	@JobProfileID int out,
	@Description varchar(256) = null,
	@RequiredThreadID tinyint = null,
	@ScheduleID int = null,
	@ScheduleFromDate datetime = null,
	@ScheduleThroughDate datetime = null,
	@UserKey int = null,
	@UserKey1 varchar(50) = null,
	@UserKey2 varchar(50) = null,
	@RegistrationFolder varchar(250),
	@FileDateMask varchar(128),
	@FileFilter varchar(30),
	@SuccessNotificationMail varchar(500),
	@JobPriority varchar(10) = 'NORMAL'
AS
BEGIN
	IF @JobProfileID is null
	BEGIN
		INSERT INTO SEIDR.JobProfile(Description, UserKey, UserKey1, UserKey2,
									ScheduleID, RequiredThreadID, RegistrationFolder,
									FileFilter, SuccessNotificationMail,
									ScheduleFromDate, ScheduleThroughDate, JobPriority)
		VALUES(@Description, @UserKey, @UserKey1, @UserKey2,
				@ScheduleID, @RequiredThreadID, @RegistrationFolder,
				@FileFilter, @SuccessNotificationMail,
				@ScheduleFromDate, @ScheduleThroughDate, COALESCE(@JobPriority, 'NORMAL')
				)
		SET @JobProfileID = SCOPE_IDENTITY()
	END
	ELSE
	BEGIN
		UPDATE SEIDR.JobProfile
		SET Description = @Description,
			RegistrationFolder = @RegistrationFolder,
			FileFilter = @FileFilter,
			FileDateMask = @FileDateMask,
			UserKey = @UserKey,
			UserKey1 = @UserKey1,
			UserKey2 = @UserKey2,
			RequiredThreadID = @RequiredThreadID,
			SuccessNotificationMail = @SuccessNotificationMail, --Job Completion.
			ScheduleID = @ScheduleID,
			ScheduleFromDate = @ScheduleFromDate,
			ScheduleThroughDate = @ScheduleThroughDate,
			@JobPriority = COALESCE(@JobPriority, JobPriority)
		WHERE JobProfileID = @JobProfileID
			--LU = GETDATE()
	END
END