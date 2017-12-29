

CREATE PROCEDURE SEIDR.usp_Operation_Validate
	@OperationList SEIDR.udt_Operation readonly
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
	UPDATE o
	SET DD = IIF(s.Operation is null, COALESCE(o.DD, GETDATE()), null),
		Description = s.Description,
		ThreadID = s.ThreadID,
		ParameterSelect = s.ParameterSelect
	FROM SEIDR.Operation o	
	LEFT JOIN @OperationList s
		ON o.Operation = s.Operation
		AND o.OperationSchema = s.OperationSchema
		AND o.Version = s.Version

	INSERT INTO SEIDR.Operation(Operation, OperationSchema, Version, Description,
		ThreadID, ParameterSelect)
	SELECT Operation, OperationSchema, Version, Description, ThreadID, ParameterSelect
	FROM @OperationList s
	WHERE NOT EXISTS(SELECT null 
						FROM SEIDR.Operation 
						WHERE Operation = s.Operation
						AND OperationSchema = s.OperationSchema
						AND Version = s.Version)
	
END