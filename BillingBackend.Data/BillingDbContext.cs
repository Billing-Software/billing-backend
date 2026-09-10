using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingBackend.Data
{
    public class BillingDbContext : DbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<BusinessSmsSettings> BusinessSmsSettings { get; set; }
        public DbSet<BusinessPaymentSettings> BusinessPaymentSettings { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<WhatsAppAccount> WhatsAppAccounts { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }
        public DbSet<MessageLog> MessageLogs { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<WebhookEventLog> WebhookEventLogs { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
        public DbSet<HSNMaster> HSNMasters { get; set; }
        public DbSet<SACMaster> SACMasters { get; set; }
        public DbSet<BusinessTypeMaster> BusinessTypeMasters { get; set; }
        public DbSet<AppFeature> AppFeatures { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<RoleFeature> RoleFeatures { get; set; }
        public DbSet<BusinessTaxSettings> BusinessTaxSettings { get; set; }
        public DbSet<BusinessInvoiceSettings> BusinessInvoiceSettings { get; set; }
        public DbSet<BusinessInvoiceDesign> BusinessInvoiceDesigns { get; set; }
        public DbSet<BusinessPrinterSettings> BusinessPrinterSettings { get; set; }
        public DbSet<CustomerLedger> CustomerLedgers { get; set; }
        public DbSet<DiscountCoupon> DiscountCoupons { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<BusinessAppPreferences> BusinessAppPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Users =====
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).HasColumnType("varbinary(max)");
                entity.Property(u => u.PasswordSalt).HasColumnType("varbinary(max)");
            });

            // ===== Businesses =====
            modelBuilder.Entity<Business>(entity =>
            {
                // 1:1 User → Business (user owns one business)
                entity.HasOne(b => b.Owner)
                    .WithOne(u => u.Business)
                    .HasForeignKey<Business>(b => b.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => b.OwnerId).IsUnique();
                entity.HasOne(b => b.ActivePlan)
                    .WithMany()
                    .HasForeignKey(b => b.ActivePlanId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.Property(b => b.RowVersion).IsRowVersion();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Businesses_SubscriptionStatus", "[SubscriptionStatus] IN ('Trial','Active','PastDue','Cancelled','Suspended','TrialExpired','Expired','Inactive')");
                });
            });

            // ===== Branches =====
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasOne(br => br.Business)
                    .WithMany(b => b.Branches)
                    .HasForeignKey(br => br.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique branch name per business
                entity.HasIndex(br => new { br.BusinessId, br.Name }).IsUnique();

                // Alternate key powers the composite tenant FKs (Bills, printer settings):
                // every branch reference is validated against (BusinessId, Id) in the database.
                // Name matches 004_IndustrialHardening so fresh and migrated DBs look identical.
                entity.HasAlternateKey(br => new { br.BusinessId, br.Id })
                    .HasName("UQ_Branches_BusinessId_Id");
            });

            // ===== Customers =====
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasOne(c => c.Business)
                    .WithMany(b => b.Customers)
                    .HasForeignKey(c => c.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Alternate key powers the composite tenant FK (Bills -> Customers).
                entity.HasAlternateKey(c => new { c.BusinessId, c.Id })
                    .HasName("UQ_Customers_BusinessId_Id");
            });

            // ===== Services =====
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasOne(s => s.Business)
                    .WithMany(b => b.Services)
                    .HasForeignKey(s => s.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique SKU per business
                entity.HasIndex(s => new { s.BusinessId, s.SKU }).IsUnique();
            });

            // ===== InventoryItems =====
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasOne(i => i.Business)
                    .WithMany(b => b.InventoryItems)
                    .HasForeignKey(i => i.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique SKU per business
                entity.HasIndex(i => new { i.BusinessId, i.SKU }).IsUnique();

                // Low-stock alert access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(i => new { i.BusinessId, i.CurrentStock })
                    .HasDatabaseName("IX_InventoryItems_BusinessId_CurrentStock");

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_InventoryItems_Stock", "[CurrentStock] >= 0 AND [ReorderLevel] >= 0");
                });
            });

            // ===== StaffMembers =====
            modelBuilder.Entity<StaffMember>(entity =>
            {
                entity.HasOne(s => s.Business)
                    .WithMany(b => b.StaffMembers)
                    .HasForeignKey(s => s.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Alternate key powers the composite tenant FK (Bills -> StaffMembers).
                entity.HasAlternateKey(s => new { s.BusinessId, s.Id })
                    .HasName("UQ_StaffMembers_BusinessId_Id");

                // Unique employee code per business
                entity.HasIndex(s => new { s.BusinessId, s.EmpCode }).IsUnique();
            });

            // ===== Bills =====
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasOne(bill => bill.Business)
                    .WithMany(b => b.Bills)
                    .HasForeignKey(bill => bill.BusinessId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(bill => bill.Branch)
                    .WithMany(br => br.Bills)
                    .HasForeignKey(bill => new { bill.BusinessId, bill.BranchId })
                    .HasPrincipalKey(br => new { br.BusinessId, br.Id })
                    .HasConstraintName("FK_Bills_Branches_Tenant")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(bill => bill.Customer)
                    .WithMany(c => c.Bills)
                    .HasForeignKey(bill => new { bill.BusinessId, bill.CustomerId })
                    .HasPrincipalKey(c => new { c.BusinessId, c.Id })
                    .HasConstraintName("FK_Bills_Customers_Tenant")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(bill => bill.CreatedByStaff)
                    .WithMany(s => s.CreatedBills)
                    .HasForeignKey(bill => new { bill.BusinessId, bill.CreatedByStaffId })
                    .HasPrincipalKey(s => new { s.BusinessId, s.Id })
                    .HasConstraintName("FK_Bills_StaffMembers_Tenant")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Unique bill number per business
                entity.HasIndex(bill => new { bill.BusinessId, bill.BillNumber }).IsUnique();

                // Unique idempotency key per business (prevents duplicate bill creation)
                entity.HasIndex(bill => new { bill.BusinessId, bill.IdempotencyKey })
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL");
                entity.Property(bill => bill.RowVersion).IsRowVersion();

                // Tenant-first reporting access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(bill => new { bill.BusinessId, bill.CreatedAt })
                    .HasDatabaseName("IX_Bills_BusinessId_CreatedAt")
                    .IsDescending(false, true);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Bills_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
                    t.HasCheckConstraint("CK_Bills_Status", "[Status] IN ('Pending','Paid','Failed','Cancelled','Refunded','PartialRefund','Completed')");
                });
            });

            // ===== BillItems =====
            modelBuilder.Entity<BillItem>(entity =>
            {
                entity.HasOne(bi => bi.Bill)
                    .WithMany(b => b.Items)
                    .HasForeignKey(bi => bi.BillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bi => bi.Service)
                    .WithMany(s => s.BillItems)
                    .HasForeignKey(bi => bi.ServiceId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_BillItems_Values", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [LineTotal] >= 0 AND [TaxRate] BETWEEN 0 AND 100");
                });
            });

            // ===== BusinessPaymentSettings =====
            modelBuilder.Entity<BusinessPaymentSettings>(entity =>
            {
                entity.HasOne(bps => bps.Business)
                    .WithOne(b => b.PaymentSettings)
                    .HasForeignKey<BusinessPaymentSettings>(bps => bps.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(bps => bps.BusinessId).IsUnique();
            });

            // ===== BusinessTaxSettings =====
            modelBuilder.Entity<BusinessTaxSettings>(entity =>
            {
                entity.HasOne(bts => bts.Business)
                    .WithOne(b => b.TaxSettings)
                    .HasForeignKey<BusinessTaxSettings>(bts => bts.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(bts => bts.BusinessId).IsUnique();
                entity.Property(bts => bts.DefaultTaxRate).HasPrecision(5, 2);
                entity.Property(bts => bts.EWayBillThreshold).HasPrecision(18, 2);
            });

            // ===== BusinessInvoiceSettings =====
            modelBuilder.Entity<BusinessInvoiceSettings>(entity =>
            {
                entity.HasOne(bis => bis.Business)
                    .WithOne(b => b.InvoiceSettings)
                    .HasForeignKey<BusinessInvoiceSettings>(bis => bis.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(bis => bis.BusinessId).IsUnique();
            });

            // ===== BusinessInvoiceDesigns =====
            modelBuilder.Entity<BusinessInvoiceDesign>(entity =>
            {
                entity.HasOne(bid => bid.Business)
                    .WithOne(b => b.InvoiceDesign)
                    .HasForeignKey<BusinessInvoiceDesign>(bid => bid.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(bid => bid.BusinessId).IsUnique();
            });

            // ===== BusinessPrinterSettings =====
            modelBuilder.Entity<BusinessPrinterSettings>(entity =>
            {
                entity.HasOne(bps => bps.Business)
                    .WithMany(b => b.PrinterSettings)
                    .HasForeignKey(bps => bps.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bps => bps.Branch)
                    .WithMany()
                    .HasForeignKey(bps => bps.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(bps => bps.BusinessId);
            });

            // ===== CustomerLedgers =====
            modelBuilder.Entity<CustomerLedger>(entity =>
            {
                entity.HasOne(cl => cl.Business)
                    .WithMany(b => b.CustomerLedgers)
                    .HasForeignKey(cl => cl.BusinessId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(cl => cl.Customer)
                    .WithMany()
                    .HasForeignKey(cl => cl.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cl => cl.Bill)
                    .WithMany()
                    .HasForeignKey(cl => cl.BillId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cl => cl.RecordedByStaff)
                    .WithMany()
                    .HasForeignKey(cl => cl.RecordedByStaffId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(cl => cl.Amount).HasPrecision(18, 2);
                entity.Property(cl => cl.RunningBalance).HasPrecision(18, 2);

                entity.HasIndex(cl => cl.BusinessId);
                entity.HasIndex(cl => cl.CustomerId);
                entity.HasIndex(cl => cl.TransactionDate);

                // Khata statement access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(cl => new { cl.BusinessId, cl.CustomerId, cl.TransactionDate })
                    .HasDatabaseName("IX_CustomerLedgers_BusinessId_CustomerId_TransactionDate")
                    .IsDescending(false, false, true);
            });

            // ===== DiscountCoupons =====
            modelBuilder.Entity<DiscountCoupon>(entity =>
            {
                entity.HasOne(dc => dc.Business)
                    .WithMany(b => b.DiscountCoupons)
                    .HasForeignKey(dc => dc.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(dc => dc.DiscountValue).HasPrecision(18, 2);
                entity.Property(dc => dc.MinimumOrderAmount).HasPrecision(18, 2);
                entity.Property(dc => dc.MaximumDiscountAmount).HasPrecision(18, 2);

                entity.HasIndex(dc => new { dc.BusinessId, dc.CouponCode }).IsUnique();
            });

            // ===== Warehouses =====
            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasOne(w => w.Business)
                    .WithMany(b => b.Warehouses)
                    .HasForeignKey(w => w.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Branch)
                    .WithMany()
                    .HasForeignKey(w => w.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(w => new { w.BusinessId, w.Code }).IsUnique();

                // Alternate key powers the composite tenant FKs (StockTransfers).
                entity.HasAlternateKey(w => new { w.BusinessId, w.Id })
                    .HasName("UQ_Warehouses_BusinessId_Id");
            });

            // ===== StockTransfers =====
            modelBuilder.Entity<StockTransfer>(entity =>
            {
                entity.HasOne(st => st.Business)
                    .WithMany()
                    .HasForeignKey(st => st.BusinessId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(st => st.SourceWarehouse)
                    .WithMany()
                    .HasForeignKey(st => new { st.BusinessId, st.SourceWarehouseId })
                    .HasPrincipalKey(w => new { w.BusinessId, w.Id })
                    .HasConstraintName("FK_StockTransfers_SourceWarehouse_Tenant")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(st => st.DestinationWarehouse)
                    .WithMany()
                    .HasForeignKey(st => new { st.BusinessId, st.DestinationWarehouseId })
                    .HasPrincipalKey(w => new { w.BusinessId, w.Id })
                    .HasConstraintName("FK_StockTransfers_DestinationWarehouse_Tenant")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(st => new { st.BusinessId, st.TransferNumber }).IsUnique();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_StockTransfers_Warehouses", "[SourceWarehouseId] <> [DestinationWarehouseId]");
                });
            });

            // ===== StockTransferItems =====
            modelBuilder.Entity<StockTransferItem>(entity =>
            {
                entity.HasOne(sti => sti.StockTransfer)
                    .WithMany(st => st.Items)
                    .HasForeignKey(sti => sti.StockTransferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sti => sti.InventoryItem)
                    .WithMany()
                    .HasForeignKey(sti => sti.InventoryItemId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Property(sti => sti.Quantity).HasPrecision(18, 2);
            });

            // ===== BusinessAppPreferences =====
            modelBuilder.Entity<BusinessAppPreferences>(entity =>
            {
                entity.HasOne(bap => bap.Business)
                    .WithOne(b => b.AppPreferences)
                    .HasForeignKey<BusinessAppPreferences>(bap => bap.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(bap => bap.BusinessId).IsUnique();
            });

            // ===== WhatsAppAccounts =====
            modelBuilder.Entity<WhatsAppAccount>(entity =>
            {
                entity.HasOne(w => w.Business)
                    .WithOne(b => b.WhatsAppAccount)
                    .HasForeignKey<WhatsAppAccount>(w => w.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(w => w.BusinessId).IsUnique();
            });

            // ===== WhatsAppTemplates =====
            modelBuilder.Entity<WhatsAppTemplate>(entity =>
            {
                entity.HasOne(t => t.WhatsAppAccount)
                    .WithMany(w => w.WhatsAppTemplates)
                    .HasForeignKey(t => t.WhatsAppAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(t => new { t.WhatsAppAccountId, t.TemplateName }).IsUnique();
            });

            // ===== MessageLogs =====
            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.HasOne(m => m.WhatsAppAccount)
                    .WithMany(w => w.MessageLogs)
                    .HasForeignKey(m => m.WhatsAppAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Bill)
                    .WithMany()
                    .HasForeignKey(m => m.BillId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Index for fast webhook lookups by message ID
                entity.HasIndex(m => m.MetaMessageId);
                entity.HasIndex(m => m.TwilioMessageSid);
            });
            // ===== Expenses =====
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Purchases =====
            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.HasOne(p => p.Business)
                    .WithMany()
                    .HasForeignKey(p => p.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== PurchaseItems =====
            modelBuilder.Entity<PurchaseItem>(entity =>
            {
                entity.HasOne(pi => pi.Purchase)
                    .WithMany(p => p.Items)
                    .HasForeignKey(pi => pi.PurchaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional stock link: history survives inventory cleanup, dangling ids are blocked.
                entity.HasOne(pi => pi.InventoryItem)
                    .WithMany()
                    .HasForeignKey(pi => pi.InventoryItemId)
                    .HasConstraintName("FK_PurchaseItems_InventoryItems")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PurchaseItems_Values", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [LineTotal] >= 0");
                });
            });

            // ===== UserRefreshTokens =====
            modelBuilder.Entity<UserRefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rt => rt.Token).IsUnique();
            });

            // ===== PendingRegistrations =====
            modelBuilder.Entity<PendingRegistration>(entity =>
            {
                entity.HasIndex(pr => pr.Token).IsUnique();
                entity.HasIndex(pr => pr.Email);
                entity.Property(pr => pr.PasswordHash).HasColumnType("varbinary(max)");
                entity.Property(pr => pr.PasswordSalt).HasColumnType("varbinary(max)");
            });

            // ===== AuditLogs =====
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
                entity.HasIndex(a => a.BusinessId);
                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => a.CorrelationId);

                // Tenant audit-trail access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(a => new { a.BusinessId, a.CreatedAt })
                    .HasDatabaseName("IX_AuditLogs_BusinessId_CreatedAt")
                    .IsDescending(false, true);
            });

            // ===== PaymentTransactions =====
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
                entity.HasOne(p => p.Business).WithMany().HasForeignKey(p => p.BusinessId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(p => p.SubscriptionPlan).WithMany().HasForeignKey(p => p.SubscriptionPlanId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(p => p.RazorpayOrderId).IsUnique().HasFilter("[RazorpayOrderId] IS NOT NULL");
                entity.HasIndex(p => p.RazorpayPaymentId).IsUnique().HasFilter("[RazorpayPaymentId] IS NOT NULL");
                entity.Property(p => p.RowVersion).IsRowVersion();

                // Tenant-first reporting access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(p => new { p.BusinessId, p.CreatedAt })
                    .HasDatabaseName("IX_PaymentTransactions_BusinessId_CreatedAt")
                    .IsDescending(false, true);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PaymentTransactions_Amount", "[Amount] >= 0");
                    t.HasCheckConstraint("CK_PaymentTransactions_Status", "[Status] IN ('Created','Authorized','Captured','Failed','Refunded')");
                });
            });

            // ===== SubscriptionPlans =====
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.Property(s => s.MonthlyPrice).HasPrecision(18, 2);
                entity.Property(s => s.YearlyPrice).HasPrecision(18, 2);
            });

            // ===== AppFeatures =====
            modelBuilder.Entity<AppFeature>(entity =>
            {
                entity.HasIndex(f => f.FeatureKey).IsUnique();
            });

            // ===== PlanFeatures =====
            modelBuilder.Entity<PlanFeature>(entity =>
            {
                entity.HasOne(pf => pf.Plan)
                    .WithMany()
                    .HasForeignKey(pf => pf.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pf => pf.Feature)
                    .WithMany(f => f.PlanFeatures)
                    .HasForeignKey(pf => pf.FeatureId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(pf => new { pf.PlanId, pf.FeatureId }).IsUnique();
            });

            // ===== RoleFeatures =====
            modelBuilder.Entity<RoleFeature>(entity =>
            {
                entity.HasOne(rf => rf.Feature)
                    .WithMany(f => f.RoleFeatures)
                    .HasForeignKey(rf => rf.FeatureId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rf => new { rf.RoleName, rf.FeatureId }).IsUnique();
            });

            // ===== WebhookEventLogs =====
            modelBuilder.Entity<WebhookEventLog>(entity =>
            {
                entity.HasIndex(w => new { w.Source, w.ExternalEventId }).IsUnique()
                    .HasFilter("[ExternalEventId] IS NOT NULL");
                entity.HasIndex(w => w.ProcessingStatus);
                entity.HasIndex(w => w.CreatedAt);

                // Retention-job access path (mirrors 004_IndustrialHardening).
                entity.HasIndex(w => new { w.ProcessingStatus, w.ProcessedAt })
                    .HasDatabaseName("IX_WebhookEventLogs_Completed_ProcessedAt");
            });

            // ===== Seed Subscription Plans =====
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Starter Shop",
                    Subtitle = "Ideal for Single Kirana, Small Cafes & Standalone Stores",
                    RazorpayPlanIdMonthly = "plan_starter_monthly",
                    RazorpayPlanIdYearly = "plan_starter_yearly",
                    MonthlyPrice = 499.00m,
                    YearlyPrice = 4999.00m,
                    MaxBranches = 1,
                    MaxStaff = 2,
                    IsPopular = false,
                    DisplayOrder = 1,
                    FeaturesJson = "[{\"text\":\"Single Store & Counter POS\",\"included\":true},{\"text\":\"2 Cashier Staff Accounts\",\"included\":true},{\"text\":\"Thermal & A4 Tax Invoice Printing\",\"included\":true},{\"text\":\"Customer Udhar Khata Ledger\",\"included\":true},{\"text\":\"Stock Warning Alerts\",\"included\":true},{\"text\":\"Multi-Branch Franchise Sync\",\"included\":false},{\"text\":\"Stylist Commission Calculator\",\"included\":false}]",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Growth Business",
                    Subtitle = "Perfect for High-Volume Retailers, Salons & Restaurants",
                    RazorpayPlanIdMonthly = "plan_growth_monthly",
                    RazorpayPlanIdYearly = "plan_growth_yearly",
                    MonthlyPrice = 999.00m,
                    YearlyPrice = 9999.00m,
                    MaxBranches = 3,
                    MaxStaff = 10,
                    IsPopular = true,
                    DisplayOrder = 2,
                    FeaturesJson = "[{\"text\":\"Up to 3 Store Outlets\",\"included\":true},{\"text\":\"10 Staff Accounts & Role Controls\",\"included\":true},{\"text\":\"Automated DLT SMS Receipts\",\"included\":true},{\"text\":\"Barcode & Electronic Scale Integration\",\"included\":true},{\"text\":\"Kitchen KOT & Table Layouts\",\"included\":true},{\"text\":\"GST E-Invoicing & Tally Prime Sync\",\"included\":true},{\"text\":\"Operating Expense & Profit Tracker\",\"included\":true}]",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Enterprise Chain",
                    Subtitle = "Custom Architecture for Large Multi-City Franchises",
                    RazorpayPlanIdMonthly = "plan_enterprise_monthly",
                    RazorpayPlanIdYearly = "plan_enterprise_yearly",
                    MonthlyPrice = 2499.00m,
                    YearlyPrice = 24999.00m,
                    MaxBranches = 25,
                    MaxStaff = 50,
                    IsPopular = false,
                    DisplayOrder = 3,
                    FeaturesJson = "[{\"text\":\"Unlimited Outlets & Central Warehouse\",\"included\":true},{\"text\":\"50 Staff Accounts with Role Controls\",\"included\":true},{\"text\":\"Dedicated Account Manager & 24/7 SLA\",\"included\":true},{\"text\":\"Custom ERP & Tally 2-Way Sync\",\"included\":true},{\"text\":\"Multi-Branch Royalty & P&L Analytics\",\"included\":true},{\"text\":\"High-Throughput Exotel DLT SMS\",\"included\":true}]",
                    IsActive = true
                }
            );

            // ===== Seed AppFeatures =====
            modelBuilder.Entity<AppFeature>().HasData(
                new AppFeature { Id = 1, FeatureKey = "dashboard", DisplayName = "Dashboard", Description = "Main dashboard overview", Category = "Core" },
                new AppFeature { Id = 2, FeatureKey = "billing", DisplayName = "Billing & Invoicing", Description = "Create new bills and invoices", Category = "Core" },
                new AppFeature { Id = 3, FeatureKey = "invoices", DisplayName = "Invoice History", Description = "View and manage past invoices", Category = "Core" },
                new AppFeature { Id = 4, FeatureKey = "customers", DisplayName = "Customer Management", Description = "CRM customer directory", Category = "Core" },
                new AppFeature { Id = 5, FeatureKey = "services", DisplayName = "Service Catalog", Description = "Manage service listings", Category = "Core" },
                new AppFeature { Id = 6, FeatureKey = "inventory", DisplayName = "Inventory & Stock", Description = "Product stock management", Category = "Standard" },
                new AppFeature { Id = 7, FeatureKey = "reports", DisplayName = "GST Reports", Description = "Tax and sales reports", Category = "Standard" },
                new AppFeature { Id = 8, FeatureKey = "staff_manage", DisplayName = "Staff Management", Description = "Manage staff members and roles", Category = "Standard" },
                new AppFeature { Id = 9, FeatureKey = "branches", DisplayName = "Multi-Branch Sync", Description = "Multi-location branch management", Category = "Advanced" },
                new AppFeature { Id = 10, FeatureKey = "expenses", DisplayName = "Expense Tracking", Description = "Track business expenses", Category = "Standard" },
                new AppFeature { Id = 11, FeatureKey = "settings", DisplayName = "Business Settings", Description = "Configure business preferences", Category = "Core" },
                new AppFeature { Id = 12, FeatureKey = "whatsapp", DisplayName = "WhatsApp Integration", Description = "WhatsApp messaging webhooks", Category = "Advanced" },
                new AppFeature { Id = 13, FeatureKey = "purchases", DisplayName = "Purchase Management", Description = "Track supplier purchases", Category = "Standard" },
                new AppFeature { Id = 14, FeatureKey = "custom_templates", DisplayName = "Custom Invoice Templates", Description = "Custom PDF invoice designs", Category = "Premium" },
                new AppFeature { Id = 15, FeatureKey = "api_webhooks", DisplayName = "API & Webhook Access", Description = "External API integrations", Category = "Premium" },
                new AppFeature { Id = 16, FeatureKey = "dedicated_db", DisplayName = "Dedicated Database", Description = "Dedicated database cluster", Category = "Premium" }
            );

            // ===== Seed PlanFeatures (Plan → Feature mapping) =====
            // Starter Plan (Id=1): Core + Standard features, no Advanced/Premium
            // Growth Plan (Id=2): Core + Standard + Advanced, no Premium
            // Enterprise Plan (Id=3): Everything enabled
            var planFeatureId = 0;
            var planFeatureList = new List<PlanFeature>();
            int[] allFeatureIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            int[] starterEnabled = { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 13 }; // No branches(9), whatsapp(12), custom_templates(14), api_webhooks(15), dedicated_db(16)
            int[] growthEnabled = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }; // No custom_templates(14), api_webhooks(15), dedicated_db(16)

            foreach (var fid in allFeatureIds)
            {
                // Starter
                planFeatureList.Add(new PlanFeature { Id = ++planFeatureId, PlanId = 1, FeatureId = fid, IsEnabled = Array.Exists(starterEnabled, x => x == fid) });
                // Growth
                planFeatureList.Add(new PlanFeature { Id = ++planFeatureId, PlanId = 2, FeatureId = fid, IsEnabled = Array.Exists(growthEnabled, x => x == fid) });
                // Enterprise - all enabled
                planFeatureList.Add(new PlanFeature { Id = ++planFeatureId, PlanId = 3, FeatureId = fid, IsEnabled = true });
            }
            modelBuilder.Entity<PlanFeature>().HasData(planFeatureList.ToArray());

            // ===== Seed RoleFeatures (Role → Feature mapping) =====
            // Owner: full access to business features
            // Staff: billing, invoices, customers, services only
            // SuperAdmin: dashboard + reports only (system-level access)
            var roleFeatureId = 0;
            var roleFeatureList = new List<RoleFeature>();
            int[] ownerEnabled = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }; // All
            int[] staffEnabled = { 1, 2, 3, 4, 5 }; // dashboard, billing, invoices, customers, services
            int[] superAdminEnabled = { 1, 7 }; // dashboard, reports

            foreach (var fid in allFeatureIds)
            {
                roleFeatureList.Add(new RoleFeature { Id = ++roleFeatureId, RoleName = "Owner", FeatureId = fid, IsEnabled = Array.Exists(ownerEnabled, x => x == fid) });
                roleFeatureList.Add(new RoleFeature { Id = ++roleFeatureId, RoleName = "Staff", FeatureId = fid, IsEnabled = Array.Exists(staffEnabled, x => x == fid) });
                roleFeatureList.Add(new RoleFeature { Id = ++roleFeatureId, RoleName = "SuperAdmin", FeatureId = fid, IsEnabled = Array.Exists(superAdminEnabled, x => x == fid) });
            }

            modelBuilder.Entity<RoleFeature>().HasData(roleFeatureList.ToArray());

            // ===== Seed Tax Categories =====
            modelBuilder.Entity<TaxCategory>().HasData(
                new TaxCategory { Id = 1, BusinessId = null, Name = "Standard Goods (18%)", TaxType = "Goods", HSNCode = "9999", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m },
                new TaxCategory { Id = 2, BusinessId = null, Name = "Reduced Goods (5%)", TaxType = "Goods", HSNCode = "1001", GSTPercentage = 5.00m, CGSTPercentage = 2.50m, SGSTPercentage = 2.50m, IGSTPercentage = 5.00m, CessPercentage = 0.00m },
                new TaxCategory { Id = 3, BusinessId = null, Name = "Essential / Exempt (0%)", TaxType = "Goods", HSNCode = "0000", GSTPercentage = 0.00m, CGSTPercentage = 0.00m, SGSTPercentage = 0.00m, IGSTPercentage = 0.00m, CessPercentage = 0.00m },
                new TaxCategory { Id = 4, BusinessId = null, Name = "Luxury Goods (28% + Cess)", TaxType = "Goods", HSNCode = "8703", GSTPercentage = 28.00m, CGSTPercentage = 14.00m, SGSTPercentage = 14.00m, IGSTPercentage = 28.00m, CessPercentage = 12.00m },
                new TaxCategory { Id = 5, BusinessId = null, Name = "Restaurant Service (5%)", TaxType = "Services", SACCode = "996331", GSTPercentage = 5.00m, CGSTPercentage = 2.50m, SGSTPercentage = 2.50m, IGSTPercentage = 5.00m, CessPercentage = 0.00m },
                new TaxCategory { Id = 6, BusinessId = null, Name = "IT & Professional Services (18%)", TaxType = "Services", SACCode = "998313", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m },
                new TaxCategory { Id = 7, BusinessId = null, Name = "Personal Care / Salon (18%)", TaxType = "Services", SACCode = "999721", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m }
            );

            // ===== Seed HSN Master =====
            modelBuilder.Entity<HSNMaster>().HasData(
                new HSNMaster { Id = 1, Code = "6109", Description = "T-shirts, singlets and other vests, knitted or crocheted", SearchTerms = "tshirt t-shirt shirt clothing garment apparel", UQC = "PCS", DefaultGSTPercentage = 5.00m },
                new HSNMaster { Id = 2, Code = "1006", Description = "Rice, husk rice, husked rice, semi-milled or wholly milled rice", SearchTerms = "rice grocery grain kirana food basmati", UQC = "KGS", DefaultGSTPercentage = 5.00m },
                new HSNMaster { Id = 3, Code = "1701", Description = "Cane or beet sugar and chemically pure sucrose, in solid form", SearchTerms = "sugar grocery sweet kirana", UQC = "KGS", DefaultGSTPercentage = 5.00m },
                new HSNMaster { Id = 4, Code = "8517", Description = "Telephone sets, including smartphones and other apparatus for cellular networks", SearchTerms = "mobile phone smartphone electronics laptop computer accessory", UQC = "PCS", DefaultGSTPercentage = 18.00m },
                new HSNMaster { Id = 5, Code = "3004", Description = "Medicaments consisting of mixed or unmixed products for therapeutic uses", SearchTerms = "medicine tablet capsule pharmacy medical drug doctor syrup", UQC = "BOX", DefaultGSTPercentage = 12.00m },
                new HSNMaster { Id = 6, Code = "1905", Description = "Bread, pastry, cakes, biscuits and other bakers wares", SearchTerms = "bakery bread biscuit cake pastry cookies snack", UQC = "PCS", DefaultGSTPercentage = 18.00m }
            );

            // ===== Seed SAC Master =====
            modelBuilder.Entity<SACMaster>().HasData(
                new SACMaster { Id = 1, Code = "996331", Description = "Services provided by restaurants, cafes and other similar eating establishments", SearchTerms = "restaurant food idly dosa coffee mess tiffin dining cloud kitchen hotel", DefaultGSTPercentage = 5.00m },
                new SACMaster { Id = 2, Code = "998313", Description = "Information technology (IT) design and development services for applications", SearchTerms = "software web development IT services consulting computer programming website app", DefaultGSTPercentage = 18.00m },
                new SACMaster { Id = 3, Code = "999721", Description = "Hairdressing and beauty treatment services, salon, spa and personal care", SearchTerms = "salon barber haircut beauty parlour spa facial styling makeup massage", DefaultGSTPercentage = 18.00m },
                new SACMaster { Id = 4, Code = "998713", Description = "Maintenance and repair services of computers, mobile phones and consumer electronics", SearchTerms = "repair maintenance laptop computer fixing service mobile hardware", DefaultGSTPercentage = 18.00m }
            );

            // ===== Seed Business Type Master =====
            modelBuilder.Entity<BusinessTypeMaster>().HasData(
                new BusinessTypeMaster
                {
                    Id = 1,
                    Code = "restaurant",
                    Name = "Restaurant",
                    Category = "Food & Hospitality",
                    IconName = "Utensils",
                    SellingModel = "GOODS_AND_SERVICES",
                    AliasesJson = "[\"hotel\",\"dine in\",\"eatery\",\"food court\",\"dhaba\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Menu Item\",\"plural\":\"Menu Items\"},\"service\":{\"singular\":\"Dining Service\",\"plural\":\"Dining Services\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Order / Bill\",\"plural\":\"Orders / Bills\"},\"inventory\":{\"singular\":\"Ingredient Stock\",\"plural\":\"Ingredient Stock\"},\"purchase\":{\"singular\":\"Ingredient Purchase\",\"plural\":\"Ingredient Purchases\"},\"supplier\":{\"singular\":\"Vendor / Supplier\",\"plural\":\"Vendors & Suppliers\"},\"staff\":{\"singular\":\"Staff / Waiter\",\"plural\":\"Staff & Waiters\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 2,
                    Code = "tiffin_center",
                    Name = "Tiffin Center / Mess",
                    Category = "Food & Hospitality",
                    IconName = "Soup",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"mess\",\"tiffin\",\"canteen\",\"fast food\",\"food stall\",\"tiffin service\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":false,\"appointments\":false,\"customers\":true,\"staff\":false,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Food Item\",\"plural\":\"Food Items\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Bill\",\"plural\":\"Bills\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 3,
                    Code = "bakery",
                    Name = "Bakery & Confectionery",
                    Category = "Food & Hospitality",
                    IconName = "Cake",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"bakery\",\"cake shop\",\"pastry\",\"sweets\",\"sweet shop\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Bakery Item\",\"plural\":\"Bakery Items\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Bill\",\"plural\":\"Bills\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 4,
                    Code = "grocery_kirana",
                    Name = "Grocery / Kirana Store",
                    Category = "Retail",
                    IconName = "ShoppingCart",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"kirana\",\"grocery\",\"provision store\",\"super market\",\"general store\",\"departmental store\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Product\",\"plural\":\"Products\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Bill\",\"plural\":\"Bills\"},\"inventory\":{\"singular\":\"Stock\",\"plural\":\"Stock & Inventory\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 5,
                    Code = "clothing_store",
                    Name = "Clothing / Garments Store",
                    Category = "Retail",
                    IconName = "Shirt",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"garments\",\"cloth shop\",\"boutique\",\"fashion\",\"textile retail\",\"apparel\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Garment / Item\",\"plural\":\"Apparel & Garments\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Invoice / Bill\",\"plural\":\"Invoices & Bills\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 6,
                    Code = "pharmacy",
                    Name = "Pharmacy / Medical Store",
                    Category = "Retail",
                    IconName = "Pill",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"medical shop\",\"chemist\",\"pharmacy\",\"medical store\",\"drugs store\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Medicine\",\"plural\":\"Medicines & Drugs\"},\"customer\":{\"singular\":\"Patient / Customer\",\"plural\":\"Patients & Customers\"},\"invoice\":{\"singular\":\"Tax Invoice\",\"plural\":\"Tax Invoices\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 7,
                    Code = "electronics_mobile",
                    Name = "Electronics & Mobile Store",
                    Category = "Retail",
                    IconName = "Smartphone",
                    SellingModel = "GOODS_AND_SERVICES",
                    AliasesJson = "[\"computer shop\",\"laptop store\",\"mobile store\",\"mobile accessories\",\"electronics\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":true,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Device / Product\",\"plural\":\"Products & Accessories\"},\"service\":{\"singular\":\"Repair / Service\",\"plural\":\"Repair Services\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Invoice\",\"plural\":\"Invoices\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 8,
                    Code = "salon_barber",
                    Name = "Salon / Barber / Beauty Parlour",
                    Category = "Personal Services",
                    IconName = "Scissors",
                    SellingModel = "SERVICES_ONLY",
                    AliasesJson = "[\"saloon\",\"barber\",\"parlour\",\"beauty parlour\",\"spa\",\"hairdresser\",\"hair spa\"]",
                    DefaultFeaturesJson = "{\"products\":false,\"services\":true,\"inventory\":false,\"appointments\":true,\"customers\":true,\"staff\":true,\"khata\":false,\"purchases\":false,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Product\",\"plural\":\"Products\"},\"service\":{\"singular\":\"Service\",\"plural\":\"Services\"},\"customer\":{\"singular\":\"Client / Customer\",\"plural\":\"Clients & Customers\"},\"invoice\":{\"singular\":\"Bill / Receipt\",\"plural\":\"Bills & Receipts\"},\"staff\":{\"singular\":\"Stylist / Staff\",\"plural\":\"Stylists & Staff\"},\"appointment\":{\"singular\":\"Appointment\",\"plural\":\"Appointments\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 9,
                    Code = "repair_services",
                    Name = "Repair & Maintenance Services",
                    Category = "Personal Services",
                    IconName = "Wrench",
                    SellingModel = "GOODS_AND_SERVICES",
                    AliasesJson = "[\"repair shop\",\"service center\",\"bike repair\",\"car mechanic\",\"appliance repair\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":true,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Spare Part\",\"plural\":\"Spare Parts\"},\"service\":{\"singular\":\"Repair Service\",\"plural\":\"Repair Services\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Service Bill\",\"plural\":\"Service Bills\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 10,
                    Code = "software_it_services",
                    Name = "Software / IT Services",
                    Category = "Professional Services",
                    IconName = "Code",
                    SellingModel = "SERVICES_ONLY",
                    AliasesJson = "[\"IT company\",\"software house\",\"web development\",\"digital agency\",\"consultancy\",\"freelancer\"]",
                    DefaultFeaturesJson = "{\"products\":false,\"services\":true,\"inventory\":false,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":false,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"service\":{\"singular\":\"Service / Plan\",\"plural\":\"Services & Offerings\"},\"customer\":{\"singular\":\"Client\",\"plural\":\"Clients\"},\"invoice\":{\"singular\":\"GST Invoice\",\"plural\":\"GST Invoices\"},\"staff\":{\"singular\":\"Team Member\",\"plural\":\"Team Members\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 11,
                    Code = "consulting_accounting",
                    Name = "Consulting & Professional Services",
                    Category = "Professional Services",
                    IconName = "Briefcase",
                    SellingModel = "SERVICES_ONLY",
                    AliasesJson = "[\"ca firm\",\"accounting\",\"legal services\",\"architect\",\"business consultancy\",\"advisory\"]",
                    DefaultFeaturesJson = "{\"products\":false,\"services\":true,\"inventory\":false,\"appointments\":true,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":false,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"service\":{\"singular\":\"Consultation / Service\",\"plural\":\"Services & Retainers\"},\"customer\":{\"singular\":\"Client\",\"plural\":\"Clients\"},\"invoice\":{\"singular\":\"Tax Invoice\",\"plural\":\"Tax Invoices\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 12,
                    Code = "healthcare_clinic",
                    Name = "Clinic / Healthcare Center",
                    Category = "Healthcare",
                    IconName = "Activity",
                    SellingModel = "SERVICES_ONLY",
                    AliasesJson = "[\"doctor clinic\",\"hospital\",\"dental clinic\",\"physiotherapy\",\"diagnostic\"]",
                    DefaultFeaturesJson = "{\"products\":false,\"services\":true,\"inventory\":false,\"appointments\":true,\"customers\":true,\"staff\":true,\"khata\":false,\"purchases\":false,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"service\":{\"singular\":\"Treatment / Consultation\",\"plural\":\"Treatments & Consultations\"},\"customer\":{\"singular\":\"Patient\",\"plural\":\"Patients\"},\"invoice\":{\"singular\":\"Medical Bill\",\"plural\":\"Medical Bills\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 13,
                    Code = "coaching_education",
                    Name = "Tuition / Coaching Center",
                    Category = "Education",
                    IconName = "GraduationCap",
                    SellingModel = "SERVICES_ONLY",
                    AliasesJson = "[\"tuition\",\"coaching\",\"institute\",\"school\",\"academy\",\"training center\"]",
                    DefaultFeaturesJson = "{\"products\":false,\"services\":true,\"inventory\":false,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":false,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"service\":{\"singular\":\"Course / Fee\",\"plural\":\"Courses & Batches\"},\"customer\":{\"singular\":\"Student / Parent\",\"plural\":\"Students & Parents\"},\"invoice\":{\"singular\":\"Fee Receipt\",\"plural\":\"Fee Receipts\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 14,
                    Code = "manufacturing",
                    Name = "Manufacturing & Production",
                    Category = "Manufacturing",
                    IconName = "Factory",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"factory\",\"workshop\",\"production unit\",\"maker\",\"textile mill\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Finished Good\",\"plural\":\"Finished Goods\"},\"customer\":{\"singular\":\"Distributor / Buyer\",\"plural\":\"Distributors & Buyers\"},\"invoice\":{\"singular\":\"Commercial Invoice\",\"plural\":\"Commercial Invoices\"},\"inventory\":{\"singular\":\"Raw Material & Stock\",\"plural\":\"Raw Materials & Goods\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 15,
                    Code = "wholesale",
                    Name = "Wholesale & Trading",
                    Category = "Wholesale",
                    IconName = "Boxes",
                    SellingModel = "GOODS_ONLY",
                    AliasesJson = "[\"wholesaler\",\"distributor\",\"trader\",\"stockist\",\"bulk supplier\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":false,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Wholesale Item\",\"plural\":\"Wholesale Products\"},\"customer\":{\"singular\":\"Retailer / Client\",\"plural\":\"Retailers & Clients\"},\"invoice\":{\"singular\":\"Tax Invoice\",\"plural\":\"Tax Invoices\"}}"
                },
                new BusinessTypeMaster
                {
                    Id = 16,
                    Code = "general_retail",
                    Name = "General Retail Store",
                    Category = "Retail",
                    IconName = "Store",
                    SellingModel = "GOODS_AND_SERVICES",
                    AliasesJson = "[\"general\",\"other\",\"retail\",\"shop\"]",
                    DefaultFeaturesJson = "{\"products\":true,\"services\":true,\"inventory\":true,\"appointments\":false,\"customers\":true,\"staff\":true,\"khata\":true,\"purchases\":true,\"expenses\":true}",
                    DefaultTerminologyJson = "{\"product\":{\"singular\":\"Product\",\"plural\":\"Products\"},\"service\":{\"singular\":\"Service\",\"plural\":\"Services\"},\"customer\":{\"singular\":\"Customer\",\"plural\":\"Customers\"},\"invoice\":{\"singular\":\"Bill / Invoice\",\"plural\":\"Bills & Invoices\"}}"
                }
            );
        }
    }
}
