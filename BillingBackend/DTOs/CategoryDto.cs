using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // 'Service', 'Inventory', 'Expense'

        public DateTime CreatedAt { get; set; }
    }
}
