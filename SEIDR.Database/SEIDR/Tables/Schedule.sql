CREATE TABLE [SEIDR].[Schedule] (
    [ScheduleID]  INT           IDENTITY (1, 1) NOT NULL,
    [Description] VARCHAR (100) NOT NULL,
    [DD]          SMALLDATETIME NULL,
    [Active]      AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED,
    [DateFrom]    DATE          DEFAULT (getdate()) NOT NULL,
    [DateThrough] DATE          NULL,
    PRIMARY KEY CLUSTERED ([ScheduleID] ASC)
);

