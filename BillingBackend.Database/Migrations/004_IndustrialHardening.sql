/*
  SmartBill Pro - industrial schema hardening (SQL Server)
  Apply through the release pipeline with a principal allowed to ALTER schema.
  The script is deliberately fail-closed: invalid historical rows stop deployment
  instead of silently weakening tenant or financial integrity.
  v2: 'Inactive' added to subscription statuses (DTO/Migrator default, filter branch);
      staff-tenant and purchase-orphan pre-checks; CK_Bills_Status (incl. legacy
      'Completed'); staff tenant FK preserves app SET NULL behavior;
      PurchaseItems -> InventoryItems FK (SET NULL).
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
    CREATE TABLE dbo.__SchemaMigrations (
        MigrationId nvarchar(150) NOT NULL PRIMARY KEY,
        AppliedAt datetime2 NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAt DEFAULT SYSUTCDATETIME(),
        AppliedBy nvarchar(128) NULL,
        Checksum nvarchar(128) NULL
    );

IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaMigrations WHERE MigrationId = N'004_IndustrialHardening')
BEGIN
    /* Validate existing data before installing constraints. */
    IF EXISTS (SELECT 1 FROM dbo.Businesses b LEFT JOIN dbo.SubscriptionPlans p ON p.Id = b.ActivePlanId WHERE p.Id IS NULL)
        THROW 51000, 'Migration blocked: Businesses contains an invalid ActivePlanId.', 1;
    IF EXISTS (SELECT 1 FROM dbo.PaymentTransactions pt LEFT JOIN dbo.Businesses b ON b.Id = pt.BusinessId WHERE pt.BusinessId IS NOT NULL AND b.Id IS NULL)
        THROW 51001, 'Migration blocked: PaymentTransactions contains an invalid BusinessId.', 1;
    IF EXISTS (SELECT 1 FROM dbo.PaymentTransactions pt LEFT JOIN dbo.SubscriptionPlans p ON p.Id = pt.SubscriptionPlanId WHERE pt.SubscriptionPlanId IS NOT NULL AND p.Id IS NULL)
        THROW 51002, 'Migration blocked: PaymentTransactions contains an invalid SubscriptionPlanId.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Bills b JOIN dbo.Branches br ON br.Id = b.BranchId WHERE b.BusinessId <> br.BusinessId)
        THROW 51003, 'Migration blocked: a Bill branch belongs to a different business.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Bills b JOIN dbo.Customers c ON c.Id = b.CustomerId WHERE b.BusinessId <> c.BusinessId)
        THROW 51004, 'Migration blocked: a Bill customer belongs to a different business.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Bills b JOIN dbo.StaffMembers s ON s.Id = b.CreatedByStaffId WHERE b.BusinessId <> s.BusinessId)
        THROW 51008, 'Migration blocked: a Bill creator belongs to a different business.', 1;
    IF EXISTS (SELECT 1 FROM dbo.PurchaseItems pi LEFT JOIN dbo.InventoryItems i ON i.Id = pi.InventoryItemId WHERE pi.InventoryItemId IS NOT NULL AND i.Id IS NULL)
        THROW 51009, 'Migration blocked: a PurchaseItem references a missing InventoryItem.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Bills WHERE Status NOT IN ('Pending','Paid','Failed','Cancelled','Refunded','PartialRefund','Completed'))
        THROW 51010, 'Migration blocked: Bills contains an unrecognized Status.', 1;
    IF EXISTS (SELECT 1 FROM dbo.StockTransfers WHERE SourceWarehouseId = DestinationWarehouseId)
        THROW 51005, 'Migration blocked: a stock transfer uses the same source and destination warehouse.', 1;
    IF EXISTS (SELECT 1 FROM dbo.StockTransfers st JOIN dbo.Warehouses sw ON sw.Id = st.SourceWarehouseId JOIN dbo.Warehouses dw ON dw.Id = st.DestinationWarehouseId WHERE st.BusinessId <> sw.BusinessId OR st.BusinessId <> dw.BusinessId)
        THROW 51006, 'Migration blocked: a stock transfer warehouse belongs to a different business.', 1;

    /* Rowversion gives EF optimistic concurrency protection for mutable financial/stock rows. */
    IF COL_LENGTH('dbo.Businesses', 'RowVersion') IS NULL ALTER TABLE dbo.Businesses ADD RowVersion rowversion NOT NULL;
    IF COL_LENGTH('dbo.Bills', 'RowVersion') IS NULL ALTER TABLE dbo.Bills ADD RowVersion rowversion NOT NULL;
    IF COL_LENGTH('dbo.InventoryItems', 'RowVersion') IS NULL ALTER TABLE dbo.InventoryItems ADD RowVersion rowversion NOT NULL;
    IF COL_LENGTH('dbo.CustomerLedgers', 'RowVersion') IS NULL ALTER TABLE dbo.CustomerLedgers ADD RowVersion rowversion NOT NULL;
    IF COL_LENGTH('dbo.StockTransfers', 'RowVersion') IS NULL ALTER TABLE dbo.StockTransfers ADD RowVersion rowversion NOT NULL;
    IF COL_LENGTH('dbo.PaymentTransactions', 'RowVersion') IS NULL ALTER TABLE dbo.PaymentTransactions ADD RowVersion rowversion NOT NULL;

    /* Physical plan/payment integrity. */
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Businesses_SubscriptionPlans_ActivePlanId')
        ALTER TABLE dbo.Businesses WITH CHECK ADD CONSTRAINT FK_Businesses_SubscriptionPlans_ActivePlanId FOREIGN KEY (ActivePlanId) REFERENCES dbo.SubscriptionPlans(Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Businesses_BusinessId')
        ALTER TABLE dbo.PaymentTransactions WITH CHECK ADD CONSTRAINT FK_PaymentTransactions_Businesses_BusinessId FOREIGN KEY (BusinessId) REFERENCES dbo.Businesses(Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_SubscriptionPlans_SubscriptionPlanId')
        ALTER TABLE dbo.PaymentTransactions WITH CHECK ADD CONSTRAINT FK_PaymentTransactions_SubscriptionPlans_SubscriptionPlanId FOREIGN KEY (SubscriptionPlanId) REFERENCES dbo.SubscriptionPlans(Id);

    /* Composite foreign keys stop cross-tenant references even if application validation is bypassed. */
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Branches_BusinessId_Id') ALTER TABLE dbo.Branches ADD CONSTRAINT UQ_Branches_BusinessId_Id UNIQUE (BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Customers_BusinessId_Id') ALTER TABLE dbo.Customers ADD CONSTRAINT UQ_Customers_BusinessId_Id UNIQUE (BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_StaffMembers_BusinessId_Id') ALTER TABLE dbo.StaffMembers ADD CONSTRAINT UQ_StaffMembers_BusinessId_Id UNIQUE (BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Warehouses_BusinessId_Id') ALTER TABLE dbo.Warehouses ADD CONSTRAINT UQ_Warehouses_BusinessId_Id UNIQUE (BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Bills_Branches_Tenant') ALTER TABLE dbo.Bills WITH CHECK ADD CONSTRAINT FK_Bills_Branches_Tenant FOREIGN KEY (BusinessId, BranchId) REFERENCES dbo.Branches(BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Bills_Customers_Tenant') ALTER TABLE dbo.Bills WITH CHECK ADD CONSTRAINT FK_Bills_Customers_Tenant FOREIGN KEY (BusinessId, CustomerId) REFERENCES dbo.Customers(BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Bills_StaffMembers_Tenant') ALTER TABLE dbo.Bills WITH CHECK ADD CONSTRAINT FK_Bills_StaffMembers_Tenant FOREIGN KEY (BusinessId, CreatedByStaffId) REFERENCES dbo.StaffMembers(BusinessId, Id) ON DELETE SET NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockTransfers_SourceWarehouse_Tenant') ALTER TABLE dbo.StockTransfers WITH CHECK ADD CONSTRAINT FK_StockTransfers_SourceWarehouse_Tenant FOREIGN KEY (BusinessId, SourceWarehouseId) REFERENCES dbo.Warehouses(BusinessId, Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockTransfers_DestinationWarehouse_Tenant') ALTER TABLE dbo.StockTransfers WITH CHECK ADD CONSTRAINT FK_StockTransfers_DestinationWarehouse_Tenant FOREIGN KEY (BusinessId, DestinationWarehouseId) REFERENCES dbo.Warehouses(BusinessId, Id);

    /* Rules that represent business invariants. */
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Bills_Amounts')
        ALTER TABLE dbo.Bills WITH CHECK ADD CONSTRAINT CK_Bills_Amounts CHECK (Subtotal >= 0 AND DiscountAmount >= 0 AND TaxAmount >= 0 AND TotalAmount >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BillItems_Values')
        ALTER TABLE dbo.BillItems WITH CHECK ADD CONSTRAINT CK_BillItems_Values CHECK (Quantity > 0 AND UnitPrice >= 0 AND LineTotal >= 0 AND TaxRate BETWEEN 0 AND 100);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PurchaseItems_Values')
        ALTER TABLE dbo.PurchaseItems WITH CHECK ADD CONSTRAINT CK_PurchaseItems_Values CHECK (Quantity > 0 AND UnitPrice >= 0 AND LineTotal >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventoryItems_Stock')
        ALTER TABLE dbo.InventoryItems WITH CHECK ADD CONSTRAINT CK_InventoryItems_Stock CHECK (CurrentStock >= 0 AND ReorderLevel >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PaymentTransactions_Amount')
        ALTER TABLE dbo.PaymentTransactions WITH CHECK ADD CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_StockTransfers_Warehouses')
        ALTER TABLE dbo.StockTransfers WITH CHECK ADD CONSTRAINT CK_StockTransfers_Warehouses CHECK (SourceWarehouseId <> DestinationWarehouseId);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PaymentTransactions_Status')
        ALTER TABLE dbo.PaymentTransactions WITH CHECK ADD CONSTRAINT CK_PaymentTransactions_Status CHECK (Status IN ('Created','Authorized','Captured','Failed','Refunded'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Bills_Status')
        ALTER TABLE dbo.Bills WITH CHECK ADD CONSTRAINT CK_Bills_Status CHECK (Status IN ('Pending','Paid','Failed','Cancelled','Refunded','PartialRefund','Completed'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Businesses_SubscriptionStatus')
        ALTER TABLE dbo.Businesses WITH CHECK ADD CONSTRAINT CK_Businesses_SubscriptionStatus CHECK (SubscriptionStatus IN ('Trial','Active','PastDue','Cancelled','Suspended','TrialExpired','Expired','Inactive'));

    /* Optional purchase-to-stock link: preserve history, block dangling references. */
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseItems_InventoryItems')
        ALTER TABLE dbo.PurchaseItems WITH CHECK ADD CONSTRAINT FK_PurchaseItems_InventoryItems FOREIGN KEY (InventoryItemId) REFERENCES dbo.InventoryItems(Id) ON DELETE SET NULL;

    /* Operational indexes: tenant-first access paths and audit/webhook cleanup. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bills_BusinessId_CreatedAt') CREATE INDEX IX_Bills_BusinessId_CreatedAt ON dbo.Bills(BusinessId, CreatedAt DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_BusinessId_CreatedAt') CREATE INDEX IX_PaymentTransactions_BusinessId_CreatedAt ON dbo.PaymentTransactions(BusinessId, CreatedAt DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerLedgers_BusinessId_CustomerId_TransactionDate') CREATE INDEX IX_CustomerLedgers_BusinessId_CustomerId_TransactionDate ON dbo.CustomerLedgers(BusinessId, CustomerId, TransactionDate DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryItems_BusinessId_CurrentStock') CREATE INDEX IX_InventoryItems_BusinessId_CurrentStock ON dbo.InventoryItems(BusinessId, CurrentStock);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_BusinessId_CreatedAt') CREATE INDEX IX_AuditLogs_BusinessId_CreatedAt ON dbo.AuditLogs(BusinessId, CreatedAt DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebhookEventLogs_Completed_ProcessedAt') CREATE INDEX IX_WebhookEventLogs_Completed_ProcessedAt ON dbo.WebhookEventLogs(ProcessingStatus, ProcessedAt);

    /* Retention job target: retain the event envelope, remove sensitive payload after the policy period. */
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.PurgeCompletedWebhookPayloads @RetentionDays int = 90 AS
        BEGIN
          SET NOCOUNT ON;
          IF @RetentionDays < 30 THROW 51007, ''Retention must be at least 30 days.'', 1;
          UPDATE dbo.WebhookEventLogs SET RawPayload = NULL
          WHERE ProcessingStatus = ''Completed'' AND ProcessedAt < DATEADD(day, -@RetentionDays, SYSUTCDATETIME()) AND RawPayload IS NOT NULL;
        END');

    INSERT INTO dbo.__SchemaMigrations(MigrationId, AppliedBy, Checksum) VALUES (N'004_IndustrialHardening', SUSER_SNAME(), N'v2');
END
COMMIT TRANSACTION;
