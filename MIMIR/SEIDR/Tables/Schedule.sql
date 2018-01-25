CREATE TABLE [SEIDR].[Schedule] (
    [ScheduleID]  INT           IDENTITY (1, 1) NOT NULL,
    [Description] VARCHAR (250) NULL,
    [Creator]     VARCHAR (128) DEFAULT (suser_name()) NOT NULL,
    [DC]          DATETIME      DEFAULT (getdate()) NOT NULL,
    [DD]          DATETIME      NULL,
    [Active]      AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)),
    PRIMARY KEY CLUSTERED ([ScheduleID] ASC)
);

