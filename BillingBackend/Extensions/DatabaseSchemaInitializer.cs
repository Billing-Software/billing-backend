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
                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                logger.LogInformation("Verifying Oracle Database Schema for SmartBilling...");

                // 1. Ensure Columns on Businesses table
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"SellingModel\" VARCHAR2(50) DEFAULT 'GOODS_AND_SERVICES'", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"BusinessType\" VARCHAR2(100) DEFAULT 'General Retail Store'", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"GstScheme\" VARCHAR2(50) DEFAULT 'Regular'", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"RegisteredState\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"CustomTerminologyJson\" NCLOB NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Businesses\" ADD \"UpdatedAt\" TIMESTAMP NULL", logger);

                // 2. Ensure Columns on BillItems table
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"HSNCode\" VARCHAR2(20) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"SACCode\" VARCHAR2(20) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"TaxableValue\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"TaxRate\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"CGSTRate\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"CGSTAmount\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"SGSTRate\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"SGSTAmount\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"IGSTRate\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"IGSTAmount\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"CessRate\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"CessAmount\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);

                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"ItemType\" VARCHAR2(50) DEFAULT 'Service'", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" MODIFY \"ItemType\" DEFAULT 'Service'", logger);

                // 3. Ensure Columns on Customers table
                ExecuteSafeSql(context, "ALTER TABLE \"Customers\" ADD \"GstIn\" VARCHAR2(50) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Customers\" ADD \"UpdatedAt\" TIMESTAMP NULL", logger);

                // 4. Ensure Columns on PaymentTransactions table
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"RazorpayPaymentId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"RazorpayOrderId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"RazorpaySubscriptionId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"PaymentMethod\" VARCHAR2(50) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"RawWebhookPayload\" NCLOB NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"FailureReason\" VARCHAR2(500) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"RetryCount\" NUMBER(10) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"WebhookEventId\" NUMBER(10) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"CorrelationId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PaymentTransactions\" ADD \"UpdatedAt\" TIMESTAMP NULL", logger);

                // 5. Ensure Columns on PendingRegistrations table
                ExecuteSafeSql(context, "ALTER TABLE \"PendingRegistrations\" ADD \"RazorpayCustomerId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PendingRegistrations\" ADD \"RazorpaySubscriptionId\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PendingRegistrations\" ADD \"ReminderEmailSent\" NUMBER(1) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PendingRegistrations\" ADD \"ReminderEmailSentAt\" TIMESTAMP NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PendingRegistrations\" ADD \"SelectedPlanId\" NUMBER(10) DEFAULT 1 NOT NULL", logger);

                // 6. Ensure Columns on UserRefreshTokens table
                ExecuteSafeSql(context, "ALTER TABLE \"UserRefreshTokens\" MODIFY \"ExpiresAt\" NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"UserRefreshTokens\" ADD \"ExpiryTime\" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"UserRefreshTokens\" ADD \"IsRevoked\" NUMBER(1) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"UserRefreshTokens\" ADD \"CreatedAt\" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL", logger);

                // 7. Ensure Columns on Categories table
                ExecuteSafeSql(context, "ALTER TABLE \"Categories\" ADD \"Type\" VARCHAR2(50) DEFAULT 'Service' NOT NULL", logger);

                // 8. Ensure Columns on InventoryItems table
                ExecuteSafeSql(context, "ALTER TABLE \"InventoryItems\" RENAME COLUMN \"StockQuantity\" TO \"CurrentStock\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"InventoryItems\" ADD \"CurrentStock\" NUMBER(10) DEFAULT 0 NOT NULL", logger);

                // 9. Ensure Columns on Bills table
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" RENAME COLUMN \"StaffMemberId\" TO \"CreatedByStaffId\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" RENAME COLUMN \"SubTotal\" TO \"Subtotal\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" RENAME COLUMN \"PaymentStatus\" TO \"Status\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"CreatedByStaffId\" NUMBER(10) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"Subtotal\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"DiscountCode\" VARCHAR2(50) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"Status\" VARCHAR2(20) DEFAULT 'Completed' NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"IdempotencyKey\" VARCHAR2(100) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"PaymentReference\" VARCHAR2(200) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"Notes\" VARCHAR2(500) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Bills\" ADD \"UpdatedAt\" TIMESTAMP NULL", logger);

                // 10. Ensure Columns on BillItems table
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" RENAME COLUMN \"ItemId\" TO \"ServiceId\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" RENAME COLUMN \"Name\" TO \"ServiceName\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" RENAME COLUMN \"TotalPrice\" TO \"LineTotal\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"ServiceId\" NUMBER(10) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"ServiceName\" VARCHAR2(200) DEFAULT '' NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"BillItems\" ADD \"LineTotal\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);

                // 11. Ensure Columns on StaffMembers table
                ExecuteSafeSql(context, "ALTER TABLE \"StaffMembers\" ADD \"UserId\" NUMBER(10) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"StaffMembers\" ADD \"BranchId\" NUMBER(10) NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"StaffMembers\" ADD \"TotalBills\" NUMBER(10) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"StaffMembers\" ADD \"RevenueGenerated\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);

                // 12. Ensure Columns on Purchases table
                ExecuteSafeSql(context, "ALTER TABLE \"Purchases\" ADD \"Subtotal\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"Purchases\" ADD \"TaxAmount\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);

                // 13. Ensure Columns on PurchaseItems table
                ExecuteSafeSql(context, "ALTER TABLE \"PurchaseItems\" RENAME COLUMN \"UnitCost\" TO \"UnitPrice\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PurchaseItems\" RENAME COLUMN \"TotalCost\" TO \"LineTotal\"", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PurchaseItems\" ADD \"UnitPrice\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);
                ExecuteSafeSql(context, "ALTER TABLE \"PurchaseItems\" ADD \"LineTotal\" NUMBER(18,2) DEFAULT 0 NOT NULL", logger);

                logger.LogInformation("Oracle Database Schema Verification Completed.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database schema verification notice: {Message}", ex.Message);
            }
        }

        private static void ExecuteSafeSql(BillingDbContext context, string sql, ILogger logger)
        {
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ORA-01430") || ex.Message.Contains("ORA-00955") || ex.Message.Contains("already exists") || ex.Message.Contains("ORA-01451") || ex.Message.Contains("ORA-00904"))
                {
                    // Column modification/addition handled or non-existent, ignore safely
                }
                else
                {
                    logger.LogDebug("Database schema verification notice: {Message}", ex.Message);
                }
            }
        }
    }
}
