using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public PurchaseService(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDto>> GetByBusinessIdAsync(int businessId)
        {
            var list = await _purchaseRepository.GetByBusinessIdAsync(businessId);
            return list.Select(MapToDto);
        }

        public async Task<PurchaseDto?> GetByIdAsync(int businessId, int id)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(businessId, id);
            if (purchase == null) return null;
            return MapToDto(purchase);
        }

        public async Task<PurchaseDto> AddAsync(int businessId, PurchaseDto dto)
        {
            var purchase = new Purchase
            {
                BusinessId = businessId,
                VendorName = dto.VendorName,
                InvoiceNumber = dto.InvoiceNumber,
                Subtotal = dto.Subtotal,
                TaxAmount = dto.TaxAmount,
                TotalAmount = dto.TotalAmount,
                Status = dto.Status,
                PurchaseDate = dto.PurchaseDate == default ? DateTime.UtcNow : dto.PurchaseDate,
                Items = dto.Items.Select(item => new PurchaseItem
                {
                    InventoryItemId = item.InventoryItemId,
                    ItemName = item.ItemName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                }).ToList()
            };

            var added = await _purchaseRepository.AddAsync(purchase);
            return MapToDto(added);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _purchaseRepository.DeleteAsync(businessId, id);
        }

        private PurchaseDto MapToDto(Purchase p)
        {
            return new PurchaseDto
            {
                Id = p.Id,
                BusinessId = p.BusinessId,
                VendorName = p.VendorName,
                InvoiceNumber = p.InvoiceNumber,
                Subtotal = p.Subtotal,
                TaxAmount = p.TaxAmount,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                PurchaseDate = p.PurchaseDate,
                CreatedAt = p.CreatedAt,
                Items = p.Items.Select(item => new PurchaseItemDto
                {
                    Id = item.Id,
                    PurchaseId = item.PurchaseId,
                    InventoryItemId = item.InventoryItemId,
                    ItemName = item.ItemName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                }).ToList()
            };
        }
    }
}
