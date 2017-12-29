CREATE TABLE [SEIDR].[RepetitionGroup] (
    [RepetitionGroupID]     INT           IDENTITY (1, 1) NOT NULL,
    [Description]           VARCHAR (200) NULL,
    [AllowLateRegistration] BIT           DEFAULT ((0)) NOT NULL,
    [MinuteInterval]        TINYINT       NULL,
    [HourInterval]          TINYINT       NULL,
    [PartialDay]            AS            (CONVERT([bit],case when [MinuteInterval] IS NOT NULL OR [HourInterval] IS NOT NULL then (1) else (0) end)) PERSISTED,
    PRIMARY KEY CLUSTERED ([RepetitionGroupID] ASC)
);

