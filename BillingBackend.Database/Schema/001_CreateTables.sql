-- ============================================================
-- SmartBilling Database Schema
-- Multi-Tenant Architecture: User → Business → Everything
-- ============================================================

-- 1. Users (Platform Accounts - Business Owners)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE [dbo].[Users] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [Username]      NVARCHAR(100)   NOT NULL,
    [Email]         NVARCHAR(256)   NOT NULL,
    [PasswordHash]  VARBINARY(MAX)  NOT NULL,
    [PasswordSalt]  VARBINARY(MAX)  NOT NULL,
    [Role]          NVARCHAR(50)    NOT NULL DEFAULT 'Owner',
    [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Users_Username] UNIQUE ([Username]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);
GO

-- 2. Businesses (The Business Entity)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Businesses')
CREATE TABLE [dbo].[Businesses] (
    [Id]                INT             IDENTITY(1,1) NOT NULL,
    [OwnerId]           INT             NOT NULL,
    [LegalName]         NVARCHAR(200)   NOT NULL,
    [TradingName]       NVARCHAR(200)   NULL,
    [LogoUrl]           NVARCHAR(500)   NULL,
    [Address]           NVARCHAR(500)   NULL,
    [City]              NVARCHAR(100)   NULL,
    [State]             NVARCHAR(100)   NULL,
    [PostalCode]        NVARCHAR(20)    NULL,
    [Country]           NVARCHAR(100)   NULL DEFAULT 'India',
    [Phone]             NVARCHAR(20)    NULL,
    [Email]             NVARCHAR(256)   NULL,
    [Website]           NVARCHAR(500)   NULL,
    [GstIn]             NVARCHAR(50)    NULL,
    [DefaultTaxRate]    DECIMAL(5,2)    NOT NULL DEFAULT 18.00,
    [PricesIncludeTax]  BIT             NOT NULL DEFAULT 1,
    [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]         DATETIME2       NULL,
    CONSTRAINT [PK_Businesses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Businesses_Users] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Businesses_OwnerId] UNIQUE ([OwnerId])
);
GO

-- 3. Branches (Multi-Location)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branches')
CREATE TABLE [dbo].[Branches] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]    INT             NOT NULL,
    [Name]          NVARCHAR(100)   NOT NULL,
    [Address]       NVARCHAR(500)   NULL,
    [City]          NVARCHAR(100)   NULL,
    [PostalCode]    NVARCHAR(20)    NULL,
    [Phone]         NVARCHAR(20)    NULL,
    [IsActive]      BIT             NOT NULL DEFAULT 1,
    [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Branches] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Branches_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Branches_BusinessId_Name] UNIQUE ([BusinessId], [Name])
);
GO

-- 4. Customers (Business's Clients)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
CREATE TABLE [dbo].[Customers] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]    INT             NOT NULL,
    [Name]          NVARCHAR(200)   NOT NULL,
    [Phone]         NVARCHAR(20)    NULL,
    [Email]         NVARCHAR(256)   NULL,
    [IsWalkIn]      BIT             NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2       NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Customers_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE
);
GO

-- 5. Services (Service Catalog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Services')
CREATE TABLE [dbo].[Services] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]    INT             NOT NULL,
    [Name]          NVARCHAR(200)   NOT NULL,
    [SKU]           NVARCHAR(50)    NOT NULL,
    [Category]      NVARCHAR(100)   NOT NULL,
    [BasePrice]     DECIMAL(18,2)   NOT NULL,
    [TaxRate]       DECIMAL(5,2)    NOT NULL DEFAULT 0,
    [Status]        NVARCHAR(20)    NOT NULL DEFAULT 'Active',
    [ImageUrl]      NVARCHAR(500)   NULL,
    [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2       NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Services_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Services_BusinessId_SKU] UNIQUE ([BusinessId], [SKU])
);
GO

-- 6. InventoryItems (Stock Management)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryItems')
CREATE TABLE [dbo].[InventoryItems] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]    INT             NOT NULL,
    [Name]          NVARCHAR(200)   NOT NULL,
    [SKU]           NVARCHAR(50)    NOT NULL,
    [Category]      NVARCHAR(100)   NOT NULL,
    [CurrentStock]  INT             NOT NULL DEFAULT 0,
    [Unit]          NVARCHAR(50)    NOT NULL,
    [ReorderLevel]  INT             NOT NULL DEFAULT 0,
    [ImageUrl]      NVARCHAR(500)   NULL,
    [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2       NULL,
    CONSTRAINT [PK_InventoryItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_InventoryItems_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_InventoryItems_BusinessId_SKU] UNIQUE ([BusinessId], [SKU])
);
GO

