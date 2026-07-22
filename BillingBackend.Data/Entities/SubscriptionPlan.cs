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
        public string Name { get; set; } = string.Empty; // Starter, Professional, Enterprise

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

        public bool IsActive { get; set; } = true;
    }
}
