CREATE TABLE [SEIDR].[Profile_Operation] (
    [Profile_OperationID]   INT           IDENTITY (1, 1) NOT NULL,
    [BatchProfileID]        INT           NOT NULL,
    [OperationID]           INT           NOT NULL,
    [Step]                  TINYINT       NOT NULL,
    [FailureDelay]          SMALLINT      DEFAULT ((5)) NOT NULL,
    [OnSuccessNotification] VARCHAR (350) NULL,
    [QualifyingBatchStatus] VARCHAR (2)   NULL,
    [CanRunOnErrorStatus]   BIT           DEFAULT ((0)) NOT NULL,
    [CanRunOnSuccessStatus] BIT           DEFAULT ((1)) NOT NULL,
    [DD]                    SMALLDATETIME NULL,
    [Active]                AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED NOT NULL,
    PRIMARY KEY CLUSTERED ([Profile_OperationID] ASC),
    CHECK ((CONVERT([int],[CanRunOnErrorStatus])+CONVERT([int],[CanRunOnSuccessStatus]))=(1)),
    CHECK ([FailureDelay]>(0)),
    CHECK ([Step]>(0)),
    FOREIGN KEY ([BatchProfileID]) REFERENCES [SEIDR].[BatchProfile] ([BatchProfileID]),
    FOREIGN KEY ([OperationID]) REFERENCES [SEIDR].[Operation] ([OperationID]),
    FOREIGN KEY ([QualifyingBatchStatus]) REFERENCES [SEIDR].[BatchStatus] ([BatchStatusCode])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Profile_Op_QualifyingBatchStatus]
    ON [SEIDR].[Profile_Operation]([BatchProfileID] ASC, [Step] ASC, [QualifyingBatchStatus] ASC) WHERE ([QualifyingBatchStatus] IS NOT NULL);