-- 7. StaffMembers (Business Employees)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffMembers')
CREATE TABLE [dbo].[StaffMembers] (
    [Id]                INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]        INT             NOT NULL,
    [UserId]            INT             NULL,
    [Name]              NVARCHAR(200)   NOT NULL,
    [EmpCode]           NVARCHAR(50)    NOT NULL,
    [Contact]           NVARCHAR(256)   NULL,
    [Role]              NVARCHAR(50)    NOT NULL DEFAULT 'Staff',
    [TotalBills]        INT             NOT NULL DEFAULT 0,
    [RevenueGenerated]  DECIMAL(18,2)   NOT NULL DEFAULT 0,
    [Status]            NVARCHAR(20)    NOT NULL DEFAULT 'Active',
    [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]         DATETIME2       NULL,
    CONSTRAINT [PK_StaffMembers] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_StaffMembers_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StaffMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_StaffMembers_BusinessId_EmpCode] UNIQUE ([BusinessId], [EmpCode])
);
GO

-- 8. Bills (Invoices/Transactions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bills')
CREATE TABLE [dbo].[Bills] (
    [Id]                INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]        INT             NOT NULL,
    [BranchId]          INT             NOT NULL,
    [CustomerId]        INT             NOT NULL,
    [CreatedByStaffId]  INT             NULL,
    [BillNumber]        NVARCHAR(50)    NOT NULL,
    [Subtotal]          DECIMAL(18,2)   NOT NULL,
    [DiscountCode]      NVARCHAR(50)    NULL,
    [DiscountAmount]    DECIMAL(18,2)   NOT NULL DEFAULT 0,
    [TaxAmount]         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    [TotalAmount]       DECIMAL(18,2)   NOT NULL,
    [PaymentMethod]     NVARCHAR(20)    NOT NULL DEFAULT 'Cash',
    [Status]            NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
    [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Bills] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Bills_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bills_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bills_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bills_StaffMembers] FOREIGN KEY ([CreatedByStaffId]) REFERENCES [dbo].[StaffMembers]([Id]) ON DELETE SET NULL,
    CONSTRAINT [UQ_Bills_BusinessId_BillNumber] UNIQUE ([BusinessId], [BillNumber])
);
GO

-- 9. BillItems (Invoice Line Items)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BillItems')
CREATE TABLE [dbo].[BillItems] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BillId]        INT             NOT NULL,
    [ServiceId]     INT             NOT NULL,
    [ServiceName]   NVARCHAR(200)   NOT NULL,
    [UnitPrice]     DECIMAL(18,2)   NOT NULL,
    [Quantity]      INT             NOT NULL DEFAULT 1,
    [LineTotal]     DECIMAL(18,2)   NOT NULL,
    CONSTRAINT [PK_BillItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_BillItems_Bills] FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BillItems_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services]([Id]) ON DELETE NO ACTION
);
GO

-- 10. WhatsAppSettings (Per-Business)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WhatsAppSettings')
CREATE TABLE [dbo].[WhatsAppSettings] (
    [Id]            INT             IDENTITY(1,1) NOT NULL,
    [BusinessId]    INT             NOT NULL,
    [ApiKey]        NVARCHAR(256)   NULL,
    [IsConnected]   BIT             NOT NULL DEFAULT 0,
    [UpdatedAt]     DATETIME2       NULL,
    CONSTRAINT [PK_WhatsAppSettings] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_WhatsAppSettings_Businesses] FOREIGN KEY ([BusinessId]) REFERENCES [dbo].[Businesses]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_WhatsAppSettings_BusinessId] UNIQUE ([BusinessId])
);
GO

-- 11. WhatsAppTemplates (Message Templates)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WhatsAppTemplates')
CREATE TABLE [dbo].[WhatsAppTemplates] (
    [Id]                    INT             IDENTITY(1,1) NOT NULL,
    [WhatsAppSettingsId]    INT             NOT NULL,
    [TemplateName]          NVARCHAR(200)   NOT NULL,
    CONSTRAINT [PK_WhatsAppTemplates] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_WhatsAppTemplates_WhatsAppSettings] FOREIGN KEY ([WhatsAppSettingsId]) REFERENCES [dbo].[WhatsAppSettings]([Id]) ON DELETE CASCADE
);
GO

PRINT 'All 11 tables created successfully.';
GO
