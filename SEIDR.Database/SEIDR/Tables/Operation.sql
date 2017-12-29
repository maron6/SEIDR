CREATE TABLE [SEIDR].[Operation] (
    [OperationID]     INT           IDENTITY (1, 1) NOT NULL,
    [Operation]       VARCHAR (128) NOT NULL,
    [OperationSchema] VARCHAR (128) NOT NULL,
    [Description]     VARCHAR (300) NULL,
    [Version]         INT           NOT NULL,
    [ThreadID]        TINYINT       NULL,
    [ParameterSelect] VARCHAR (140) NULL,
    [DD]              SMALLDATETIME NULL,
    [Active]          AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED NOT NULL,
    CONSTRAINT [PK_Operation] PRIMARY KEY CLUSTERED ([OperationID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Operation_Op_Sch_V]
    ON [SEIDR].[Operation]([Operation] ASC, [OperationSchema] ASC, [Version] ASC)
    INCLUDE([ThreadID], [ParameterSelect]);

