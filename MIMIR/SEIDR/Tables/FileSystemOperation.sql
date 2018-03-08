CREATE TABLE [SEIDR].[FileSystemOperation]
(
	[FileSystemOperationCode] VARCHAR(30) NOT NULL PRIMARY KEY, 
    [Description] VARCHAR(200) NOT NULL, 
    [RequireSource] BIT NOT NULL DEFAULT 0, 
    [RequireDestination] BIT NOT NULL DEFAULT 0
)
