CREATE TABLE [SEIDR].[Repetition] (
    [RepetitionID]               INT         IDENTITY (1, 1) NOT NULL,
    [RepetitionGroupID]          INT         NOT NULL,
    [RepetitionIntervalTypeCode] VARCHAR (2) NOT NULL,
    [RepetitionInterval]         SMALLINT    NOT NULL,
    [Inverted] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([RepetitionID] ASC),
    CONSTRAINT [CK_Repetition_RepetitionInterval] CHECK ([RepetitionInterval]>(0)),
    FOREIGN KEY ([RepetitionGroupID]) REFERENCES [SEIDR].[RepetitionGroup] ([RepetitionGroupID]),
    FOREIGN KEY ([RepetitionIntervalTypeCode]) REFERENCES [SEIDR].[RepetitionIntervalType] ([RepetitionIntervalTypeCode])
);

