using System;

namespace BillingBackend.Data.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'Service', 'Inventory', 'Expense'
        public int? ParentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
