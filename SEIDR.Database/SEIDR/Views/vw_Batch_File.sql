CREATE VIEW SEIDR.vw_Batch_File
AS
	SELECT f.Batch_FileID, b.*, f.FilePath, f.OperationSuccess, f.FileDate, f.FileName, f.FileHash
	FROM SEIDR.vw_Batch b
	JOIN SEIDR.Batch_File f
		ON b.BatchID = f.BatchID
	WHERE f.Active = 1
