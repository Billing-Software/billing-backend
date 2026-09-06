using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class Business
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string LegalName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TradingName { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string Country { get; set; } = "India";

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? GstIn { get; set; }

        [MaxLength(100)]
        public string BusinessType { get; set; } = "General Retail Store";

        [MaxLength(50)]
        public string SellingModel { get; set; } = "GOODS_AND_SERVICES"; // GOODS_ONLY, SERVICES_ONLY, GOODS_AND_SERVICES

        [MaxLength(50)]
        public string GstScheme { get; set; } = "Regular"; // Regular, Composition, None

        [MaxLength(100)]
        public string? RegisteredState { get; set; }

        public string? CustomTerminologyJson { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultTaxRate { get; set; } = 18.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [MaxLength(500)]
        public string? ReceiptHeader { get; set; }

        [MaxLength(500)]
        public string? ReceiptFooter { get; set; }

        public bool ShowLogoOnReceipt { get; set; } = true;

        [MaxLength(50)]
        public string ReceiptTemplateType { get; set; } = "Thermal80mm";
        public bool IsSuspended { get; set; } = false;
        public int ActivePlanId { get; set; } = 1; // Default to Plan 1
        public int AllowedBranches { get; set; } = 1; // Default Starter Plan branch limit
        public int AllowedStaff { get; set; } = 2; // Default Starter Plan staff limit

        [MaxLength(100)]
        public string? RazorpayCustomerId { get; set; }

        [MaxLength(100)]
        public string? RazorpaySubscriptionId { get; set; }

        [MaxLength(50)]
        public string SubscriptionStatus { get; set; } = "Trial"; // Trial, Active, PastDue, Cancelled, TrialExpired

        public bool IsTrial { get; set; } = true;
        public DateTime? TrialStartsAt { get; set; }
        public DateTime? TrialEndsAt { get; set; }

        public DateTime? SubscriptionExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OwnerId))]
        public User Owner { get; set; } = null!;

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public ICollection<StaffMember> StaffMembers { get; set; } = new List<StaffMember>();
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
        public WhatsAppAccount? WhatsAppAccount { get; set; }
        public BusinessPaymentSettings? PaymentSettings { get; set; }
        public BusinessTaxSettings? TaxSettings { get; set; }
        public BusinessInvoiceSettings? InvoiceSettings { get; set; }
        public BusinessInvoiceDesign? InvoiceDesign { get; set; }
        public BusinessAppPreferences? AppPreferences { get; set; }
        public ICollection<BusinessPrinterSettings> PrinterSettings { get; set; } = new List<BusinessPrinterSettings>();
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public ICollection<DiscountCoupon> DiscountCoupons { get; set; } = new List<DiscountCoupon>();
        public ICollection<CustomerLedger> CustomerLedgers { get; set; } = new List<CustomerLedger>();
    }
}
