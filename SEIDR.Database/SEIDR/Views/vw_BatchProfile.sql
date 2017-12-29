CREATE VIEW SEIDR.vw_BatchProfile
WITH SCHEMABINDING
AS
	SELECT 
	BatchProfileID,
	p.BatchTypeCode ,
	t.Description [BatchType],
	ISNULL(p.Description, 			
			+ ' ' 
			+ p.BatchTypeCode + ' Profile' 
			--+ ISNULL(', ' + k1.[Key] + ' - "' + k1.Keyvalue + '"', '')
			--+ ISNULL(', ' + k2.[Key] + ' - "' + k2.Keyvalue + '"', '') --Null key value will lead to ''
		)
		as [Profile],
	IIF(t.[ProfileCanOverrideThreadID] = 1, 
		ISNULL(p.ThreadID, t.ThreadID), 
		t.ThreadID) as [ThreadID],
	[InputFolder] ,	
	[ScheduleID] ,
	LastRegistration,
	Sequenced,	
	[FileMask] ,	
	ISNULL([InputFileDateFormat], '*<YYYY><MM><DD>*')[InputFileDateFormat],
	p.DayOffset,	
	[DefaultPriority],	 
	t.MinFileCount,
	t.MaxFileCount,
	UserKey,
	InvalidRegistration = CONVERT(bit,
				IIF(InputFolder IS NULL OR LEFT(InputFolder, 9) = '*INVALID*',
					1, 0) --Not valid for creating batches via registration --Maybe also 
				)
	FROM SEIDR.BatchProfile p
	JOIN SEIDR.BatchType t
		ON p.BatchTypeCode = t.BatchTypeCode	
	WHERE p.Active = 1

GO
CREATE UNIQUE CLUSTERED INDEX [pk_vw_BatchProfile_BatchProfileID]
    ON [SEIDR].[vw_BatchProfile]([BatchProfileID] ASC);


GO
CREATE NONCLUSTERED INDEX [idx_vw_BatchProfile_ExecutionTHread]
    ON [SEIDR].[vw_BatchProfile]([ThreadID] ASC);
GO
CREATE NONCLUSTERED INDEX idex_vw_BatchProfile_InputFolder_Format
	ON SEIDR.vw_BatchProfile(InputFolder,FileMask, InputFileDateFormat, DayOffset)	
	INCLUDE(InvalidRegistration)
GO

