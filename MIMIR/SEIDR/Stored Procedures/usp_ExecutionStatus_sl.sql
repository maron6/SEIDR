
CREATE PROCEDURE [SEIDR].[usp_ExecutionStatus_sl]
	@ExecutionStatusCode varchar(2) = null,
	@NameSpace varchar(128) = null,
	@IsComplete bit = null,
	@IsError bit = null
as begin
	SELECT * FROM SEIDR.ExecutionStatus WITH (NOLOCK)
	WHERE (@NameSpace is null or [NameSpace] = @NameSpace)
	AND (@ExecutionStatusCode is null or ExecutionStatusCode = @ExecutionStatusCode)
	AND (@IsComplete is null or IsComplete = @IsComplete)
	AND (@IsError is null or IsError = @IsError)
End
