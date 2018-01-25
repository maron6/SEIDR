CREATE PROCEDURE [SEIDR].[usp_ExecutionStatus_i]
	@ExecutionStatusCode varchar(2),
	@NameSpace varchar(128),
	@IsComplete bit,
	@IsError bit,
	@Description varchar(60)
AS
BEGIN
	IF EXISTS(SELECT null FROM SEIDR.ExecutionStatus WHERE ExecutionStatusCode = @ExecutionStatusCode AND [NameSpace] = @NameSpace)
		RETURN

	INSERT INTO SEIDR.ExecutionStatus(ExecutionStatusCode, NameSpace, IsComplete, IsError, [Description])
	VALUES(@ExecutionStatusCode, @NameSpace, @IsComplete, @IsError, @Description)
END
