CREATE TABLE [SEIDR].[Batch_File] (
    [Batch_FileID]     INT            IDENTITY (1, 1) NOT NULL,
    [BatchID]          INT            NOT NULL,
    [FilePath]         VARCHAR (260)  NOT NULL,
    [FileName]         AS             (case when charindex('\',[FilePath])=(0) then [FilePath] else right([FilePath],charindex('\',reverse([FilePath]))-(1)) end) PERSISTED,
    [FileSize]         BIGINT         DEFAULT ((0)) NOT NULL,
    [FileHash]         VARBINARY (88) NOT NULL,
    [OriginalFilePath] VARCHAR (260)  NULL,
    [OriginalFileSize] BIGINT         NULL,
    [OriginalFileHash] VARCHAR(88) NULL,
    [FileDate]         DATE           NOT NULL,
    [OperationSuccess] BIT            DEFAULT ((0)) NOT NULL,
    [DD]               SMALLDATETIME  NULL,
    [Active]           AS             (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED,
    PRIMARY KEY CLUSTERED ([Batch_FileID] ASC),
    FOREIGN KEY ([BatchID]) REFERENCES [SEIDR].[Batch] ([BatchID])
);


GO
CREATE TRIGGER SEIDR.Batch_File_UD_Archive
	ON SEIDR.Batch_File 
	AFTER UPDATE, DELETE
	AS
	BEGIN
		INSERT INTO SEIDR.Batch_File_History(Batch_FileID,
		FilePath, [FileName], FileHash, DeleteDate)
		SELECT d.Batch_FileID, d.FilePath, d.[FileName], d.FileHash, i.DD
		FROM DELETED d
		LEFT JOIN INSERTED i
			ON d.Batch_FileID = i.Batch_FileID
		WHERE i.Batch_FileID is null OR d.FilePath <> i.FilePath OR d.FileHash <> i.FileHash
		OR d.Active <> i.Active
	END

GO
CREATE TRIGGER SEIDR.Batch_File_IUD
	ON SEIDR.Batch_File
	AFTER INSERT, UPDATE, DELETE
	AS
	BEGIN
		IF NOT EXISTS(
			SELECT Batch_FileID
			FROM INSERTED i
			INTERSECT 
			SELECT Batch_FileID
			FROM DELETED d
		) 
		OR EXISTS(
			SELECT null 
			FROM INSERTED i
			JOIN DELETED d
			ON i.Batch_FileID = d.Batch_FileID
			WHERE i.Active <> d.Active)
		BEGIN
			UPDATE b
			SET FileCount = [ActiveFileCount]
			FROM SEIDR.Batch b
			CROSS APPLY(SELECT COUNT(*) [ActiveFileCount]
						FROM SEIDR.Batch_File
						WHERE BatchID = b.BatchID
						AND Active = 1)f
			WHERE BatchID IN (SELECT distinct BatchID
								FROM INSERTED
								UNION SELECT distinct BatchID
								FROM DELETED)
		END
	END