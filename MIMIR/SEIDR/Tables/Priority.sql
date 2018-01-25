CREATE TABLE [SEIDR].[Priority] (
    [PriorityCode]  VARCHAR (10) NOT NULL,
    [PriorityValue] TINYINT      NULL,
    PRIMARY KEY CLUSTERED ([PriorityCode] ASC),
    CHECK ([PriorityValue]>=(0) AND [PriorityValue]<=(15))
);

