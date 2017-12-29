CREATE TABLE [SEIDR].[BatchStatus] (
    [BatchStatusCode] VARCHAR (2)   NOT NULL,
    [Description]     VARCHAR (100) NULL,
    [IsError]         BIT           DEFAULT ((0)) NOT NULL,
    [IsComplete]      BIT           DEFAULT ((0)) NOT NULL,
    [CanContinue]     BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BatchStatusCode] ASC),
    CHECK ([CanContinue]=(0) OR [IsComplete]=(0))
);


GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE TRIGGER [SEIDR].[trg_BatchStatus_UD]
   ON  [SEIDR].[BatchStatus]
   AFTER UPDATE, DELETE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	IF EXISTS(SELECT null 
				FROM INSERTED 
				WHERE BatchStatusCode IN ('C','CE','IV','R','RL','S','SA'
											,'SE','SK','SR','SX'/*,'W','F'*/ )
				)
	OR EXISTS(SELECT null 
				FROM(SELECT BatchStatusCode 
						FROM DELETED 
						WHERE BatchStatusCode IN ('C','CE','IV','R','RL','S','SA'
											,'SE','SK','SR','SX'/*,'W','F'*/ )
					EXCEPT
					SELECT BatchStatusCode 
						FROM INSERTED
					)d 
			)
	BEGIN
		ROLLBACK TRANSACTION
		RAISERROR('Cannot Update or Remove the base statuses: C, CE, IV, R, RL, S, SA, SE, SK, SR, SX'/*, W, F' */, 
			16, 1)
	END
    -- Insert statements for trigger here

END
