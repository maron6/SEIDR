CREATE TYPE [SEIDR].[udt_JobMetaData] AS TABLE (
    [JobName]        VARCHAR (128) NOT NULL,
    [Description]    VARCHAR (256) NULL,
    [JobNameSpace]   VARCHAR (128) NOT NULL,
    [ThreadName]     VARCHAR (128) NULL,
    [SingleThreaded] BIT           DEFAULT ((0)) NOT NULL);

