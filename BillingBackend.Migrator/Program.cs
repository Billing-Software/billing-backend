using System;
using System.IO;
using System.Linq;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BillingBackend.Migrator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("BillCom Local SQL Server DB Fresh Setup & Purge");
            Console.WriteLine("Target: SQL Server (Integrated Security) @ localhost");
            Console.WriteLine("==================================================");

            var connectionString = "Data Source=.;Initial Catalog=SmartBillingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

            var services = new ServiceCollection();
            services.AddDbContext<BillingDbContext>(options =>
                options.UseSqlServer(connectionString));

            var serviceProvider = services.BuildServiceProvider();

            bool dropOnly = args.Contains("--drop-only") || args.Contains("drop");

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                try
                {
                    // Ensure database exists
                    context.Database.EnsureCreated();

                    Console.WriteLine("\n[1/3] Purging all SQL Server tables, views, and constraints...");
                    context.Database.ExecuteSqlRaw(@"
                        -- Drop all foreign key constraints first
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';' + CHAR(13)
                        FROM sys.foreign_keys f
                        INNER JOIN sys.tables t ON f.parent_object_id = t.object_id
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all tables
                        SET @sql = N'';
                        SELECT @sql += 'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(13)
                        FROM sys.tables t
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all views
                        SET @sql = N'';
                        SELECT @sql += 'DROP VIEW ' + QUOTENAME(s.name) + '.' + QUOTENAME(v.name) + ';' + CHAR(13)
                        FROM sys.views v
                        INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all sequences
                        SET @sql = N'';
                        SELECT @sql += 'DROP SEQUENCE ' + QUOTENAME(s.name) + '.' + QUOTENAME(sq.name) + ';' + CHAR(13)
                        FROM sys.sequences sq
                        INNER JOIN sys.schemas s ON sq.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;");
                    Console.WriteLine("✓ Purged all SQL Server tables, views, sequences, and constraints cleanly.");

                    if (dropOnly)
                    {
                        Console.WriteLine("\n==================================================");
                        Console.WriteLine("SUCCESS: All SQL Server database tables completely purged!");
                        Console.WriteLine("==================================================");
                        return;
                    }

                    Console.WriteLine("\n[2/3] Creating 26 fresh SQL Server Database schema tables...");
                    string[] createTableSqls = new[]
                    {
                        @"CREATE TABLE [Users] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Username] NVARCHAR(100) NOT NULL UNIQUE,
                            [Email] NVARCHAR(256) NOT NULL UNIQUE,
                            [PasswordHash] VARBINARY(MAX) NOT NULL,
                            [PasswordSalt] VARBINARY(MAX) NOT NULL,
                            [Role] NVARCHAR(50) DEFAULT 'Owner' NOT NULL,
                            [PasswordResetToken] NVARCHAR(100),
                            [PasswordResetTokenExpiry] DATETIME2,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [SubscriptionPlans] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Name] NVARCHAR(100) NOT NULL,
                            [RazorpayPlanIdMonthly] NVARCHAR(100) NOT NULL,
                            [RazorpayPlanIdYearly] NVARCHAR(100) NOT NULL,
                            [MonthlyPrice] DECIMAL(18,2) NOT NULL,
                            [YearlyPrice] DECIMAL(18,2) NOT NULL,
                            [MaxBranches] INT NOT NULL,
                            [MaxStaff] INT NOT NULL,
                            [IsActive] BIT DEFAULT 1 NOT NULL
                        )",

                        @"CREATE TABLE [Businesses] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [OwnerId] INT NOT NULL UNIQUE REFERENCES [Users]([Id]) ON DELETE CASCADE,
                            [LegalName] NVARCHAR(200) NOT NULL,
                            [TradingName] NVARCHAR(200),
                            [LogoUrl] NVARCHAR(500),
                            [Address] NVARCHAR(500),
                            [City] NVARCHAR(100),
                            [State] NVARCHAR(100),
                            [PostalCode] NVARCHAR(20),
                            [Country] NVARCHAR(100) DEFAULT 'India' NOT NULL,
                            [Phone] NVARCHAR(20),
                            [Email] NVARCHAR(256),
                            [Website] NVARCHAR(500),
                            [GstIn] NVARCHAR(50),
                            [BusinessType] NVARCHAR(100) DEFAULT 'General Retail Store' NOT NULL,
                            [SellingModel] NVARCHAR(50) DEFAULT 'GOODS_AND_SERVICES' NOT NULL,
                            [GstScheme] NVARCHAR(50) DEFAULT 'Regular' NOT NULL,
                            [RegisteredState] NVARCHAR(100),
                            [CustomTerminologyJson] NVARCHAR(MAX),
                            [DefaultTaxRate] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [PricesIncludeTax] BIT DEFAULT 1 NOT NULL,
                            [ReceiptHeader] NVARCHAR(500),
                            [ReceiptFooter] NVARCHAR(500),
                            [ShowLogoOnReceipt] BIT DEFAULT 1 NOT NULL,
                            [ReceiptTemplateType] NVARCHAR(50) DEFAULT 'Thermal80mm' NOT NULL,
                            [IsSuspended] BIT DEFAULT 0 NOT NULL,
                            [ActivePlanId] INT DEFAULT 1 NOT NULL REFERENCES [SubscriptionPlans]([Id]) ON DELETE SET NULL,
                            [AllowedBranches] INT DEFAULT 1 NOT NULL,
                            [AllowedStaff] INT DEFAULT 2 NOT NULL,
                            [RazorpayCustomerId] NVARCHAR(100),
                            [RazorpaySubscriptionId] NVARCHAR(100),
                            [SubscriptionStatus] NVARCHAR(50) DEFAULT 'Inactive' NOT NULL,
                            [SubscriptionExpiresAt] DATETIME2,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [Branches] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Name] NVARCHAR(100) NOT NULL,
                            [Address] NVARCHAR(500),
                            [City] NVARCHAR(100),
                            [PostalCode] NVARCHAR(20),
                            [Phone] NVARCHAR(20),
                            [IsActive] BIT DEFAULT 1 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [Categories] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [ParentId] INT REFERENCES [Categories]([Id]),
                            [Name] NVARCHAR(100) NOT NULL,
                            [Type] NVARCHAR(50) DEFAULT 'Service' NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [Customers] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Name] NVARCHAR(200) NOT NULL,
                            [Phone] NVARCHAR(20),
                            [Email] NVARCHAR(256),
                            [GstIn] NVARCHAR(50),
                            [IsWalkIn] BIT DEFAULT 0 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [Services] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Name] NVARCHAR(200) NOT NULL,
                            [SKU] NVARCHAR(50) NOT NULL,
                            [Category] NVARCHAR(100) NOT NULL,
                            [BasePrice] DECIMAL(18,2) NOT NULL,
                            [TaxRate] DECIMAL(5,2) DEFAULT 0 NOT NULL,
                            [Status] NVARCHAR(20) DEFAULT 'Active' NOT NULL,
                            [ImageUrl] NVARCHAR(500),
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [InventoryItems] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Name] NVARCHAR(200) NOT NULL,
                            [SKU] NVARCHAR(50) NOT NULL,
                            [Category] NVARCHAR(100) NOT NULL,
                            [CurrentStock] INT DEFAULT 0 NOT NULL,
                            [Unit] NVARCHAR(50) DEFAULT 'pcs' NOT NULL,
                            [ReorderLevel] INT DEFAULT 5 NOT NULL,
                            [ImageUrl] NVARCHAR(500),
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [StaffMembers] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [UserId] INT REFERENCES [Users]([Id]),
                            [BranchId] INT REFERENCES [Branches]([Id]),
                            [Name] NVARCHAR(200) NOT NULL,
                            [EmpCode] NVARCHAR(50) NOT NULL,
                            [Contact] NVARCHAR(256),
                            [Role] NVARCHAR(50) DEFAULT 'Staff' NOT NULL,
                            [TotalBills] INT DEFAULT 0 NOT NULL,
                            [RevenueGenerated] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [Status] NVARCHAR(20) DEFAULT 'Active' NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [Bills] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]),
                            [BranchId] INT NOT NULL REFERENCES [Branches]([Id]),
                            [CustomerId] INT NOT NULL REFERENCES [Customers]([Id]),
                            [CreatedByStaffId] INT REFERENCES [StaffMembers]([Id]) ON DELETE SET NULL,
                            [BillNumber] NVARCHAR(50) NOT NULL,
                            [Subtotal] DECIMAL(18,2) NOT NULL,
                            [DiscountCode] NVARCHAR(50),
                            [DiscountAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [TaxAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [TotalAmount] DECIMAL(18,2) NOT NULL,
                            [PaymentMethod] NVARCHAR(20) DEFAULT 'Cash' NOT NULL,
                            [Status] NVARCHAR(20) DEFAULT 'Pending' NOT NULL,
                            [InvoicePdfUrl] NVARCHAR(500),
                            [IdempotencyKey] NVARCHAR(100),
                            [PaymentReference] NVARCHAR(200),
                            [Notes] NVARCHAR(500),
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [BillItems] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BillId] INT NOT NULL REFERENCES [Bills]([Id]) ON DELETE CASCADE,
                            [ServiceId] INT NOT NULL REFERENCES [Services]([Id]),
                            [ServiceName] NVARCHAR(200) NOT NULL,
                            [UnitPrice] DECIMAL(18,2) NOT NULL,
                            [Quantity] INT DEFAULT 1 NOT NULL,
                            [ItemType] NVARCHAR(50) DEFAULT 'Service' NOT NULL,
                            [LineTotal] DECIMAL(18,2) NOT NULL,
                            [HSNCode] NVARCHAR(20),
                            [SACCode] NVARCHAR(20),
                            [TaxableValue] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [TaxRate] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [CGSTRate] DECIMAL(5,2) DEFAULT 9.00 NOT NULL,
                            [CGSTAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [SGSTRate] DECIMAL(5,2) DEFAULT 9.00 NOT NULL,
                            [SGSTAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [IGSTRate] DECIMAL(5,2) DEFAULT 0 NOT NULL,
                            [IGSTAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [CessRate] DECIMAL(5,2) DEFAULT 0 NOT NULL,
                            [CessAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL
                        )",

                        @"CREATE TABLE [Expenses] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Description] NVARCHAR(500) NOT NULL,
                            [Amount] DECIMAL(18,2) NOT NULL,
                            [Category] NVARCHAR(100) NOT NULL,
                            [ExpenseDate] DATETIME2 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [Purchases] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [VendorName] NVARCHAR(200) NOT NULL,
                            [InvoiceNumber] NVARCHAR(100),
                            [Subtotal] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [TaxAmount] DECIMAL(18,2) DEFAULT 0 NOT NULL,
                            [TotalAmount] DECIMAL(18,2) NOT NULL,
                            [Status] NVARCHAR(50) DEFAULT 'Paid' NOT NULL,
                            [PurchaseDate] DATETIME2 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [PurchaseItems] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [PurchaseId] INT NOT NULL REFERENCES [Purchases]([Id]) ON DELETE CASCADE,
                            [InventoryItemId] INT REFERENCES [InventoryItems]([Id]),
                            [ItemName] NVARCHAR(200) NOT NULL,
                            [UnitPrice] DECIMAL(18,2) NOT NULL,
                            [Quantity] INT DEFAULT 1 NOT NULL,
                            [LineTotal] DECIMAL(18,2) NOT NULL
                        )",

                        @"CREATE TABLE [WhatsAppAccounts] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [MetaBusinessId] NVARCHAR(100),
                            [WabaId] NVARCHAR(100),
                            [PhoneNumberId] NVARCHAR(100),
                            [DisplayPhoneNumber] NVARCHAR(30),
                            [AccessToken] NVARCHAR(1000),
                            [TokenExpiry] DATETIME2,
                            [Status] NVARCHAR(20) DEFAULT 'Pending' NOT NULL,
                            [ConnectedAt] DATETIME2 NOT NULL,
                            [DisconnectedAt] DATETIME2
                        )",

                        @"CREATE TABLE [WhatsAppTemplates] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [WhatsAppAccountId] INT NOT NULL REFERENCES [WhatsAppAccounts]([Id]) ON DELETE CASCADE,
                            [TemplateName] NVARCHAR(100) NOT NULL,
                            [Language] NVARCHAR(20) DEFAULT 'en' NOT NULL,
                            [Category] NVARCHAR(30) DEFAULT 'UTILITY' NOT NULL,
                            [BodyText] NVARCHAR(2000),
                            [Status] NVARCHAR(30) DEFAULT 'PENDING' NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [MessageLogs] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [WhatsAppAccountId] INT NOT NULL REFERENCES [WhatsAppAccounts]([Id]) ON DELETE CASCADE,
                            [BillId] INT REFERENCES [Bills]([Id]),
                            [RecipientPhone] NVARCHAR(20) NOT NULL,
                            [MessageType] NVARCHAR(20) DEFAULT 'text' NOT NULL,
                            [MetaMessageId] NVARCHAR(200),
                            [Status] NVARCHAR(20) DEFAULT 'Queued' NOT NULL,
                            [SentAt] DATETIME2 NOT NULL,
                            [DeliveredAt] DATETIME2,
                            [ReadAt] DATETIME2,
                            [FailedReason] NVARCHAR(500)
                        )",

                        @"CREATE TABLE [UserRefreshTokens] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [UserId] INT NOT NULL REFERENCES [Users]([Id]) ON DELETE CASCADE,
                            [Token] NVARCHAR(500) NOT NULL UNIQUE,
                            [ExpiryTime] DATETIME2 NOT NULL,
                            [IsRevoked] BIT DEFAULT 0 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [PendingRegistrations] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Token] NVARCHAR(100) NOT NULL UNIQUE,
                            [Email] NVARCHAR(256) NOT NULL,
                            [Username] NVARCHAR(100) NOT NULL,
                            [PasswordHash] VARBINARY(MAX) NOT NULL,
                            [PasswordSalt] VARBINARY(MAX) NOT NULL,
                            [LegalName] NVARCHAR(200) NOT NULL,
                            [Phone] NVARCHAR(20),
                            [GstIn] NVARCHAR(50),
                            [Address] NVARCHAR(500),
                            [SelectedPlanId] INT DEFAULT 1 NOT NULL,
                            [RazorpayCustomerId] NVARCHAR(100),
                            [RazorpaySubscriptionId] NVARCHAR(100),
                            [Status] NVARCHAR(50) DEFAULT 'PendingPayment' NOT NULL,
                            [ReminderEmailSent] BIT DEFAULT 0 NOT NULL,
                            [ReminderEmailSentAt] DATETIME2,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [ExpiresAt] DATETIME2 NOT NULL,
                            [RawRegistrationData] NVARCHAR(MAX)
                        )",

                        @"CREATE TABLE [PaymentTransactions] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT REFERENCES [Businesses]([Id]),
                            [RazorpayPaymentId] NVARCHAR(100),
                            [RazorpayOrderId] NVARCHAR(100),
                            [RazorpaySubscriptionId] NVARCHAR(100),
                            [Amount] DECIMAL(18,2) NOT NULL,
                            [Status] NVARCHAR(50) NOT NULL,
                            [PaymentMethod] NVARCHAR(50),
                            [RawWebhookPayload] NVARCHAR(MAX),
                            [FailureReason] NVARCHAR(500),
                            [RetryCount] INT DEFAULT 0 NOT NULL,
                            [WebhookEventId] INT,
                            [CorrelationId] NVARCHAR(100),
                            [CreatedAt] DATETIME2 NOT NULL,
                            [UpdatedAt] DATETIME2
                        )",

                        @"CREATE TABLE [AuditLogs] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT,
                            [EntityType] NVARCHAR(50) NOT NULL,
                            [EntityId] INT,
                            [Action] NVARCHAR(50) NOT NULL,
                            [OldValues] NVARCHAR(MAX),
                            [NewValues] NVARCHAR(MAX),
                            [PerformedBy] NVARCHAR(100),
                            [IpAddress] NVARCHAR(50),
                            [UserAgent] NVARCHAR(500),
                            [CorrelationId] NVARCHAR(100),
                            [Description] NVARCHAR(1000),
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [WebhookEventLogs] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Source] NVARCHAR(50) NOT NULL,
                            [ExternalEventId] NVARCHAR(200),
                            [EventType] NVARCHAR(100) NOT NULL,
                            [RawPayload] NVARCHAR(MAX),
                            [ProcessingStatus] NVARCHAR(30) DEFAULT 'Received' NOT NULL,
                            [FailureReason] NVARCHAR(2000),
                            [RetryCount] INT DEFAULT 0 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [ProcessedAt] DATETIME2
                        )",

                        @"CREATE TABLE [TaxCategories] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [BusinessId] INT REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                            [Name] NVARCHAR(100) NOT NULL,
                            [TaxType] NVARCHAR(20) DEFAULT 'Goods' NOT NULL,
                            [HSNCode] NVARCHAR(20),
                            [SACCode] NVARCHAR(20),
                            [GSTPercentage] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [CGSTPercentage] DECIMAL(5,2) DEFAULT 9.00 NOT NULL,
                            [SGSTPercentage] DECIMAL(5,2) DEFAULT 9.00 NOT NULL,
                            [IGSTPercentage] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [CessPercentage] DECIMAL(5,2) DEFAULT 0.00 NOT NULL,
                            [IsActive] BIT DEFAULT 1 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )",

                        @"CREATE TABLE [HSNMasters] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Code] NVARCHAR(20) NOT NULL,
                            [Description] NVARCHAR(500) NOT NULL,
                            [SearchTerms] NVARCHAR(500),
                            [UQC] NVARCHAR(20) DEFAULT 'PCS' NOT NULL,
                            [DefaultGSTPercentage] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [IsActive] BIT DEFAULT 1 NOT NULL
                        )",

                        @"CREATE TABLE [SACMasters] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Code] NVARCHAR(20) NOT NULL,
                            [Description] NVARCHAR(500) NOT NULL,
                            [SearchTerms] NVARCHAR(500),
                            [DefaultGSTPercentage] DECIMAL(5,2) DEFAULT 18.00 NOT NULL,
                            [IsActive] BIT DEFAULT 1 NOT NULL
                        )",

                        @"CREATE TABLE [BusinessTypeMasters] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Code] NVARCHAR(100) NOT NULL,
                            [Name] NVARCHAR(150) NOT NULL,
                            [Category] NVARCHAR(100) NOT NULL,
                            [IconName] NVARCHAR(50) DEFAULT 'Store' NOT NULL,
                            [SellingModel] NVARCHAR(50) DEFAULT 'GOODS_AND_SERVICES' NOT NULL,
                            [AliasesJson] NVARCHAR(MAX),
                            [DefaultFeaturesJson] NVARCHAR(MAX),
                            [DefaultTerminologyJson] NVARCHAR(MAX),
                            [IsActive] BIT DEFAULT 1 NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL
                        )"
                    };

                    foreach (var sql in createTableSqls)
                    {
                        context.Database.ExecuteSqlRaw(sql);
                    }
                    Console.WriteLine("✓ 26 SQL Server tables created cleanly matching all Entity models.");

                    Console.WriteLine("\n[3/3] Seeding baseline Master Data & Subscription Plans...");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [SubscriptionPlans] ([Name], [RazorpayPlanIdMonthly], [RazorpayPlanIdYearly], [MonthlyPrice], [YearlyPrice], [MaxBranches], [MaxStaff], [IsActive]) 
                        VALUES ('Starter Plan', 'plan_starter_m', 'plan_starter_y', 499, 4990, 1, 2, 1)");

                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [SubscriptionPlans] ([Name], [RazorpayPlanIdMonthly], [RazorpayPlanIdYearly], [MonthlyPrice], [YearlyPrice], [MaxBranches], [MaxStaff], [IsActive]) 
                        VALUES ('Growth Plan', 'plan_growth_m', 'plan_growth_y', 1499, 14990, 5, 10, 1)");

                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [SubscriptionPlans] ([Name], [RazorpayPlanIdMonthly], [RazorpayPlanIdYearly], [MonthlyPrice], [YearlyPrice], [MaxBranches], [MaxStaff], [IsActive]) 
                        VALUES ('Enterprise Plan', 'plan_enterprise_m', 'plan_enterprise_y', 4999, 49990, 99, 999, 1)");

                    Console.WriteLine("✓ Subscription Plans seeded successfully.");

                    // Seed Tax Categories
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Standard Goods (18%)', 'Goods', '9999', NULL, 18.00, 9.00, 9.00, 18.00, 0.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Reduced Goods (5%)', 'Goods', '1001', NULL, 5.00, 2.50, 2.50, 5.00, 0.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Essential / Exempt (0%)', 'Goods', '0000', NULL, 0.00, 0.00, 0.00, 0.00, 0.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Luxury Goods (28% + Cess)', 'Goods', '8703', NULL, 28.00, 14.00, 14.00, 28.00, 12.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Restaurant Service (5%)', 'Services', NULL, '996331', 5.00, 2.50, 2.50, 5.00, 0.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('IT & Professional Services (18%)', 'Services', NULL, '998313', 18.00, 9.00, 9.00, 18.00, 0.00, 1, GETUTCDATE())");
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO [TaxCategories] ([Name], [TaxType], [HSNCode], [SACCode], [GSTPercentage], [CGSTPercentage], [SGSTPercentage], [IGSTPercentage], [CessPercentage], [IsActive], [CreatedAt])
                        VALUES ('Personal Care / Salon (18%)', 'Services', NULL, '999721', 18.00, 9.00, 9.00, 18.00, 0.00, 1, GETUTCDATE())");

                    Console.WriteLine("✓ Tax Categories seeded successfully.");

                    // Seed Business Type Masters
                    context.Database.ExecuteSqlRaw(@"
                        SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                        INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                        VALUES (1, 'restaurant', 'Restaurant', 'Food & Hospitality', 'Utensils', 'GOODS_AND_SERVICES', '[""hotel"",""dine in"",""eatery"",""food court"",""dhaba""]', '{""products"":true,""services"":false,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Menu Item"",""plural"":""Menu Items""},""service"":{""singular"":""Dining Service"",""plural"":""Dining Services""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Order / Bill"",""plural"":""Orders / Bills""},""inventory"":{""singular"":""Ingredient Stock"",""plural"":""Ingredient Stock""},""purchase"":{""singular"":""Ingredient Purchase"",""plural"":""Ingredient Purchases""},""supplier"":{""singular"":""Vendor / Supplier"",""plural"":""Vendors & Suppliers""},""staff"":{""singular"":""Staff / Waiter"",""plural"":""Staff & Waiters""}}', 1, GETUTCDATE());
                        SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                    context.Database.ExecuteSqlRaw(@"
                        SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                        INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                        VALUES (2, 'tiffin_center', 'Tiffin Center / Mess', 'Food & Hospitality', 'Soup', 'GOODS_ONLY', '[""mess"",""tiffin"",""canteen"",""fast food"",""food stall"",""tiffin service""]', '{""products"":true,""services"":false,""inventory"":false,""appointments"":false,""customers"":true,""staff"":false,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Food Item"",""plural"":""Food Items""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill"",""plural"":""Bills""}}', 1, GETUTCDATE());
                        SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                    context.Database.ExecuteSqlRaw(@"
                        SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                        INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                        VALUES (3, 'grocery_kirana', 'Grocery / Kirana Store', 'Retail', 'ShoppingCart', 'GOODS_ONLY', '[""kirana"",""grocery"",""provision store"",""super market"",""general store"",""departmental store""]', '{""products"":true,""services"":false,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Product"",""plural"":""Products""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill"",""plural"":""Bills""},""inventory"":{""singular"":""Stock"",""plural"":""Stock & Inventory""}}', 1, GETUTCDATE());
                        SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                    context.Database.ExecuteSqlRaw(@"
                        SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                        INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                        VALUES (4, 'general_retail', 'General Retail Store', 'Retail', 'Store', 'GOODS_AND_SERVICES', '[""general"",""other"",""retail"",""shop""]', '{""products"":true,""services"":true,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Product"",""plural"":""Products""},""service"":{""singular"":""Service"",""plural"":""Services""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill / Invoice"",""plural"":""Bills & Invoices""}}', 1, GETUTCDATE());
                        SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                    Console.WriteLine("✓ Business Type Masters seeded successfully.");

                    Console.WriteLine("\n[4/4] Verification: Checking existing tables in SQL Server DB...");
                    using (var cmd = context.Database.GetDbConnection().CreateCommand())
                    {
                        if (cmd.Connection.State != System.Data.ConnectionState.Open)
                            cmd.Connection.Open();
                        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
                        using (var reader = cmd.ExecuteReader())
                        {
                            int count = 0;
                            Console.WriteLine("Found the following tables in SQL Server Database:");
                            while (reader.Read())
                            {
                                count++;
                                Console.WriteLine($"  {count}. {reader.GetString(0)}");
                            }
                            Console.WriteLine($"Total Tables Count in DB: {count}");
                        }
                    }

                    Console.WriteLine("\n==================================================");
                    Console.WriteLine("SUCCESS: SQL Server Database schema & migration updated!");
                    Console.WriteLine("==================================================");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[ERROR] Migration failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }
        }
    }
}
