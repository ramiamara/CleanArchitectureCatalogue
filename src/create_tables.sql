USE [CatalogueDb];

-- Create Catalogues table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Catalogues')
BEGIN
    CREATE TABLE [Catalogues] (
        [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Name]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        [CreatedOn]   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]   NVARCHAR(MAX) NOT NULL DEFAULT '',
        [ModifiedOn]  DATETIME2 NULL,
        [ModifiedBy]  NVARCHAR(MAX) NULL,
        [DeletedOn]   DATETIME2 NULL,
        [DeletedBy]   NVARCHAR(MAX) NULL,
        [IsDeleted]   BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Catalogues] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Catalogues_IsDeleted] ON [Catalogues] ([IsDeleted]);
    PRINT 'Table Catalogues created.';
END
ELSE
    PRINT 'Table Catalogues already exists.';

-- Create Products table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Products')
BEGIN
    CREATE TABLE [Products] (
        [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Name]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Price]       DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Quantity]    INT NOT NULL DEFAULT 0,
        [CatalogueId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedOn]   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]   NVARCHAR(MAX) NOT NULL DEFAULT '',
        [ModifiedOn]  DATETIME2 NULL,
        [ModifiedBy]  NVARCHAR(MAX) NULL,
        [DeletedOn]   DATETIME2 NULL,
        [DeletedBy]   NVARCHAR(MAX) NULL,
        [IsDeleted]   BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Catalogues] FOREIGN KEY ([CatalogueId]) REFERENCES [Catalogues]([Id])
    );
    CREATE INDEX [IX_Products_CatalogueId] ON [Products] ([CatalogueId]);
    CREATE INDEX [IX_Products_IsDeleted] ON [Products] ([IsDeleted]);
    PRINT 'Table Products created.';
END
ELSE
    PRINT 'Table Products already exists.';

-- Insert a fake migration row so EF Core knows tables exist
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20241101000000_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20241101000000_InitialCreate', '9.0.0');
    PRINT 'Migration history row inserted.';
END
