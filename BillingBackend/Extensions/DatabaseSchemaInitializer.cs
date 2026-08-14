using System;
using System.Data;
using BillingBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Extensions
{
    public static class DatabaseSchemaInitializer
    {
        public static void EnsureDatabaseSchemaUpdated(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BillingDbContext>>();

            try
            {
                // Ensure all core tables are created from the EF Core model
                // This creates Users, Businesses, Branches, Bills, etc. if they don't exist
                context.Database.EnsureCreated();

                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                logger.LogInformation("Verifying SQL Server Database Schema for SmartBilling...");

                // 0. Ensure Core Tables exist if database is fresh or partially initialized
                CreateTableIfNotExists(context, "SubscriptionPlans", @"
                    CREATE TABLE [SubscriptionPlans] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Name] NVARCHAR(100) NOT NULL,
                        [RazorpayPlanIdMonthly] NVARCHAR(100) NOT NULL,
                        [RazorpayPlanIdYearly] NVARCHAR(100) NOT NULL,
                        [MonthlyPrice] DECIMAL(18,2) NOT NULL,
                        [YearlyPrice] DECIMAL(18,2) NOT NULL,
                        [MaxBranches] INT NOT NULL,
                        [MaxStaff] INT NOT NULL,
                        [IsActive] BIT DEFAULT 1 NOT NULL
                    )");

                CreateTableIfNotExists(context, "PendingRegistrations", @"
                    CREATE TABLE [PendingRegistrations] (
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
                    )");

                CreateTableIfNotExists(context, "PaymentTransactions", @"
                    CREATE TABLE [PaymentTransactions] (
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
                    )");

                CreateTableIfNotExists(context, "UserRefreshTokens", @"
                    CREATE TABLE [UserRefreshTokens] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UserId] INT NOT NULL REFERENCES [Users]([Id]) ON DELETE CASCADE,
                        [Token] NVARCHAR(500) NOT NULL UNIQUE,
                        [ExpiryTime] DATETIME2 NOT NULL,
                        [IsRevoked] BIT DEFAULT 0 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL
                    )");

                CreateTableIfNotExists(context, "WhatsAppAccounts", @"
                    CREATE TABLE [WhatsAppAccounts] (
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
                    )");

                CreateTableIfNotExists(context, "WhatsAppTemplates", @"
                    CREATE TABLE [WhatsAppTemplates] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [WhatsAppAccountId] INT NOT NULL REFERENCES [WhatsAppAccounts]([Id]) ON DELETE CASCADE,
                        [TemplateName] NVARCHAR(100) NOT NULL,
                        [Language] NVARCHAR(20) DEFAULT 'en' NOT NULL,
                        [Category] NVARCHAR(30) DEFAULT 'UTILITY' NOT NULL,
                        [BodyText] NVARCHAR(2000),
                        [Status] NVARCHAR(30) DEFAULT 'PENDING' NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "MessageLogs", @"
                    CREATE TABLE [MessageLogs] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [WhatsAppAccountId] INT NOT NULL REFERENCES [WhatsAppAccounts]([Id]) ON DELETE CASCADE,
                        [BillId] INT REFERENCES [Bills]([Id]) ON DELETE SET NULL,
                        [RecipientPhone] NVARCHAR(20) NOT NULL,
                        [MessageType] NVARCHAR(20) DEFAULT 'text' NOT NULL,
                        [MetaMessageId] NVARCHAR(200),
                        [Status] NVARCHAR(20) DEFAULT 'Queued' NOT NULL,
                        [SentAt] DATETIME2 NOT NULL,
                        [DeliveredAt] DATETIME2,
                        [ReadAt] DATETIME2,
                        [FailedReason] NVARCHAR(500)
                    )");

                // 1. Ensure Columns on Businesses table
                AddColumnIfNotExists(context, "Businesses", "SellingModel", "NVARCHAR(50) DEFAULT 'GOODS_AND_SERVICES'");
                AddColumnIfNotExists(context, "Businesses", "BusinessType", "NVARCHAR(100) DEFAULT 'General Retail Store'");
                AddColumnIfNotExists(context, "Businesses", "GstScheme", "NVARCHAR(50) DEFAULT 'Regular'");
                AddColumnIfNotExists(context, "Businesses", "RegisteredState", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "Businesses", "CustomTerminologyJson", "NVARCHAR(MAX) NULL");
                AddColumnIfNotExists(context, "Businesses", "UpdatedAt", "DATETIME2 NULL");

                // 2. Ensure Columns on BillItems table
                AddColumnIfNotExists(context, "BillItems", "HSNCode", "NVARCHAR(20) NULL");
                AddColumnIfNotExists(context, "BillItems", "SACCode", "NVARCHAR(20) NULL");
                AddColumnIfNotExists(context, "BillItems", "TaxableValue", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "TaxRate", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CGSTRate", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CGSTAmount", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "SGSTRate", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "SGSTAmount", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "IGSTRate", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "IGSTAmount", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CessRate", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CessAmount", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "ItemType", "NVARCHAR(50) DEFAULT 'Service'");

                // 3. Ensure Columns on Customers table
                AddColumnIfNotExists(context, "Customers", "GstIn", "NVARCHAR(50) NULL");
                AddColumnIfNotExists(context, "Customers", "UpdatedAt", "DATETIME2 NULL");

                // 4. Ensure Columns on PaymentTransactions table
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpayPaymentId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpayOrderId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpaySubscriptionId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "PaymentMethod", "NVARCHAR(50) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RawWebhookPayload", "NVARCHAR(MAX) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "FailureReason", "NVARCHAR(500) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RetryCount", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "WebhookEventId", "INT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "CorrelationId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "UpdatedAt", "DATETIME2 NULL");

                // 5. Ensure Columns on PendingRegistrations table
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpayCustomerId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpaySubscriptionId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "ReminderEmailSent", "BIT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "ReminderEmailSentAt", "DATETIME2 NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "SelectedPlanId", "INT DEFAULT 1 NOT NULL");

                // 6. Ensure Columns on UserRefreshTokens table
                AddColumnIfNotExists(context, "UserRefreshTokens", "ExpiryTime", "DATETIME2 DEFAULT GETUTCDATE() NOT NULL");
                AddColumnIfNotExists(context, "UserRefreshTokens", "IsRevoked", "BIT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "UserRefreshTokens", "CreatedAt", "DATETIME2 DEFAULT GETUTCDATE() NOT NULL");

                // 7. Ensure Columns on Categories table
                AddColumnIfNotExists(context, "Categories", "Type", "NVARCHAR(50) DEFAULT 'Service' NOT NULL");

                // 8. Ensure Columns on InventoryItems table
                RenameColumnIfExists(context, "InventoryItems", "StockQuantity", "CurrentStock");
                AddColumnIfNotExists(context, "InventoryItems", "CurrentStock", "INT DEFAULT 0 NOT NULL");

                // 9. Ensure Columns on Bills table
                RenameColumnIfExists(context, "Bills", "StaffMemberId", "CreatedByStaffId");
                RenameColumnIfExists(context, "Bills", "SubTotal", "Subtotal");
                RenameColumnIfExists(context, "Bills", "PaymentStatus", "Status");
                AddColumnIfNotExists(context, "Bills", "CreatedByStaffId", "INT NULL");
                AddColumnIfNotExists(context, "Bills", "Subtotal", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Bills", "DiscountCode", "NVARCHAR(50) NULL");
                AddColumnIfNotExists(context, "Bills", "Status", "NVARCHAR(20) DEFAULT 'Completed' NOT NULL");
                AddColumnIfNotExists(context, "Bills", "IdempotencyKey", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "Bills", "PaymentReference", "NVARCHAR(200) NULL");
                AddColumnIfNotExists(context, "Bills", "Notes", "NVARCHAR(500) NULL");
                AddColumnIfNotExists(context, "Bills", "UpdatedAt", "DATETIME2 NULL");

                // 10. Ensure Columns on BillItems table
                RenameColumnIfExists(context, "BillItems", "ItemId", "ServiceId");
                RenameColumnIfExists(context, "BillItems", "Name", "ServiceName");
                RenameColumnIfExists(context, "BillItems", "TotalPrice", "LineTotal");
                AddColumnIfNotExists(context, "BillItems", "ServiceId", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "ServiceName", "NVARCHAR(200) DEFAULT '' NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "LineTotal", "DECIMAL(18,2) DEFAULT 0 NOT NULL");

                // 11. Ensure Columns on StaffMembers table
                AddColumnIfNotExists(context, "StaffMembers", "UserId", "INT NULL");
                AddColumnIfNotExists(context, "StaffMembers", "BranchId", "INT NULL");
                AddColumnIfNotExists(context, "StaffMembers", "TotalBills", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "StaffMembers", "RevenueGenerated", "DECIMAL(18,2) DEFAULT 0 NOT NULL");

                // 12. Ensure Columns on Purchases table
                AddColumnIfNotExists(context, "Purchases", "Subtotal", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Purchases", "TaxAmount", "DECIMAL(18,2) DEFAULT 0 NOT NULL");

                // 13. Ensure Columns on PurchaseItems table
                RenameColumnIfExists(context, "PurchaseItems", "UnitCost", "UnitPrice");
                RenameColumnIfExists(context, "PurchaseItems", "TotalCost", "LineTotal");
                AddColumnIfNotExists(context, "PurchaseItems", "UnitPrice", "DECIMAL(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PurchaseItems", "LineTotal", "DECIMAL(18,2) DEFAULT 0 NOT NULL");

                // Seed SubscriptionPlans if table exists but empty
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionPlans')
                    AND NOT EXISTS (SELECT 1 FROM [SubscriptionPlans])
                    BEGIN
                        SET IDENTITY_INSERT [SubscriptionPlans] ON;
                        INSERT INTO [SubscriptionPlans] ([Id], [Name], [RazorpayPlanIdMonthly], [RazorpayPlanIdYearly], [MonthlyPrice], [YearlyPrice], [MaxBranches], [MaxStaff], [IsActive])
                        VALUES 
                        (1, 'Starter Plan', 'plan_starter_monthly', 'plan_starter_yearly', 499.00, 4999.00, 1, 2, 1),
                        (2, 'Growth Plan', 'plan_growth_monthly', 'plan_growth_yearly', 1499.00, 14990.00, 5, 10, 1),
                        (3, 'Enterprise Plan', 'plan_enterprise_monthly', 'plan_enterprise_yearly', 4999.00, 49990.00, 99, 999, 1);
                        SET IDENTITY_INSERT [SubscriptionPlans] OFF;
                    END
                ");

                logger.LogInformation("SQL Server Database Schema Verification Completed Successfully.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database schema verification notice: {Message}", ex.Message);
            }
        }

        private static void CreateTableIfNotExists(BillingDbContext context, string tableName, string createTableDdl)
        {
            var sql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
                BEGIN
                    {createTableDdl}
                END";
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch { /* ignore */ }
        }

        private static void AddColumnIfNotExists(BillingDbContext context, string tableName, string columnName, string columnDef)
        {
            var sql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}')
                BEGIN
                    ALTER TABLE [{tableName}] ADD [{columnName}] {columnDef}
                END";
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch { /* ignore */ }
        }

        private static void RenameColumnIfExists(BillingDbContext context, string tableName, string oldName, string newName)
        {
            var sql = $@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{oldName}')
                AND NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{newName}')
                BEGIN
                    EXEC sp_rename '{tableName}.{oldName}', '{newName}', 'COLUMN'
                END";
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch { /* ignore */ }
        }
    }
}
