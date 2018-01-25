CREATE TABLE [SEIDR].[ScheduleRuleCluster_ScheduleRule] (
    [ScheduleRuleClusterID] INT NOT NULL,
    [ScheduleRuleID]        INT NOT NULL,
    PRIMARY KEY CLUSTERED ([ScheduleRuleClusterID] ASC, [ScheduleRuleID] ASC),
    FOREIGN KEY ([ScheduleRuleClusterID]) REFERENCES [SEIDR].[ScheduleRuleCluster] ([ScheduleRuleClusterID]),
    FOREIGN KEY ([ScheduleRuleID]) REFERENCES [SEIDR].[ScheduleRule] ([ScheduleRuleID])
);

