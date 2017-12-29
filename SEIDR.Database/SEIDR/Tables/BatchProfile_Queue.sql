CREATE TABLE [SEIDR].[BatchProfile_Queue] (
    [BatchProfileID] INT           NOT NULL,
    [LastQueued]     SMALLDATETIME NULL,
    [QueueThreadID]  TINYINT       NOT NULL,
    [Priority]       TINYINT       NULL,
    PRIMARY KEY CLUSTERED ([BatchProfileID] ASC),
    FOREIGN KEY ([BatchProfileID]) REFERENCES [SEIDR].[BatchProfile] ([BatchProfileID])
);

