CREATE TABLE [SEIDR].[BatchType] (
    [BatchTypeCode]              VARCHAR (5)   NOT NULL,
    [Description]                VARCHAR (150) NOT NULL,
    [FileTypeDescription]        VARCHAR (100) NULL,
    [BatchDateFromFileDate]      BIT           DEFAULT ((1)) NOT NULL,
    [MinFileCount]               TINYINT       DEFAULT ((0)) NOT NULL,
    [MaxFileCount]               TINYINT       DEFAULT ((1)) NOT NULL,
    [ThreadID]                   TINYINT       DEFAULT ((1)) NULL,
    [ProfileCanOverrideThreadID] BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BatchTypeCode] ASC)
);

