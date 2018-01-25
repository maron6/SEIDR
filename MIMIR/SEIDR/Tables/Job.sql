CREATE TABLE [SEIDR].[Job] (
    [JobID]          INT           IDENTITY (1, 1) NOT NULL,
    [JobName]        VARCHAR (128) NOT NULL,
    [Description]    VARCHAR (256) NULL,
    [JobNameSpace]   VARCHAR (128) NOT NULL,
    [ThreadName]     VARCHAR (128) NULL,
    [SingleThreaded] BIT           NOT NULL,
    [DC]             DATETIME      DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([JobID] ASC)
);

