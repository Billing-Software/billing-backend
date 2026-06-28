-- ============================================================
-- SmartBilling Extended Schema: Receipt Settings, Expenses & Purchases
-- ============================================================

-- 1. Alter Businesses Table to add Receipt Settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Businesses') AND name = 'ReceiptHeader')
BEGIN
    ALTER TABLE [dbo].[Businesses] ADD [ReceiptHeader] NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Businesses') AND name = 'ReceiptFooter')
BEGIN
    ALTER TABLE [dbo].[Businesses] ADD [ReceiptFooter] NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Businesses') AND name = 'ShowLogoOnReceipt')
BEGIN
    ALTER TABLE [dbo].[Businesses] ADD [ShowLogoOnReceipt] BIT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Businesses') AND name = 'ReceiptTemplateType')
BEGIN
    ALTER TABLE [dbo].[Businesses] ADD [ReceiptTemplateType] NVARCHAR(50) NOT NULL DEFAULT 'Thermal80mm';
END
GO

-- 2. Create Expenses Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
BEGIN
    CREATE TABLE [dbo].[Expenses] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [BusinessId]    INT             NOT NULL,
        [Description]   NVARCHAR(500)   NOT NULL,
        [Amount]        DECIMAL(18,2)   NOT NULL,
        [Category]      NVARCHAR(100)   NOT NULL,
        [ExpenseDate]   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Expenses] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Expenses_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE
    );
END
GO

-- 3. Create Purchases Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
BEGIN
    CREATE TABLE [dbo].[Purchases] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [BusinessId]    INT             NOT NULL,
        [VendorName]    NVARCHAR(200)   NOT NULL,
        [InvoiceNumber] NVARCHAR(100)   NULL,
        [Subtotal]      DECIMAL(18,2)   NOT NULL,
        [TaxAmount]     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [TotalAmount]   DECIMAL(18,2)   NOT NULL,
        [Status]        NVARCHAR(50)    NOT NULL DEFAULT 'Paid',
        [PurchaseDate]  DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Purchases] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Purchases_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE
    );
END
GO

-- 4. Create PurchaseItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseItems')
BEGIN
    CREATE TABLE [dbo].[PurchaseItems] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [PurchaseId]    INT             NOT NULL,
        [InventoryItemId] INT           NULL,
        [ItemName]      NVARCHAR(200)   NOT NULL,
        [UnitPrice]     DECIMAL(18,2)   NOT NULL,
        [Quantity]      INT             NOT NULL DEFAULT 1,
        [LineTotal]     DECIMAL(18,2)   NOT NULL,
        CONSTRAINT [PK_PurchaseItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PurchaseItems_Purchases] FOREIGN KEY ([PurchaseId]) REFERENCES [dbo].[Purchases]([Id]) ON DELETE CASCADE
    );
END
GO

PRINT 'Receipt settings, Expenses and Purchases tables created successfully.';
GO
