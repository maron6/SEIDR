CREATE PROCEDURE SEIDR.usp_JobProfile_u_InvalidPath
	@JobProfileID int
AS
BEGIN
	UPDATE SEIDR.JobProfile
	SET FileFilter = 'INVALID'
	WHERE JobProfileID = @JobProfileID

END