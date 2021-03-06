CREATE TYPE SEIDR.udt_JobMetaData
AS
TABLE (JobName varchar(128) not null, Description varchar(256), JobNameSpace varchar(128) not null, ThreadName varchar(128), SingleThreaded bit not null default(0)) 
GO

CREATE PROCEDURE SEIDR.usp_Job_Validate
	@JobList SEIDR.udt_JobMetaData readonly
as
BEGIN
	UPDATE j
	SET ThreadName = l.ThreadName,
		SingleThreaded = l.SingleThreaded,
		Description = l.Description,
		DD = null
	FROM SEIDR.Job j
	JOIN @JobList l
		ON j.JobName = l.JobName
		AND j.JobNameSpace = l.JobNameSpace
	
	UPDATE j
	SET DD = GETDATE()
	FROM SEIDR.Job j
	WHERE Active =1  AND NOT EXISTS(SELECT null FROM @JobList WHERE JobName = j.JobName AND JobNameSpace = j.JobNameSpace)
	
	INSERT INTO SEIDR.Job(JobName, JobNameSpace, ThreadName, SingleThreaded, Description)
	SELECT JobName, JobNameSpace, ThreadName, SingleThreaded, Description
	FROM @JobList l
	WHERE NOT EXISTS(SELECT null FROM SEIDR.[Job] WHERE JobName = l.JobName AND JobNameSpace = l.JobNameSpace)

	

END



  