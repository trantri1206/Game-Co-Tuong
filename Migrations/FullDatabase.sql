IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [RoomId] int NOT NULL IDENTITY,
        [RoomName] nvarchar(100) NOT NULL,
        [IsActive] bit NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime NULL DEFAULT (getdate()),
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([RoomId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] int NOT NULL IDENTITY,
        [Username] varchar(50) NOT NULL,
        [PasswordHash] varchar(255) NOT NULL,
        [DisplayName] nvarchar(100) NULL,
        [CreatedAt] datetime NULL DEFAULT (getdate()),
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [Matches] (
        [MatchId] int NOT NULL IDENTITY,
        [RoomId] int NULL,
        [MatchType] varchar(50) NOT NULL,
        [InitialBoardState] varchar(max) NOT NULL,
        [Result] varchar(50) NULL,
        [Reason] nvarchar(255) NULL,
        [Version] varchar(20) NULL DEFAULT '1.0',
        [StartTime] datetime NULL DEFAULT (getdate()),
        [EndTime] datetime NULL,
        CONSTRAINT [PK_Matches] PRIMARY KEY ([MatchId]),
        CONSTRAINT [FK_Matches_Rooms] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [RoomMembers] (
        [RoomId] int NOT NULL,
        [UserId] int NOT NULL,
        [JoinedAt] datetime NULL DEFAULT (getdate()),
        CONSTRAINT [PK_RoomMembers] PRIMARY KEY ([RoomId], [UserId]),
        CONSTRAINT [FK_RoomMembers_Rooms] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoomMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [MatchPlayers] (
        [MatchId] int NOT NULL,
        [UserId] int NOT NULL,
        [Side] varchar(10) NOT NULL,
        CONSTRAINT [PK_MatchPlayers] PRIMARY KEY ([MatchId], [UserId]),
        CONSTRAINT [FK_MatchPlayers_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches] ([MatchId]) ON DELETE CASCADE,
        CONSTRAINT [FK_MatchPlayers_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE TABLE [Moves] (
        [MoveId] int NOT NULL IDENTITY,
        [MatchId] int NOT NULL,
        [UserId] int NOT NULL,
        [MoveOrder] int NOT NULL,
        [MoveNotation] varchar(20) NOT NULL,
        [PlayedAt] datetime NULL DEFAULT (getdate()),
        CONSTRAINT [PK_Moves] PRIMARY KEY ([MoveId]),
        CONSTRAINT [FK_Moves_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches] ([MatchId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Moves_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Matches_RoomId] ON [Matches] ([RoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MatchPlayers_UserId] ON [MatchPlayers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Moves_UserId] ON [Moves] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Moves_Order] ON [Moves] ([MatchId], [MoveOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoomMembers_UserId] ON [RoomMembers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024051734_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024051734_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053824_AddEmailToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [Email] varchar(100) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053824_AddEmailToUser'
)
BEGIN
    UPDATE [Users] SET [Email] = [Username] + '@example.com' WHERE [Email] = '' OR [Email] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053824_AddEmailToUser'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053824_AddEmailToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024053824_AddEmailToUser', N'10.0.13');
END;

COMMIT;
GO

