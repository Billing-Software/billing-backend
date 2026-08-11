using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class BusinessConfigurationService : IBusinessConfigurationService
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<BusinessConfigurationService> _logger;

        public BusinessConfigurationService(BillingDbContext context, ILogger<BusinessConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private BusinessTypePresetDto MapEntityToDto(BusinessTypeMaster entity)
        {
            var aliases = new List<string>();
            var features = new Dictionary<string, bool>();
            var terminology = new Dictionary<string, TerminologyPairDto>();

            try
            {
                if (!string.IsNullOrEmpty(entity.AliasesJson))
                {
                    aliases = JsonSerializer.Deserialize<List<string>>(entity.AliasesJson) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing AliasesJson for BusinessTypeMaster ID {Id}", entity.Id);
            }

            try
            {
                if (!string.IsNullOrEmpty(entity.DefaultFeaturesJson))
                {
                    features = JsonSerializer.Deserialize<Dictionary<string, bool>>(entity.DefaultFeaturesJson) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing DefaultFeaturesJson for BusinessTypeMaster ID {Id}", entity.Id);
            }

            try
            {
                if (!string.IsNullOrEmpty(entity.DefaultTerminologyJson))
                {
                    terminology = JsonSerializer.Deserialize<Dictionary<string, TerminologyPairDto>>(entity.DefaultTerminologyJson) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing DefaultTerminologyJson for BusinessTypeMaster ID {Id}", entity.Id);
            }

            return new BusinessTypePresetDto
            {
                Id = entity.Code,
                Name = entity.Name,
                Category = entity.Category,
                IconName = entity.IconName,
                SellingModel = entity.SellingModel,
                Aliases = aliases,
                DefaultFeatures = features,
                DefaultTerminology = terminology
            };
        }

        public async Task<List<BusinessTypePresetDto>> GetBusinessTypePresetsAsync()
        {
            var entities = await _context.BusinessTypeMasters
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Category)
                .ThenBy(b => b.Name)
                .ToListAsync();

            if (!entities.Any())
            {
                _logger.LogWarning("No BusinessTypeMasters records found in DB. Returning fallback General Retail preset.");
                return new List<BusinessTypePresetDto> { GetFallbackPreset() };
            }

            return entities.Select(MapEntityToDto).ToList();
        }

        public async Task<BusinessTypePresetDto> GetPresetByTypeNameAsync(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                typeName = "General Retail Store";
            }

            var normalized = typeName.Trim().ToLowerInvariant();

            // 1. Query database dynamically by Name, Code, or matching AliasesJson substring
            var entities = await _context.BusinessTypeMasters
                .AsNoTracking()
                .Where(b => b.IsActive)
                .ToListAsync();

            var matched = entities.FirstOrDefault(b =>
                b.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                b.Code.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(b.AliasesJson) && b.AliasesJson.ToLowerInvariant().Contains(normalized)));

            if (matched != null)
            {
                return MapEntityToDto(matched);
            }

            // 2. Exact & Keyword-based clean terminology presets

            // Restaurant, Tiffin Center, Bakery, Cafe, Mess, Food
            if (normalized.Contains("restaurant") || normalized.Contains("tiffin") || normalized.Contains("mess") || normalized.Contains("bakery") || normalized.Contains("cafe") || normalized.Contains("food") || normalized.Contains("confectionery"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "restaurant_food",
                    Name = typeName,
                    Category = "Food & Beverage",
                    IconName = "Utensils",
                    SellingModel = "GOODS_AND_SERVICES",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = false,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Item", Plural = "Items" },
                        ["service"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                        ["customer"] = new TerminologyPairDto { Singular = "Customer", Plural = "Customers" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Bill", Plural = "Bills" },
                        ["inventory"] = new TerminologyPairDto { Singular = "Stock", Plural = "Stock" }
                    }
                };
            }

            // Pharmacy, Medical Store, Chemist, Drug, Clinic, Healthcare
            if (normalized.Contains("pharmacy") || normalized.Contains("medical") || normalized.Contains("chemist") || normalized.Contains("clinic") || normalized.Contains("healthcare"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "pharmacy_medical",
                    Name = typeName,
                    Category = "Healthcare",
                    IconName = "Pill",
                    SellingModel = "GOODS_ONLY",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = false,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Medicine", Plural = "Medicines" },
                        ["service"] = new TerminologyPairDto { Singular = "Treatment", Plural = "Treatments" },
                        ["customer"] = new TerminologyPairDto { Singular = "Patient", Plural = "Patients" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Bill", Plural = "Bills" },
                        ["inventory"] = new TerminologyPairDto { Singular = "Stock", Plural = "Stock" }
                    }
                };
            }

            // Salon, Barber, Beauty Parlour, Spa
            if (normalized.Contains("salon") || normalized.Contains("barber") || normalized.Contains("beauty") || normalized.Contains("spa") || normalized.Contains("parlour"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "salon_spa",
                    Name = typeName,
                    Category = "Personal Care",
                    IconName = "Scissors",
                    SellingModel = "SERVICES_ONLY",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = true,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Product", Plural = "Products" },
                        ["service"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                        ["customer"] = new TerminologyPairDto { Singular = "Client", Plural = "Clients" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Bill", Plural = "Bills" },
                        ["staff"] = new TerminologyPairDto { Singular = "Stylist", Plural = "Stylists" },
                        ["appointment"] = new TerminologyPairDto { Singular = "Booking", Plural = "Bookings" }
                    }
                };
            }

            // Clothing, Garments, Apparel, Textile, Boutique, Fashion
            if (normalized.Contains("clothing") || normalized.Contains("garment") || normalized.Contains("apparel") || normalized.Contains("boutique"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "clothing_garments",
                    Name = typeName,
                    Category = "Apparel",
                    IconName = "Shirt",
                    SellingModel = "GOODS_ONLY",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = false,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Garment", Plural = "Garments" },
                        ["service"] = new TerminologyPairDto { Singular = "Alteration", Plural = "Alterations" },
                        ["customer"] = new TerminologyPairDto { Singular = "Customer", Plural = "Customers" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Invoice", Plural = "Invoices" },
                        ["inventory"] = new TerminologyPairDto { Singular = "Stock", Plural = "Stock" }
                    }
                };
            }

            // Tuition, Coaching, Education, School
            if (normalized.Contains("tuition") || normalized.Contains("coaching") || normalized.Contains("education") || normalized.Contains("school") || normalized.Contains("academy"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "tuition_coaching",
                    Name = typeName,
                    Category = "Education",
                    IconName = "GraduationCap",
                    SellingModel = "SERVICES_ONLY",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = false, ["services"] = true, ["inventory"] = false, ["appointments"] = true,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = false, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Course", Plural = "Courses" },
                        ["service"] = new TerminologyPairDto { Singular = "Class", Plural = "Classes" },
                        ["customer"] = new TerminologyPairDto { Singular = "Student", Plural = "Students" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Receipt", Plural = "Receipts" },
                        ["staff"] = new TerminologyPairDto { Singular = "Tutor", Plural = "Tutors" }
                    }
                };
            }

            // Software, IT Services, Consulting, Professional Services
            if (normalized.Contains("consulting") || normalized.Contains("software") || (normalized.Contains("it") && !normalized.Contains("kitchen")) || normalized.Contains("professional"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "software_consulting",
                    Name = typeName,
                    Category = "Services",
                    IconName = "Briefcase",
                    SellingModel = "SERVICES_ONLY",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = false, ["services"] = true, ["inventory"] = false, ["appointments"] = true,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = false, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                        ["service"] = new TerminologyPairDto { Singular = "Project / Service", Plural = "Services" },
                        ["customer"] = new TerminologyPairDto { Singular = "Client", Plural = "Clients" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Invoice", Plural = "Invoices" },
                        ["staff"] = new TerminologyPairDto { Singular = "Consultant", Plural = "Consultants" }
                    }
                };
            }

            // Repair, Maintenance, Garage
            if (normalized.Contains("repair") || normalized.Contains("maintenance") || normalized.Contains("tech") || normalized.Contains("garage"))
            {
                return new BusinessTypePresetDto
                {
                    Id = "services_repair",
                    Name = typeName,
                    Category = "Services & Repair",
                    IconName = "Wrench",
                    SellingModel = "GOODS_AND_SERVICES",
                    DefaultFeatures = new Dictionary<string, bool>
                    {
                        ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = true,
                        ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                    },
                    DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                    {
                        ["product"] = new TerminologyPairDto { Singular = "Part", Plural = "Parts" },
                        ["service"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                        ["customer"] = new TerminologyPairDto { Singular = "Client", Plural = "Clients" },
                        ["invoice"] = new TerminologyPairDto { Singular = "Invoice", Plural = "Invoices" },
                        ["staff"] = new TerminologyPairDto { Singular = "Technician", Plural = "Technicians" }
                    }
                };
            }

            // Grocery, Kirana, Wholesale, Trading, Manufacturing, Electronics, General Retail
            return new BusinessTypePresetDto
            {
                Id = "general_retail",
                Name = typeName,
                Category = "Retail & Trading",
                IconName = "Store",
                SellingModel = "GOODS_AND_SERVICES",
                DefaultFeatures = new Dictionary<string, bool>
                {
                    ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = false,
                    ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                },
                DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                {
                    ["product"] = new TerminologyPairDto { Singular = "Product", Plural = "Products" },
                    ["service"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                    ["customer"] = new TerminologyPairDto { Singular = "Customer", Plural = "Customers" },
                    ["invoice"] = new TerminologyPairDto { Singular = "Invoice", Plural = "Invoices" },
                    ["inventory"] = new TerminologyPairDto { Singular = "Stock", Plural = "Inventory" }
                }
            };
        }

        public async Task<BusinessConfigDto> GetConfigurationAsync(int businessId)
        {
            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                throw new InvalidOperationException($"Business with ID {businessId} not found.");
            }

            var preset = await GetPresetByTypeNameAsync(business.BusinessType ?? "General Retail Store");

            // Merge preset defaults with custom overrides if saved
            var features = new Dictionary<string, bool>(preset.DefaultFeatures);
            var terminology = new Dictionary<string, TerminologyPairDto>(preset.DefaultTerminology);

            if (!string.IsNullOrEmpty(business.CustomTerminologyJson))
            {
                try
                {
                    var customTerm = JsonSerializer.Deserialize<Dictionary<string, TerminologyPairDto>>(business.CustomTerminologyJson);
                    if (customTerm != null)
                    {
                        foreach (var kv in customTerm)
                        {
                            terminology[kv.Key] = kv.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse CustomTerminologyJson for business {BusinessId}", businessId);
                }
            }

            // Calculate Setup Progress
            var hasProductsOrServices = (await _context.InventoryItems.AnyAsync(i => i.BusinessId == businessId)) ||
                                        (await _context.Services.AnyAsync(s => s.BusinessId == businessId));
            var hasCustomers = await _context.Customers.AnyAsync(c => c.BusinessId == businessId);
            var hasBills = await _context.Bills.AnyAsync(b => b.BusinessId == businessId);

            var completedSteps = new List<string> { "Business Registration", "Business Type Selection" };
            var pendingSteps = new List<string>();

            if (hasProductsOrServices) completedSteps.Add("Add First Product / Service");
            else pendingSteps.Add("Add First Product / Service");

            if (hasCustomers) completedSteps.Add("Add Customer");
            else pendingSteps.Add("Add Customer");

            if (hasBills) completedSteps.Add("Create First Bill");
            else pendingSteps.Add("Create First Bill");

            int totalSteps = completedSteps.Count + pendingSteps.Count;
            int progress = (int)Math.Round((double)completedSteps.Count / totalSteps * 100);

            return new BusinessConfigDto
            {
                BusinessId = business.Id,
                BusinessName = business.LegalName,
                BusinessType = business.BusinessType ?? preset.Name,
                Category = preset.Category,
                SellingModel = business.SellingModel ?? preset.SellingModel,
                GstScheme = business.GstScheme ?? "Regular",
                GstIn = business.GstIn,
                RegisteredState = business.RegisteredState ?? business.State,
                Features = features,
                Terminology = terminology,
                OnboardingProgressPercentage = progress,
                CompletedSetupSteps = completedSteps,
                PendingSetupSteps = pendingSteps
            };
        }

        public async Task<BusinessConfigDto> UpdateConfigurationAsync(int businessId, UpdateBusinessConfigDto dto)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
            if (business == null)
            {
                throw new InvalidOperationException($"Business with ID {businessId} not found.");
            }

            if (!string.IsNullOrEmpty(dto.BusinessType))
            {
                business.BusinessType = dto.BusinessType;
                var preset = await GetPresetByTypeNameAsync(dto.BusinessType);
                business.SellingModel = dto.SellingModel ?? preset.SellingModel;
                if (dto.Terminology == null)
                {
                    business.CustomTerminologyJson = null;
                }
            }

            if (!string.IsNullOrEmpty(dto.SellingModel)) business.SellingModel = dto.SellingModel;
            if (!string.IsNullOrEmpty(dto.GstScheme)) business.GstScheme = dto.GstScheme;
            if (dto.GstIn != null) business.GstIn = dto.GstIn;
            if (dto.RegisteredState != null) business.RegisteredState = dto.RegisteredState;

            if (dto.Terminology != null)
            {
                business.CustomTerminologyJson = JsonSerializer.Serialize(dto.Terminology);
            }

            business.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetConfigurationAsync(businessId);
        }

        private static BusinessTypePresetDto GetFallbackPreset()
        {
            return new BusinessTypePresetDto
            {
                Id = "general_retail",
                Name = "General Retail Store",
                Category = "Retail",
                IconName = "Store",
                SellingModel = "GOODS_AND_SERVICES",
                Aliases = new List<string> { "general", "other", "retail", "shop" },
                DefaultFeatures = new Dictionary<string, bool>
                {
                    ["products"] = true, ["services"] = true, ["inventory"] = true, ["appointments"] = false,
                    ["customers"] = true, ["staff"] = true, ["khata"] = true, ["purchases"] = true, ["expenses"] = true
                },
                DefaultTerminology = new Dictionary<string, TerminologyPairDto>
                {
                    ["product"] = new TerminologyPairDto { Singular = "Product", Plural = "Products" },
                    ["service"] = new TerminologyPairDto { Singular = "Service", Plural = "Services" },
                    ["customer"] = new TerminologyPairDto { Singular = "Customer", Plural = "Customers" },
                    ["invoice"] = new TerminologyPairDto { Singular = "Bill / Invoice", Plural = "Bills & Invoices" }
                }
            };
        }
    }
}
