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
                AddColumnIfNotExists(context, "Businesses", "SellingModel", "VARCHAR2(50) DEFAULT 'GOODS_AND_SERVICES'");
                AddColumnIfNotExists(context, "Businesses", "BusinessType", "VARCHAR2(100) DEFAULT 'General Retail Store'");
                AddColumnIfNotExists(context, "Businesses", "GstScheme", "VARCHAR2(50) DEFAULT 'Regular'");
                AddColumnIfNotExists(context, "Businesses", "RegisteredState", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "Businesses", "CustomTerminologyJson", "NCLOB NULL");
                AddColumnIfNotExists(context, "Businesses", "UpdatedAt", "TIMESTAMP NULL");

                // 2. Ensure Columns on BillItems table
                AddColumnIfNotExists(context, "BillItems", "HSNCode", "VARCHAR2(20) NULL");
                AddColumnIfNotExists(context, "BillItems", "SACCode", "VARCHAR2(20) NULL");
                AddColumnIfNotExists(context, "BillItems", "TaxableValue", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "TaxRate", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CGSTRate", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CGSTAmount", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "SGSTRate", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "SGSTAmount", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "IGSTRate", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "IGSTAmount", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CessRate", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "CessAmount", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "ItemType", "VARCHAR2(50) DEFAULT 'Service'");

                // 3. Ensure Columns on Customers table
                AddColumnIfNotExists(context, "Customers", "GstIn", "VARCHAR2(50) NULL");
                AddColumnIfNotExists(context, "Customers", "UpdatedAt", "TIMESTAMP NULL");

                // 4. Ensure Columns on PaymentTransactions table
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpayPaymentId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpayOrderId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RazorpaySubscriptionId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "PaymentMethod", "VARCHAR2(50) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RawWebhookPayload", "NCLOB NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "FailureReason", "VARCHAR2(500) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "RetryCount", "NUMBER(10) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "WebhookEventId", "NUMBER(10) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "CorrelationId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PaymentTransactions", "UpdatedAt", "TIMESTAMP NULL");

                // 5. Ensure Columns on PendingRegistrations table
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpayCustomerId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "RazorpaySubscriptionId", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "ReminderEmailSent", "NUMBER(1) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "ReminderEmailSentAt", "TIMESTAMP NULL");
                AddColumnIfNotExists(context, "PendingRegistrations", "SelectedPlanId", "NUMBER(10) DEFAULT 1 NOT NULL");

                // 6. Ensure Columns on UserRefreshTokens table
                AddColumnIfNotExists(context, "UserRefreshTokens", "ExpiryTime", "TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL");
                AddColumnIfNotExists(context, "UserRefreshTokens", "IsRevoked", "NUMBER(1) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "UserRefreshTokens", "CreatedAt", "TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL");

                // 7. Ensure Columns on Categories table
                AddColumnIfNotExists(context, "Categories", "Type", "VARCHAR2(50) DEFAULT 'Service' NOT NULL");

                // 8. Ensure Columns on InventoryItems table
                RenameColumnIfExists(context, "InventoryItems", "StockQuantity", "CurrentStock");
                AddColumnIfNotExists(context, "InventoryItems", "CurrentStock", "NUMBER(10) DEFAULT 0 NOT NULL");

                // 9. Ensure Columns on Bills table
                RenameColumnIfExists(context, "Bills", "StaffMemberId", "CreatedByStaffId");
                RenameColumnIfExists(context, "Bills", "SubTotal", "Subtotal");
                RenameColumnIfExists(context, "Bills", "PaymentStatus", "Status");
                AddColumnIfNotExists(context, "Bills", "CreatedByStaffId", "NUMBER(10) NULL");
                AddColumnIfNotExists(context, "Bills", "Subtotal", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Bills", "DiscountCode", "VARCHAR2(50) NULL");
                AddColumnIfNotExists(context, "Bills", "Status", "VARCHAR2(20) DEFAULT 'Completed' NOT NULL");
                AddColumnIfNotExists(context, "Bills", "IdempotencyKey", "VARCHAR2(100) NULL");
                AddColumnIfNotExists(context, "Bills", "PaymentReference", "VARCHAR2(200) NULL");
                AddColumnIfNotExists(context, "Bills", "Notes", "VARCHAR2(500) NULL");
                AddColumnIfNotExists(context, "Bills", "UpdatedAt", "TIMESTAMP NULL");

                // 10. Ensure Columns on BillItems table
                RenameColumnIfExists(context, "BillItems", "ItemId", "ServiceId");
                RenameColumnIfExists(context, "BillItems", "Name", "ServiceName");
                RenameColumnIfExists(context, "BillItems", "TotalPrice", "LineTotal");
                AddColumnIfNotExists(context, "BillItems", "ServiceId", "NUMBER(10) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "ServiceName", "VARCHAR2(200) DEFAULT '' NOT NULL");
                AddColumnIfNotExists(context, "BillItems", "LineTotal", "NUMBER(18,2) DEFAULT 0 NOT NULL");

                // 11. Ensure Columns on StaffMembers table
                AddColumnIfNotExists(context, "StaffMembers", "UserId", "NUMBER(10) NULL");
                AddColumnIfNotExists(context, "StaffMembers", "BranchId", "NUMBER(10) NULL");
                AddColumnIfNotExists(context, "StaffMembers", "TotalBills", "NUMBER(10) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "StaffMembers", "RevenueGenerated", "NUMBER(18,2) DEFAULT 0 NOT NULL");

                // 12. Ensure Columns on Purchases table
                AddColumnIfNotExists(context, "Purchases", "Subtotal", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "Purchases", "TaxAmount", "NUMBER(18,2) DEFAULT 0 NOT NULL");

                // 13. Ensure Columns on PurchaseItems table
                RenameColumnIfExists(context, "PurchaseItems", "UnitCost", "UnitPrice");
                RenameColumnIfExists(context, "PurchaseItems", "TotalCost", "LineTotal");
                AddColumnIfNotExists(context, "PurchaseItems", "UnitPrice", "NUMBER(18,2) DEFAULT 0 NOT NULL");
                AddColumnIfNotExists(context, "PurchaseItems", "LineTotal", "NUMBER(18,2) DEFAULT 0 NOT NULL");

                logger.LogInformation("Oracle Database Schema Verification Completed Successfully.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database schema verification notice: {Message}", ex.Message);
            }
        }

        private static void AddColumnIfNotExists(BillingDbContext context, string tableName, string columnName, string columnDef)
        {
            var sql = $@"
                DECLARE
                    v_count NUMBER := 0;
                BEGIN
                    SELECT COUNT(*) INTO v_count FROM user_tab_cols WHERE UPPER(table_name) = '{tableName.ToUpperInvariant()}' AND UPPER(column_name) = '{columnName.ToUpperInvariant()}';
                    IF v_count = 0 THEN
                        EXECUTE IMMEDIATE 'ALTER TABLE ""{tableName}"" ADD ""{columnName}"" {columnDef}';
                    END IF;
                END;";
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch { /* ignore */ }
        }

        private static void RenameColumnIfExists(BillingDbContext context, string tableName, string oldName, string newName)
        {
            var sql = $@"
                DECLARE
                    v_old_count NUMBER := 0;
                    v_new_count NUMBER := 0;
                BEGIN
                    SELECT COUNT(*) INTO v_old_count FROM user_tab_cols WHERE UPPER(table_name) = '{tableName.ToUpperInvariant()}' AND UPPER(column_name) = '{oldName.ToUpperInvariant()}';
                    SELECT COUNT(*) INTO v_new_count FROM user_tab_cols WHERE UPPER(table_name) = '{tableName.ToUpperInvariant()}' AND UPPER(column_name) = '{newName.ToUpperInvariant()}';
                    IF v_old_count > 0 AND v_new_count = 0 THEN
                        EXECUTE IMMEDIATE 'ALTER TABLE ""{tableName}"" RENAME COLUMN ""{oldName}"" TO ""{newName}""';
                    END IF;
                END;";
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch { /* ignore */ }
        }
    }
}
