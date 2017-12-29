CREATE TYPE [SEIDR].[udt_Operation] AS TABLE (
    [Operation]       VARCHAR (50)  NOT NULL,
    [OperationSchema] VARCHAR (40)  NOT NULL,
    [Version]         INT           NOT NULL,
    [Description]     VARCHAR (300) NULL,
    [ThreadID]        TINYINT       NULL,
    [ParameterSelect] VARCHAR (140) NULL,
    PRIMARY KEY CLUSTERED ([Operation] ASC, [OperationSchema] ASC, [Version] ASC));

