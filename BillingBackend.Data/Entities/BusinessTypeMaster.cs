using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.Data.Entities
{
    public class BusinessTypeMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IconName { get; set; } = "Store";

        [MaxLength(50)]
        public string SellingModel { get; set; } = "GOODS_AND_SERVICES";

        public string AliasesJson { get; set; } = "[]";

        public string DefaultFeaturesJson { get; set; } = "{}";

        public string DefaultTerminologyJson { get; set; } = "{}";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
