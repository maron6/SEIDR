CREATE TABLE [SEIDR].[Schedule_RepetitionGroup] (
    [ScheduleID]        INT           NOT NULL,
    [RepetitionGroupID] INT           NOT NULL,
    [StartDate]         DATE          DEFAULT (getdate()) NOT NULL,
    [StartHour]         TINYINT       NOT NULL,
    [DD]                SMALLDATETIME NULL,
    [Active]            AS            (CONVERT([bit],case when [DD] IS NULL then (1) else (0) end)) PERSISTED,
    [EndHour]           TINYINT       NULL,
    CHECK (isnull([EndHour],(24))>=(1) AND isnull([EndHour],(24))<=(24)),
    CONSTRAINT [CK__Schedule___Start__7849DB76] CHECK ([StartHour]>=(0) AND [StartHour]<=(23)),
    CONSTRAINT [CK_Sched_RepGroup_EndHour] CHECK ([EndHour] IS NULL OR [StartHour]<[ENdHour]),
    FOREIGN KEY ([RepetitionGroupID]) REFERENCES [SEIDR].[RepetitionGroup] ([RepetitionGroupID]),
    FOREIGN KEY ([ScheduleID]) REFERENCES [SEIDR].[Schedule] ([ScheduleID])
);

