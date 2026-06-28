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
        public DbSet<WhatsAppSettings> WhatsAppSettings { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<Category> Categories { get; set; }

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

            // ===== WhatsAppSettings =====
            modelBuilder.Entity<WhatsAppSettings>(entity =>
            {
                entity.HasOne(w => w.Business)
                    .WithOne(b => b.WhatsAppSettings)
                    .HasForeignKey<WhatsAppSettings>(w => w.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(w => w.BusinessId).IsUnique();
            });

            // ===== WhatsAppTemplates =====
            modelBuilder.Entity<WhatsAppTemplate>(entity =>
            {
                entity.HasOne(t => t.WhatsAppSettings)
                    .WithMany(w => w.Templates)
                    .HasForeignKey(t => t.WhatsAppSettingsId)
                    .OnDelete(DeleteBehavior.Cascade);
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
        }
    }
}
