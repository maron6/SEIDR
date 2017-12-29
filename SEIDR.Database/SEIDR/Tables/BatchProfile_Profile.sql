CREATE TABLE [SEIDR].[BatchProfile_Profile] (
    [BatchProfile_ProfileID] INT IDENTITY (1, 1) NOT NULL,
    [BatchProfileID]         INT NOT NULL,
    [ParentProfileID]        INT NOT NULL,
    [ParentingDayOffset]     INT DEFAULT ((0)) NOT NULL,
    [AllowError]             BIT DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BatchProfile_ProfileID] ASC),
    CHECK ([BatchProfileID]<>[ParentProfileID]),
    FOREIGN KEY ([BatchProfileID]) REFERENCES [SEIDR].[BatchProfile] ([BatchProfileID]),
    FOREIGN KEY ([ParentProfileID]) REFERENCES [SEIDR].[BatchProfile] ([BatchProfileID])
);

