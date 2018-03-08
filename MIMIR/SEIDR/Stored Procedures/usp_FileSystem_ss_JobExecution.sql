CREATE PROCEDURE [SEIDR].[usp_FileSystem_ss_JobExecution]
	@JobProfile_JobID int
AS
	SELECT *
	FROM SEIDR.FileSystemJob
	WHERE JobProfile_JobID = @JobProfile_JobID
RETURN 0
