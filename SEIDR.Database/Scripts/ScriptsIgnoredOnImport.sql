
INSERT INTO SEIDR.batchStatus(BatchStatusCode, Description,
	IsError, IsComplete, CanContinue)
	SELECT * FROM(
	SELECT 'RS' [StatusCode], 'Registered (From Schedule), Ready' [Description], 0 [Err], 0[Complete], 1 [Continue]
	UNION ALL
	SELECT 'RL', 'Registered (From Loading/InputFolder), Ready', 0, 0, 1
	UNION ALL
	--CE - finishes with records in Batch_error
	SELECT 'CE', 'Invalid complete, has errors', 1, 1, 0
	UNION ALL
	SELECT 'C', 'Complete', 0, 1, 0
	UNION ALL
	SELECT 'S', 'Step successful', 0, 0,1 
	--Set to S should override Error log entries? Or don't care really..
	--S vs F only care about operation result. C vs CE will check for batch errors
	UNION ALL 
	SELECT 'SE', 'Partial success, must be set by Operation assembly', 1, 0, 1
	UNION ALL 
	SELECT 'IV', 'Invalid - Force stop', 1, 0, 0
	UNION ALL
	SELECT 'SR', 'Stop Requested', 1, 0, 0
	UNION ALL
	SELECT 'SA', 'Stop Request Acknowledged', 1, 0, 0
	UNION ALL
	SELECT 'SX', 'Stopped', 1, 0, 0
	UNION ALL/* 
		--Replace Working/Failure with locking and unlocking. 
		--(Maintain status to ensure grabbing correct operation and to simplify status archiving/updating)
	SELECT 'W', 'Working', 0, 0, 0
	UNION ALL	
	SELECT 'F', 'Step Failure - Will retry', 1, 0, 1	
	UNION ALL*/
	SELECT 'SK', 'Skip - no work done, but can move to next step', 0, 0, 1
	)s
	WHERE NOT EXISTS(SELECT null 
						FROM SEIDR.BatchStatus 
						WHERE StatusCode = BatchStatusCode)
GO

INSERT INTO SEIDR.RepetitionIntervalType(RepetitionIntervalTypeCode, Description)
	VALUES('D', 'Repeat after N days'), ('W', 'Repeat after N weeks (DateDiff)'), ('M', 'Repeat after N Months')
	, ('Y', 'Repeat after N years'), ('DW', 'Repeat on days of week'), ('DM', 'Repeat on days of month'),
	('DY', 'Repeat on days of year')
GO
