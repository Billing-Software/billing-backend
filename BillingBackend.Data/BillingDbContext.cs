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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Users =====
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
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
                    .HasFilter("\"IdempotencyKey\" IS NOT NULL");
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
            });

            // ===== AuditLogs =====
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
                entity.HasIndex(a => a.BusinessId);
                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => a.CorrelationId);
            });

            // ===== WebhookEventLogs =====
            modelBuilder.Entity<WebhookEventLog>(entity =>
            {
                entity.HasIndex(w => new { w.Source, w.ExternalEventId }).IsUnique()
                    .HasFilter("\"ExternalEventId\" IS NOT NULL");
                entity.HasIndex(w => w.ProcessingStatus);
                entity.HasIndex(w => w.CreatedAt);
            });
        }
    }
}
