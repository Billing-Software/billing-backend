-- ============================================================
-- SmartBilling Extended Schema: Categories Management Table
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [BusinessId]    INT             NOT NULL,
        [Name]          NVARCHAR(100)   NOT NULL,
        [Type]          NVARCHAR(50)    NOT NULL, -- 'Service', 'Inventory', 'Expense'
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Categories_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses] ([Id]) ON DELETE CASCADE
    );
END
GO

PRINT 'Categories table created successfully.';
GO
