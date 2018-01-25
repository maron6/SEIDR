CREATE TABLE [SEIDR].[ScheduleRule] (
    [ScheduleRuleID] INT           IDENTITY (1, 1) NOT NULL,
    [Description]    VARCHAR (250) NULL,
    [PartOfDateType] VARCHAR (4)   NULL,
    [PartOfDate]     INT           NULL,
    [IntervalType]   VARCHAR (4)   NULL,
    [IntervalValue]  INT           NULL,
    [DC]             SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [Creator]        VARCHAR (128) DEFAULT (suser_name()) NOT NULL,
    [DD]             SMALLDATETIME NULL,
    [Active]         AS            (CONVERT([bit],case when [dd] IS NULL then (1) else (0) end)) PERSISTED NOT NULL,
    PRIMARY KEY CLUSTERED ([ScheduleRuleID] ASC),
    CHECK ([IntervalType] IS NULL OR [IntervalValue]>(0)),
    CHECK ([PartOfDateType] IS NOT NULL OR [IntervalType] IS NOT NULL),
    CHECK ([PartOfDateType] IS NULL OR [PartOfDate] IS NOT NULL)
);

