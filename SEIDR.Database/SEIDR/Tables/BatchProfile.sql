﻿CREATE TABLE [SEIDR].[BatchProfile] (
    [BatchProfileID]      INT           IDENTITY (1, 1) NOT NULL,
    [Description]         VARCHAR (160) NULL,
    [BatchTypeCode]       VARCHAR (5)   NOT NULL,
    [ThreadID]            TINYINT       NULL,
    [DefaultPriority]     TINYINT       NULL,
    [InputFolder]         VARCHAR (160) NULL,
    [ScheduleID]          INT           NULL,
    [FileMask]            VARCHAR (30)  DEFAULT ('*.*') NOT NULL,
    [InputFileDateFormat] VARCHAR (80)  DEFAULT ('*<YYYY><MM><DD>*') NULL,
    [DayOffset]           INT           DEFAULT ((0)) NOT NULL,
    [Sequenced]           BIT           DEFAULT ((0)) NOT NULL,
    [DC]                  SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [LU]                  SMALLDATETIME DEFAULT (getdate()) NOT NULL,
    [DD]                  SMALLDATETIME NULL,
    [Active]              AS            (CONVERT([bit],case when [DD] IS NOT NULL then (0) else (1) end)) PERSISTED,
    [CB]                  VARCHAR (128) DEFAULT (suser_name()) NOT NULL,
    [UB]                  VARCHAR (128) DEFAULT (suser_name()) NOT NULL,
    [UserKey]             VARCHAR (300) NULL,
    [LastRegistration]    SMALLDATETIME NULL,
    PRIMARY KEY CLUSTERED ([BatchProfileID] ASC),
    FOREIGN KEY ([BatchTypeCode]) REFERENCES [SEIDR].[BatchType] ([BatchTypeCode]),
    FOREIGN KEY ([ScheduleID]) REFERENCES [SEIDR].[Schedule] ([ScheduleID])
);

