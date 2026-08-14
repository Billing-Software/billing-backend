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
            });

            // ===== Customers =====
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasOne(c => c.Business)
                    .WithMany(b => b.Customers)
                    .HasForeignKey(c => c.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    .HasForeignKey(bill => bill.BranchId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(bill => bill.Customer)
                    .WithMany(c => c.Bills)
                    .HasForeignKey(bill => bill.CustomerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(bill => bill.CreatedByStaff)
                    .WithMany(s => s.CreatedBills)
                    .HasForeignKey(bill => bill.CreatedByStaffId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Unique bill number per business
                entity.HasIndex(bill => new { bill.BusinessId, bill.BillNumber }).IsUnique();

                // Unique idempotency key per business (prevents duplicate bill creation)
                entity.HasIndex(bill => new { bill.BusinessId, bill.IdempotencyKey })
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL");
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

                // Index for fast webhook lookups by Meta message ID
                entity.HasIndex(m => m.MetaMessageId);
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
            });

            // ===== PaymentTransactions =====
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
            });

            // ===== SubscriptionPlans =====
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.Property(s => s.MonthlyPrice).HasPrecision(18, 2);
                entity.Property(s => s.YearlyPrice).HasPrecision(18, 2);
            });

            // ===== WebhookEventLogs =====
            modelBuilder.Entity<WebhookEventLog>(entity =>
            {
                entity.HasIndex(w => new { w.Source, w.ExternalEventId }).IsUnique()
                    .HasFilter("[ExternalEventId] IS NOT NULL");
                entity.HasIndex(w => w.ProcessingStatus);
                entity.HasIndex(w => w.CreatedAt);
            });

            // ===== Seed Subscription Plans =====
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Starter Plan", RazorpayPlanIdMonthly = "plan_starter_monthly", RazorpayPlanIdYearly = "plan_starter_yearly", MonthlyPrice = 499.00m, YearlyPrice = 4999.00m, MaxBranches = 1, MaxStaff = 2, IsActive = true },
                new SubscriptionPlan { Id = 2, Name = "Growth Plan", RazorpayPlanIdMonthly = "plan_growth_monthly", RazorpayPlanIdYearly = "plan_growth_yearly", MonthlyPrice = 1499.00m, YearlyPrice = 14990.00m, MaxBranches = 5, MaxStaff = 10, IsActive = true },
                new SubscriptionPlan { Id = 3, Name = "Enterprise Plan", RazorpayPlanIdMonthly = "plan_enterprise_monthly", RazorpayPlanIdYearly = "plan_enterprise_yearly", MonthlyPrice = 4999.00m, YearlyPrice = 49990.00m, MaxBranches = 99, MaxStaff = 999, IsActive = true }
            );

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
