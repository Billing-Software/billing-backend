using System;
using System.Data;
using System.Linq;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
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

                logger.LogInformation("Verifying SQL Server Database Schema for BillCom...");

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

                CreateTableIfNotExists(context, "BusinessSmsSettings", @"
                    CREATE TABLE [BusinessSmsSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [Provider] NVARCHAR(50) DEFAULT 'Exotel' NOT NULL,
                        [SenderId] NVARCHAR(10) NOT NULL,
                        [DltEntityId] NVARCHAR(50) NOT NULL,
                        [InvoiceTemplateId] NVARCHAR(50) NOT NULL,
                        [TemplateBody] NVARCHAR(500),
                        [IsActive] BIT DEFAULT 1 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "BusinessPaymentSettings", @"
                    CREATE TABLE [BusinessPaymentSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL UNIQUE REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [BankName] NVARCHAR(100),
                        [AccountHolderName] NVARCHAR(150),
                        [AccountNumber] NVARCHAR(50),
                        [IfscCode] NVARCHAR(20),
                        [BranchName] NVARCHAR(100),
                        [UpiVpa] NVARCHAR(100),
                        [ShowUpiQrOnInvoice] BIT DEFAULT 1 NOT NULL,
                        [ShowBankDetailsOnInvoice] BIT DEFAULT 0 NOT NULL,
                        [PaymentGatewayProvider] NVARCHAR(50) DEFAULT 'None' NOT NULL,
                        [PaymentGatewayApiKey] NVARCHAR(200),
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "BusinessTaxSettings", @"
                    CREATE TABLE [BusinessTaxSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL UNIQUE REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [IsGstRegistered] BIT DEFAULT 0 NOT NULL,
                        [GstIn] NVARCHAR(15),
                        [GstScheme] NVARCHAR(50) DEFAULT 'None' NOT NULL,
                        [PanNumber] NVARCHAR(20),
                        [RegisteredState] NVARCHAR(100),
                        [RegisteredStateCode] NVARCHAR(10),
                        [DefaultTaxRate] DECIMAL(5,2) DEFAULT 0.00 NOT NULL,
                        [PricesIncludeTax] BIT DEFAULT 1 NOT NULL,
                        [TaxFilingFrequency] NVARCHAR(50) DEFAULT 'Monthly' NOT NULL,
                        [EnableReverseCharge] BIT DEFAULT 0 NOT NULL,
                        [EnableEInvoicing] BIT DEFAULT 0 NOT NULL,
                        [EWayBillThreshold] DECIMAL(18,2) DEFAULT 50000.00 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "BusinessInvoiceSettings", @"
                    CREATE TABLE [BusinessInvoiceSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL UNIQUE REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [InvoicePrefix] NVARCHAR(20) DEFAULT 'INV-' NOT NULL,
                        [StartingInvoiceNumber] INT DEFAULT 1001 NOT NULL,
                        [CurrentSequenceNumber] INT DEFAULT 1000 NOT NULL,
                        [InvoiceNumberFormat] NVARCHAR(50) DEFAULT 'INV-XXXX' NOT NULL,
                        [DefaultCurrency] NVARCHAR(20) DEFAULT 'INR (₹)' NOT NULL,
                        [DefaultPaymentTerms] NVARCHAR(50) DEFAULT 'Due on Receipt' NOT NULL,
                        [InvoiceDueDays] INT DEFAULT 0 NOT NULL,
                        [DefaultNotes] NVARCHAR(500),
                        [TermsAndConditions] NVARCHAR(MAX),
                        [AutoRoundOff] BIT DEFAULT 1 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "BusinessInvoiceDesigns", @"
                    CREATE TABLE [BusinessInvoiceDesigns] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL UNIQUE REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [ThemeId] NVARCHAR(50) DEFAULT 'modern' NOT NULL,
                        [PaperSize] NVARCHAR(50) DEFAULT 'Thermal80mm' NOT NULL,
                        [BrandColorHex] NVARCHAR(20) DEFAULT '#006A61' NOT NULL,
                        [StoreDisplayName] NVARCHAR(200),
                        [Tagline] NVARCHAR(200),
                        [ShowLogo] BIT DEFAULT 1 NOT NULL,
                        [LogoPosition] NVARCHAR(20) DEFAULT 'left' NOT NULL,
                        [ShowGstin] BIT DEFAULT 1 NOT NULL,
                        [ShowContact] BIT DEFAULT 1 NOT NULL,
                        [ShowSerialNo] BIT DEFAULT 1 NOT NULL,
                        [ShowItemName] BIT DEFAULT 1 NOT NULL,
                        [ShowHsnSac] BIT DEFAULT 1 NOT NULL,
                        [ShowMrp] BIT DEFAULT 1 NOT NULL,
                        [ShowDiscount] BIT DEFAULT 1 NOT NULL,
                        [ShowTaxRate] BIT DEFAULT 1 NOT NULL,
                        [ShowBatchExpiry] BIT DEFAULT 0 NOT NULL,
                        [ShowAmountInWords] BIT DEFAULT 1 NOT NULL,
                        [ShowPreviousBalance] BIT DEFAULT 1 NOT NULL,
                        [ShowTaxBreakdown] BIT DEFAULT 1 NOT NULL,
                        [ShowSignature] BIT DEFAULT 1 NOT NULL,
                        [SignatoryTitle] NVARCHAR(100) DEFAULT 'Authorized Signatory' NOT NULL,
                        [FooterMessage] NVARCHAR(500),
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "BusinessPrinterSettings", @"
                    CREATE TABLE [BusinessPrinterSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [BranchId] INT REFERENCES [Branches]([Id]) ON DELETE SET NULL,
                        [PrinterName] NVARCHAR(100) NOT NULL,
                        [PrinterType] NVARCHAR(50) DEFAULT 'BluetoothThermal' NOT NULL,
                        [MacAddressOrIp] NVARCHAR(100),
                        [PaperWidthMm] INT DEFAULT 80 NOT NULL,
                        [AutoPrintOnBillComplete] BIT DEFAULT 1 NOT NULL,
                        [NumberOfCopies] INT DEFAULT 1 NOT NULL,
                        [FeedLinesAfterPrint] INT DEFAULT 2 NOT NULL,
                        [CutPaperEnabled] BIT DEFAULT 1 NOT NULL,
                        [OpenCashDrawerEnabled] BIT DEFAULT 0 NOT NULL,
                        [IsActive] BIT DEFAULT 1 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "CustomerLedgers", @"
                    CREATE TABLE [CustomerLedgers] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]),
                        [CustomerId] INT NOT NULL REFERENCES [Customers]([Id]) ON DELETE CASCADE,
                        [BillId] INT REFERENCES [Bills]([Id]) ON DELETE SET NULL,
                        [TransactionType] NVARCHAR(50) NOT NULL,
                        [Amount] DECIMAL(18,2) NOT NULL,
                        [RunningBalance] DECIMAL(18,2) NOT NULL,
                        [PaymentMode] NVARCHAR(50) DEFAULT 'Cash' NOT NULL,
                        [ReferenceNumber] NVARCHAR(100),
                        [Notes] NVARCHAR(500),
                        [RecordedByStaffId] INT REFERENCES [StaffMembers]([Id]),
                        [TransactionDate] DATETIME2 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL
                    )");

                CreateTableIfNotExists(context, "DiscountCoupons", @"
                    CREATE TABLE [DiscountCoupons] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [CouponCode] NVARCHAR(50) NOT NULL,
                        [DiscountType] NVARCHAR(20) DEFAULT 'Percentage' NOT NULL,
                        [DiscountValue] DECIMAL(18,2) NOT NULL,
                        [MinimumOrderAmount] DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
                        [MaximumDiscountAmount] DECIMAL(18,2),
                        [ValidFrom] DATETIME2 NOT NULL,
                        [ValidUntil] DATETIME2 NOT NULL,
                        [UsageLimit] INT,
                        [UsedCount] INT DEFAULT 0 NOT NULL,
                        [IsActive] BIT DEFAULT 1 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL
                    )");

                CreateTableIfNotExists(context, "Warehouses", @"
                    CREATE TABLE [Warehouses] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [BranchId] INT REFERENCES [Branches]([Id]) ON DELETE SET NULL,
                        [Name] NVARCHAR(150) NOT NULL,
                        [Code] NVARCHAR(50) NOT NULL,
                        [Address] NVARCHAR(300),
                        [ContactPerson] NVARCHAR(100),
                        [Phone] NVARCHAR(20),
                        [IsPrimary] BIT DEFAULT 0 NOT NULL,
                        [IsActive] BIT DEFAULT 1 NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "StockTransfers", @"
                    CREATE TABLE [StockTransfers] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]),
                        [TransferNumber] NVARCHAR(50) NOT NULL,
                        [SourceWarehouseId] INT NOT NULL REFERENCES [Warehouses]([Id]),
                        [DestinationWarehouseId] INT NOT NULL REFERENCES [Warehouses]([Id]),
                        [Status] NVARCHAR(50) DEFAULT 'Completed' NOT NULL,
                        [TransferDate] DATETIME2 NOT NULL,
                        [DispatchedByStaffId] INT REFERENCES [StaffMembers]([Id]),
                        [ReceivedByStaffId] INT REFERENCES [StaffMembers]([Id]),
                        [Notes] NVARCHAR(500),
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "StockTransferItems", @"
                    CREATE TABLE [StockTransferItems] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [StockTransferId] INT NOT NULL REFERENCES [StockTransfers]([Id]) ON DELETE CASCADE,
                        [InventoryItemId] INT NOT NULL REFERENCES [InventoryItems]([Id]),
                        [Quantity] DECIMAL(18,2) NOT NULL,
                        [Unit] NVARCHAR(20) DEFAULT 'PCS' NOT NULL,
                        [BatchNumber] NVARCHAR(50)
                    )");

                CreateTableIfNotExists(context, "BusinessAppPreferences", @"
                    CREATE TABLE [BusinessAppPreferences] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL UNIQUE REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [DefaultLanguage] NVARCHAR(10) DEFAULT 'en' NOT NULL,
                        [DateFormat] NVARCHAR(50) DEFAULT 'dd/MM/yyyy' NOT NULL,
                        [TimeFormat] NVARCHAR(20) DEFAULT '12h' NOT NULL,
                        [CurrencySymbol] NVARCHAR(10) DEFAULT N'₹' NOT NULL,
                        [CurrencyPlacement] NVARCHAR(20) DEFAULT 'BeforeAmount' NOT NULL,
                        [EnableSoundEffects] BIT DEFAULT 1 NOT NULL,
                        [EnableHapticFeedback] BIT DEFAULT 1 NOT NULL,
                        [BarcodeScannerMode] NVARCHAR(50) DEFAULT 'AutoDetect' NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2
                    )");

                CreateTableIfNotExists(context, "SmsLogs", @"
                    CREATE TABLE [SmsLogs] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [BusinessId] INT NOT NULL REFERENCES [Businesses]([Id]) ON DELETE CASCADE,
                        [BillId] INT REFERENCES [Bills]([Id]) ON DELETE SET NULL,
                        [RecipientPhone] NVARCHAR(20) NOT NULL,
                        [SenderId] NVARCHAR(10) NOT NULL,
                        [MessageBody] NVARCHAR(1000) NOT NULL,
                        [DltEntityId] NVARCHAR(50),
                        [DltTemplateId] NVARCHAR(50),
                        [ExotelSid] NVARCHAR(100),
                        [Status] NVARCHAR(30) DEFAULT 'Sent' NOT NULL,
                        [ErrorMessage] NVARCHAR(500),
                        [SentAt] DATETIME2 NOT NULL
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
                AddColumnIfNotExists(context, "PaymentTransactions", "SubscriptionPlanId", "INT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "BillingCycle", "NVARCHAR(20) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpaySubscriptionId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "PaymentMethod", "NVARCHAR(50) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RawWebhookPayload", "NVARCHAR(MAX) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "FailureReason", "NVARCHAR(500) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RetryCount", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "WebhookEventId", "INT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "CorrelationId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "UpdatedAt", "DATETIME2 NULL");
                ExecuteRawSqlDirect(context, @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PaymentTransactions_RazorpayOrderId' AND object_id = OBJECT_ID('PaymentTransactions')) CREATE UNIQUE INDEX [UX_PaymentTransactions_RazorpayOrderId] ON [PaymentTransactions]([RazorpayOrderId]) WHERE RazorpayOrderId IS NOT NULL;");
                ExecuteRawSqlDirect(context, @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PaymentTransactions_RazorpayPaymentId' AND object_id = OBJECT_ID('PaymentTransactions')) CREATE UNIQUE INDEX [UX_PaymentTransactions_RazorpayPaymentId] ON [PaymentTransactions]([RazorpayPaymentId]) WHERE RazorpayPaymentId IS NOT NULL;");

                // 5. Ensure Columns on PendingRegistrations table
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpayCustomerId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpaySubscriptionId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpayOrderId", "NVARCHAR(100) NULL");
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

                // 14. Ensure Columns on WhatsAppAccounts table
                AddColumnIfNotExists(context, "WhatsAppAccounts", "Provider", "NVARCHAR(50) DEFAULT 'Twilio' NOT NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "TwilioSubaccountSid", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "TwilioSubaccountAuthToken", "NVARCHAR(1000) NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "SenderId", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "DisplayName", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "CreatedAt", "DATETIME2 DEFAULT GETUTCDATE() NOT NULL");
                AddColumnIfNotExists(context, "WhatsAppAccounts", "UpdatedAt", "DATETIME2 NULL");

                // 15. Ensure Columns on MessageLogs table
                AddColumnIfNotExists(context, "MessageLogs", "TwilioMessageSid", "NVARCHAR(200) NULL");
                AddColumnIfNotExists(context, "MessageLogs", "Provider", "NVARCHAR(50) DEFAULT 'Twilio' NOT NULL");
                AddColumnIfNotExists(context, "MessageLogs", "TemplateName", "NVARCHAR(100) NULL");
                AddColumnIfNotExists(context, "MessageLogs", "MediaUrl", "NVARCHAR(1000) NULL");
                AddColumnIfNotExists(context, "MessageLogs", "ErrorCode", "NVARCHAR(50) NULL");
                AddColumnIfNotExists(context, "MessageLogs", "ErrorMessage", "NVARCHAR(500) NULL");
                AddColumnIfNotExists(context, "MessageLogs", "CreatedAt", "DATETIME2 DEFAULT GETUTCDATE() NOT NULL");

                // Ensure Trial Tracking Columns on Businesses table
                AddColumnIfNotExists(context, "Businesses", "IsTrial", "BIT DEFAULT 1 NOT NULL");
                AddColumnIfNotExists(context, "Businesses", "TrialStartsAt", "DATETIME2 NULL");
                AddColumnIfNotExists(context, "Businesses", "TrialEndsAt", "DATETIME2 NULL");

                // Industrial auth hardening columns on Users table (lockout + OTP attempt guards)
                AddColumnIfNotExists(context, "Users", "FailedLoginAttempts", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Users", "LockoutEnd", "DATETIME2 NULL");
                AddColumnIfNotExists(context, "Users", "PasswordResetAttemptCount", "INT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Users", "PasswordResetRequestedAt", "DATETIME2 NULL");

                // 16. RowVersion columns for EF optimistic concurrency (model marks them [Timestamp]).
                // Fresh databases get them from EnsureCreated; existing databases are upgraded here.
                // rowversion is auto-populated by SQL Server, so ADD is safe on populated tables.
                AddColumnIfNotExists(context, "Businesses", "RowVersion", "rowversion");
                AddColumnIfNotExists(context, "Bills", "RowVersion", "rowversion");
                AddColumnIfNotExists(context, "InventoryItems", "RowVersion", "rowversion");
                AddColumnIfNotExists(context, "CustomerLedgers", "RowVersion", "rowversion");
                AddColumnIfNotExists(context, "StockTransfers", "RowVersion", "rowversion");
                AddColumnIfNotExists(context, "PaymentTransactions", "RowVersion", "rowversion");

                // 17. Operational tenant/reporting indexes (online-safe, mirrors 004_IndustrialHardening).
                // Foreign keys and CHECK constraints stay pipeline-only (004): they lock tables and
                // require the fail-closed data validation that must run before enforcement.
                CreateCompositeIndexIfNotExists(context, "Bills", "IX_Bills_BusinessId_CreatedAt", "[BusinessId], [CreatedAt] DESC");
                CreateCompositeIndexIfNotExists(context, "PaymentTransactions", "IX_PaymentTransactions_BusinessId_CreatedAt", "[BusinessId], [CreatedAt] DESC");
                CreateCompositeIndexIfNotExists(context, "CustomerLedgers", "IX_CustomerLedgers_BusinessId_CustomerId_TransactionDate", "[BusinessId], [CustomerId], [TransactionDate] DESC");
                CreateCompositeIndexIfNotExists(context, "InventoryItems", "IX_InventoryItems_BusinessId_CurrentStock", "[BusinessId], [CurrentStock]");
                CreateCompositeIndexIfNotExists(context, "AuditLogs", "IX_AuditLogs_BusinessId_CreatedAt", "[BusinessId], [CreatedAt] DESC");
                CreateCompositeIndexIfNotExists(context, "WebhookEventLogs", "IX_WebhookEventLogs_Completed_ProcessedAt", "[ProcessingStatus], [ProcessedAt]");

                // Initialize trial tracking for existing Businesses if not set
                ExecuteRawSqlDirect(context, """
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Businesses' AND COLUMN_NAME = 'TrialStartsAt')
                    BEGIN
                        UPDATE [Businesses]
                        SET [TrialStartsAt] = ISNULL([TrialStartsAt], [CreatedAt]),
                            [TrialEndsAt] = ISNULL([TrialEndsAt], DATEADD(day, 7, [CreatedAt])),
                            [IsTrial] = CASE 
                                WHEN [SubscriptionStatus] = 'Active' AND [RazorpaySubscriptionId] IS NOT NULL AND [RazorpaySubscriptionId] <> '' THEN 0
                                WHEN DATEADD(day, 7, [CreatedAt]) > GETUTCDATE() THEN 1 
                                ELSE 0 
                            END
                        WHERE [TrialStartsAt] IS NULL;
                    END
                """);

                // Ensure Columns on SubscriptionPlans table
                AddColumnIfNotExists(context, "SubscriptionPlans", "Subtitle", "NVARCHAR(200) NULL");
                AddColumnIfNotExists(context, "SubscriptionPlans", "IsPopular", "BIT DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "SubscriptionPlans", "DisplayOrder", "INT DEFAULT 1 NOT NULL");
                AddColumnIfNotExists(context, "SubscriptionPlans", "FeaturesJson", "NVARCHAR(MAX) NULL");

                // Unconditionally UPDATE SubscriptionPlans rows 1, 2, and 3 in SQL Server
                ExecuteRawSqlDirect(context, """
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionPlans')
                    BEGIN
                        UPDATE [SubscriptionPlans]
                        SET [Name] = 'Starter Shop',
                            [Subtitle] = 'Ideal for Single Kirana, Small Cafes & Standalone Stores',
                            [MonthlyPrice] = 499.00,
                            [YearlyPrice] = 4999.00,
                            [MaxBranches] = 1,
                            [MaxStaff] = 2,
                            [IsPopular] = 0,
                            [DisplayOrder] = 1,
                            [FeaturesJson] = '[{"text":"Single Store & Counter POS","included":true},{"text":"2 Cashier Staff Accounts","included":true},{"text":"Thermal & A4 Tax Invoice Printing","included":true},{"text":"Customer Udhar Khata Ledger","included":true},{"text":"Stock Warning Alerts","included":true},{"text":"Multi-Branch Franchise Sync","included":false},{"text":"Stylist Commission Calculator","included":false}]'
                        WHERE [Id] = 1 OR [Name] LIKE '%Starter%';

                        UPDATE [SubscriptionPlans]
                        SET [Name] = 'Growth Business',
                            [Subtitle] = 'Perfect for High-Volume Retailers, Salons & Restaurants',
                            [MonthlyPrice] = 999.00,
                            [YearlyPrice] = 9999.00,
                            [MaxBranches] = 3,
                            [MaxStaff] = 10,
                            [IsPopular] = 1,
                            [DisplayOrder] = 2,
                            [FeaturesJson] = '[{"text":"Up to 3 Store Outlets","included":true},{"text":"10 Staff Accounts & Role Controls","included":true},{"text":"Automated DLT SMS Receipts","included":true},{"text":"Barcode & Electronic Scale Integration","included":true},{"text":"Kitchen KOT & Table Layouts","included":true},{"text":"GST E-Invoicing & Tally Prime Sync","included":true},{"text":"Operating Expense & Profit Tracker","included":true}]'
                        WHERE [Id] = 2 OR [Name] LIKE '%Growth%' OR [Name] LIKE '%Professional%';

                        UPDATE [SubscriptionPlans]
                        SET [Name] = 'Enterprise Chain',
                            [Subtitle] = 'Custom Architecture for Large Multi-City Franchises',
                            [MonthlyPrice] = 2499.00,
                            [YearlyPrice] = 24999.00,
                            [MaxBranches] = 25,
                            [MaxStaff] = 50,
                            [IsPopular] = 0,
                            [DisplayOrder] = 3,
                            [FeaturesJson] = '[{"text":"Unlimited Outlets & Central Warehouse","included":true},{"text":"50 Staff Accounts with Role Controls","included":true},{"text":"Dedicated Account Manager & 24/7 SLA","included":true},{"text":"Custom ERP & Tally 2-Way Sync","included":true},{"text":"Multi-Branch Royalty & P&L Analytics","included":true},{"text":"High-Throughput Exotel DLT SMS","included":true}]'
                        WHERE [Id] = 3 OR [Name] LIKE '%Enterprise%';
                    END
                """);

                // ===== AppFeatures Table =====
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppFeatures')
                    BEGIN
                        CREATE TABLE [AppFeatures] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [FeatureKey] NVARCHAR(100) NOT NULL,
                            [DisplayName] NVARCHAR(200) NOT NULL,
                            [Description] NVARCHAR(500) NULL,
                            [Category] NVARCHAR(50) NOT NULL DEFAULT 'Core',
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            CONSTRAINT [UQ_AppFeatures_FeatureKey] UNIQUE ([FeatureKey])
                        );
                    END
                ");

                // ===== PlanFeatures Table =====
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlanFeatures')
                    BEGIN
                        CREATE TABLE [PlanFeatures] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [PlanId] INT NOT NULL,
                            [FeatureId] INT NOT NULL,
                            [IsEnabled] BIT NOT NULL DEFAULT 1,
                            CONSTRAINT [FK_PlanFeatures_Plans] FOREIGN KEY ([PlanId]) REFERENCES [SubscriptionPlans]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_PlanFeatures_Features] FOREIGN KEY ([FeatureId]) REFERENCES [AppFeatures]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [UQ_PlanFeatures_PlanId_FeatureId] UNIQUE ([PlanId], [FeatureId])
                        );
                    END
                ");

                // ===== RoleFeatures Table =====
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RoleFeatures')
                    BEGIN
                        CREATE TABLE [RoleFeatures] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [RoleName] NVARCHAR(50) NOT NULL,
                            [FeatureId] INT NOT NULL,
                            [IsEnabled] BIT NOT NULL DEFAULT 1,
                            CONSTRAINT [FK_RoleFeatures_Features] FOREIGN KEY ([FeatureId]) REFERENCES [AppFeatures]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [UQ_RoleFeatures_RoleName_FeatureId] UNIQUE ([RoleName], [FeatureId])
                        );
                    END
                ");

                // ===== Seed AppFeatures =====
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppFeatures')
                    AND NOT EXISTS (SELECT 1 FROM [AppFeatures])
                    BEGIN
                        SET IDENTITY_INSERT [AppFeatures] ON;
                        INSERT INTO [AppFeatures] ([Id], [FeatureKey], [DisplayName], [Description], [Category], [IsActive]) VALUES
                        (1, 'dashboard', 'Dashboard', 'Main dashboard overview', 'Core', 1),
                        (2, 'billing', 'Billing & Invoicing', 'Create new bills and invoices', 'Core', 1),
                        (3, 'invoices', 'Invoice History', 'View and manage past invoices', 'Core', 1),
                        (4, 'customers', 'Customer Management', 'CRM customer directory', 'Core', 1),
                        (5, 'services', 'Service Catalog', 'Manage service listings', 'Core', 1),
                        (6, 'inventory', 'Inventory & Stock', 'Product stock management', 'Standard', 1),
                        (7, 'reports', 'GST Reports', 'Tax and sales reports', 'Standard', 1),
                        (8, 'staff_manage', 'Staff Management', 'Manage staff members and roles', 'Standard', 1),
                        (9, 'branches', 'Multi-Branch Sync', 'Multi-location branch management', 'Advanced', 1),
                        (10, 'expenses', 'Expense Tracking', 'Track business expenses', 'Standard', 1),
                        (11, 'settings', 'Business Settings', 'Configure business preferences', 'Core', 1),
                        (12, 'whatsapp', 'WhatsApp Integration', 'WhatsApp messaging webhooks', 'Advanced', 1),
                        (13, 'purchases', 'Purchase Management', 'Track supplier purchases', 'Standard', 1),
                        (14, 'custom_templates', 'Custom Invoice Templates', 'Custom PDF invoice designs', 'Premium', 1),
                        (15, 'api_webhooks', 'API & Webhook Access', 'External API integrations', 'Premium', 1),
                        (16, 'dedicated_db', 'Dedicated Database', 'Dedicated database cluster', 'Premium', 1);
                        SET IDENTITY_INSERT [AppFeatures] OFF;
                    END
                ");

                // ===== Seed PlanFeatures =====
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlanFeatures')
                    AND NOT EXISTS (SELECT 1 FROM [PlanFeatures])
                    AND EXISTS (SELECT 1 FROM [AppFeatures])
                    BEGIN
                        -- Starter Plan (1): Core + Standard, no Advanced/Premium
                        INSERT INTO [PlanFeatures] ([PlanId], [FeatureId], [IsEnabled])
                        SELECT 1, [Id], CASE WHEN [Category] IN ('Core', 'Standard') THEN 1 ELSE 0 END FROM [AppFeatures];

                        -- Growth Plan (2): Core + Standard + Advanced, no Premium
                        INSERT INTO [PlanFeatures] ([PlanId], [FeatureId], [IsEnabled])
                        SELECT 2, [Id], CASE WHEN [Category] IN ('Core', 'Standard', 'Advanced') THEN 1 ELSE 0 END FROM [AppFeatures];

                        -- Enterprise Plan (3): Everything enabled
                        INSERT INTO [PlanFeatures] ([PlanId], [FeatureId], [IsEnabled])
                        SELECT 3, [Id], 1 FROM [AppFeatures];
                    END
                ");

                // ===== Seed RoleFeatures =====
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RoleFeatures')
                    AND NOT EXISTS (SELECT 1 FROM [RoleFeatures])
                    AND EXISTS (SELECT 1 FROM [AppFeatures])
                    BEGIN
                        -- Owner: all features enabled
                        INSERT INTO [RoleFeatures] ([RoleName], [FeatureId], [IsEnabled])
                        SELECT 'Owner', [Id], 1 FROM [AppFeatures];

                        -- Staff: only dashboard, billing, invoices, customers, services
                        INSERT INTO [RoleFeatures] ([RoleName], [FeatureId], [IsEnabled])
                        SELECT 'Staff', [Id], CASE WHEN [FeatureKey] IN ('dashboard', 'billing', 'invoices', 'customers', 'services') THEN 1 ELSE 0 END FROM [AppFeatures];

                        -- SuperAdmin: only dashboard and reports
                        INSERT INTO [RoleFeatures] ([RoleName], [FeatureId], [IsEnabled])
                        SELECT 'SuperAdmin', [Id], CASE WHEN [FeatureKey] IN ('dashboard', 'reports') THEN 1 ELSE 0 END FROM [AppFeatures];
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

        private static void CreateIndexIfNotExists(BillingDbContext context, string tableName, string indexName, string columnName, bool unique, string? filter = null)
        {
            var uniqueKeyword = unique ? "UNIQUE " : string.Empty;
            var filterClause = string.IsNullOrWhiteSpace(filter) ? string.Empty : $" WHERE {filter}";
            var sql = $@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}'))
                         CREATE {uniqueKeyword}INDEX [{indexName}] ON [{tableName}]([{columnName}]){filterClause};";
            try { context.Database.ExecuteSqlRaw(sql); } catch { /* schema verification is best effort */ }
        }

        private static void CreateCompositeIndexIfNotExists(BillingDbContext context, string tableName, string indexName, string columnsSql)
        {
            var sql = $@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}'))
                         CREATE INDEX [{indexName}] ON [{tableName}]({columnsSql});";
            try { context.Database.ExecuteSqlRaw(sql); } catch { /* schema verification is best effort */ }
        }

        private static void ExecuteRawSqlDirect(BillingDbContext context, string sql)
        {
            try
            {
                var conn = context.Database.GetDbConnection();
                bool isClosed = conn.State != ConnectionState.Open;
                if (isClosed) conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    if (isClosed) conn.Close();
                }
            }
            catch { /* ignore */ }
        }
    }
}
