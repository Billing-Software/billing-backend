using System;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<InventoryDto?> GetByIdAsync(int businessId, int id)
        {
            try
            {
                var item = await _inventoryRepository.GetByIdAsync(businessId, id);
                return item == null ? null : MapToDto(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService.GetByIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<InventoryDto>> GetByBusinessIdAsync(int businessId)
        {
            try
            {
                var items = await _inventoryRepository.GetByBusinessIdAsync(businessId);
                return items.Select(MapToDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService.GetByBusinessIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<InventoryDto> AddAsync(int businessId, InventoryDto dto)
        {
            try
            {
                var existing = await _inventoryRepository.GetBySKUAsync(businessId, dto.SKU);
                if (existing != null)
                {
                    throw new InvalidOperationException("An inventory item with this SKU already exists.");
                }
                var item = new InventoryItem
                {
                    BusinessId = businessId,
                    Name = dto.Name,
                    SKU = dto.SKU,
                    Category = dto.Category,
                    CurrentStock = dto.CurrentStock,
                    Unit = dto.Unit,
                    ReorderLevel = dto.ReorderLevel,
                    ImageUrl = dto.ImageUrl
                };

                var added = await _inventoryRepository.AddAsync(item);
                return MapToDto(added);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService.AddAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<InventoryDto> UpdateAsync(int businessId, InventoryDto dto)
        {
            try
            {
                var existing = await _inventoryRepository.GetBySKUAsync(businessId, dto.SKU);
                if (existing != null && existing.Id != dto.Id)
                {
                    throw new InvalidOperationException("An inventory item with this SKU already exists.");
                }
                var item = new InventoryItem
                {
                    Id = dto.Id,
                    BusinessId = businessId,
                    Name = dto.Name,
                    SKU = dto.SKU,
                    Category = dto.Category,
                    CurrentStock = dto.CurrentStock,
                    Unit = dto.Unit,
                    ReorderLevel = dto.ReorderLevel,
                    ImageUrl = dto.ImageUrl
                };

                var updated = await _inventoryRepository.UpdateAsync(item);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService.UpdateAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            try
            {
                return await _inventoryRepository.DeleteAsync(businessId, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService.DeleteAsync Error]: {ex.Message}");
                throw;
            }
        }

        private InventoryDto MapToDto(InventoryItem i)
        {
            return new InventoryDto
            {
                Id = i.Id,
                BusinessId = i.BusinessId,
                Name = i.Name,
                SKU = i.SKU,
                Category = i.Category,
                CurrentStock = i.CurrentStock,
                Unit = i.Unit,
                ReorderLevel = i.ReorderLevel,
                ImageUrl = i.ImageUrl
            };
        }
    }
}
