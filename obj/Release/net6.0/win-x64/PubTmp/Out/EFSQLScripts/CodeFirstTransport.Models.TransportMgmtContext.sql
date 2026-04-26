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
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [PaymentTables] (
        [PId] int NOT NULL IDENTITY,
        [PayRecDate] datetime2 NULL,
        [FID] int NULL,
        [DocNumber] nvarchar(max) NULL,
        [Shortage] float NULL,
        CONSTRAINT [PK_PaymentTables] PRIMARY KEY ([PId])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [TblFactories] (
        [FID] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [FactoryName] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetime2 NULL,
        [ModifiedOn] datetime2 NULL,
        [CreatedBy] int NULL,
        [ModifiedBy] int NULL,
        [Gstin] float NULL,
        CONSTRAINT [PK_TblFactories] PRIMARY KEY ([FID])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [TblFreights] (
        [DestId] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(max) NOT NULL,
        [Destination] nvarchar(max) NOT NULL,
        [Wheels] nvarchar(max) NOT NULL,
        [Quantity] nvarchar(max) NOT NULL,
        [FreightRate] float NOT NULL,
        [Vid] int NOT NULL,
        CONSTRAINT [PK_TblFreights] PRIMARY KEY ([DestId])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [TblUsers] (
        [UserId] int NOT NULL IDENTITY,
        [UserName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [Doj] datetime2 NULL,
        [LastLogIn] datetime2 NULL,
        [Role] nvarchar(max) NULL,
        CONSTRAINT [PK_TblUsers] PRIMARY KEY ([UserId])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [BillTables] (
        [BillID] int NOT NULL IDENTITY,
        [BillNum] nvarchar(max) NULL,
        [PId] int NULL,
        [BillDate] datetime2 NULL,
        [BillType] nvarchar(max) NULL,
        [FID] int NULL,
        [PaymentReceived] float NULL,
        [PaymentTablePId] int NULL,
        CONSTRAINT [PK_BillTables] PRIMARY KEY ([BillID]),
        CONSTRAINT [FK_BillTables_PaymentTables_PaymentTablePId] FOREIGN KEY ([PaymentTablePId]) REFERENCES [PaymentTables] ([PId])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE TABLE [TblDispatches] (
        [DispId] int NOT NULL IDENTITY,
        [ChallanNo] nvarchar(max) NULL,
        [DispatchDate] datetime2 NULL,
        [Destination] nvarchar(max) NULL,
        [DispatchQuantity] float NULL,
        [UnitPrice] float NULL,
        [FinalPrice] float NULL,
        [DisVid] int NULL,
        [BillID] int NULL,
        [VehicleNo] nvarchar(max) NULL,
        CONSTRAINT [PK_TblDispatches] PRIMARY KEY ([DispId]),
        CONSTRAINT [FK_TblDispatches_BillTables_BillID] FOREIGN KEY ([BillID]) REFERENCES [BillTables] ([BillID])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE INDEX [IX_BillTables_PaymentTablePId] ON [BillTables] ([PaymentTablePId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    CREATE INDEX [IX_TblDispatches_BillID] ON [TblDispatches] ([BillID]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20240627070801_init')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240627070801_init', N'6.0.21');
END;
GO

COMMIT;
GO

