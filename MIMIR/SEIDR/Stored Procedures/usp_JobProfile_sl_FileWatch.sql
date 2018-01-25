CREATE PROCEDURE SEIDR.usp_JobProfile_sl_FileWatch
	@ThreadID int,
	@ThreadCount int
AS
BEGIN
	SELECT * 
	FROM SEIDR.JobProfile
	WHERE Active = 1
	AND JobProfileID % @ThreadCount = @ThreadID
	AND FileFilter is not null
	AND NULLIF(LTRIM(RTRIM(RegistrationFolder)),'') is not null
	AND FileFilter != 'INVALID'
	AND FileFilter NOT LIKE '%INACTIVE'	
END
