using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.Data.Entities
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Starter, Growth, Enterprise

        [MaxLength(200)]
        public string? Subtitle { get; set; } // Short tag line for the plan

        [Required]
        [MaxLength(100)]
        public string RazorpayPlanIdMonthly { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string RazorpayPlanIdYearly { get; set; } = string.Empty;

        public decimal MonthlyPrice { get; set; }

        public decimal YearlyPrice { get; set; }

        public int MaxBranches { get; set; } // 1 for Starter, 5 for Pro, -1 for Enterprise

        public int MaxStaff { get; set; } // Max staff members allowed

        public bool IsPopular { get; set; } = false;

        public int DisplayOrder { get; set; } = 1;

        /// <summary>
        /// JSON array storing plan feature items: [{"text":"1 Branch Sync","included":true}]
        /// </summary>
        public string? FeaturesJson { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
