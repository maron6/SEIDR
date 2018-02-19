﻿CREATE TABLE [SEIDR].[JobProfile] (
    [JobProfileID]            INT           IDENTITY (1, 1) NOT NULL,
    [Description]             VARCHAR (256) NOT NULL,
    [RegistrationFolder]      VARCHAR (250) NULL,
    [FileDateMask]            VARCHAR (128) NULL,
    [FileFilter]              VARCHAR (30)  NULL,
    [RequiredThreadID]        TINYINT       NULL,
    [ScheduleID]              INT           NULL,
    [UserKey]                 INT           NULL,
    [UserKey1]                VARCHAR (50)  NULL,
    [UserKey2]                VARCHAR (50)  NULL,
    [Creator]                 VARCHAR (128) DEFAULT (suser_name()) NOT NULL,
    [DC]                      DATETIME      DEFAULT (getdate()) NOT NULL,
    [DD]                      DATETIME      NULL,
    [Active]                  AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)),
    [SuccessNotificationMail] VARCHAR (500) NULL,
    [JobPriority]             VARCHAR (10)  DEFAULT ('NORMAL') NOT NULL,
    [ScheduleFromDate]        DATETIME      NULL,
    [ScheduleThroughDate]     DATETIME      NULL,
    [ScheduleValid]           AS            (CONVERT([bit],case when [ScheduleID] IS NULL then (0) when [ScheduleFromDate] IS NULL OR [ScheduleFromDate]>getdate() then (0) when [ScheduleThroughDate] IS NULL then (1) when [ScheduleThroughDate]<=[ScheduleFromDate] then (0) else (1) end)),
    [ScheduleNoHistory] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([JobProfileID] ASC),
    FOREIGN KEY ([JobPriority]) REFERENCES [SEIDR].[Priority] ([PriorityCode]),
    FOREIGN KEY ([ScheduleID]) REFERENCES [SEIDR].[Schedule] ([ScheduleID])
);

