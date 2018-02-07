CREATE TABLE SEIDR.JobExecution_Note
(
	ID bigint identity(1,1) not null primary key,
	JobExecutionID bigint not null foreign key REFERENCES SEIDR.JobExecution(JobExecutionID),
	StepNumber smallint not null,
	JobProfile_JobID int null foreign key references SEIDR.JobProfile_Job(JobProfile_JobID),
	NoteText varchar(2000) not null,
	DC datetime not null default(GETDATE()),
	UserName varchar(128) not null default (SUSER_NAME()),
	SUID bigint null --Seidr UserID from window app
)

CREATE TABLE SEIDR.Priority
(
	PriorityCode varchar(10) not null primary key,
	PriorityValue tinyint check(PriorityValue BETWEEN 0 and 15)
)

INSERT INTO SEIDR.Priority(PriorityCode, PriorityValue)
SELECT 'LOW', 3
UNION ALL
SELECT 'NORMAL', 6
UNION ALL
SELECT 'HIGH', 12
UNION ALL
SELECT 'MAX', 15

ALTER TABLE SEIDR.JobExecution
ADD JobPriority varchar(10) not null foreign key references SEIDR.Priority(PriorityCode) default('NORMAL')

ALTER TABLE SEIDR.JobProfile_Job
ADD SequenceScheduleID int null foreign key references SEIDR.schedule(ScheduleID)

SELECT * FROM SEIDR.JobProfile_Job
ALTER TABLE SEIDR.Jobprofile_Job
DROP COLUMN SequenceScheduleID
 

 SELECT TOP 1 * FROM SEIDR.ExecutionStatus
 SELECT TOP 1 * FROM SEIDR.JobExecution

 SELECT * FROM SEIDR.vw_JobExecution

 SELECT TOP 1 * FROM SEIDR.JobProfile_Job