CREATE PROCEDURE SEIDR.usp_Job_Validate
	@JobList SEIDR.udt_JobMetaData readonly
as
BEGIN
	UPDATE j
	SET ThreadName = l.ThreadName,
		SingleThreaded = l.SingleThreaded,
		Description = l.Description
	FROM SEIDR.Job j
	JOIN @JobList l
		ON j.JobName = l.JobName
		AND j.JobNameSpace = l.JobNameSpace
	
	
	INSERT INTO SEIDR.Job(JobName, JobNameSpace, ThreadName, SingleThreaded, Description)
	SELECT JobName, JobNameSpace, ThreadName, SingleThreaded, Description
	FROM @JobList l
	WHERE NOT EXISTS(SELECT null FROM SEIDR.[Job] WHERE JobName = l.JobName AND JobNameSpace = l.JobNameSpace)
END