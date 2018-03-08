CREATE PROCEDURE SEIDR.usp_JobProfile_ss
	@JobProfileID int
as
BEGIN
	SELECT * 
	FROM SEIDR.JobProfile
	WHERE JobProfileID = @JobProfileID
	AND Active = 1

END