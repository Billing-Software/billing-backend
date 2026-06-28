using System;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IServiceRepository _serviceRepository;

        public ServiceService(IServiceRepository serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<ServiceDto?> GetByIdAsync(int businessId, int id)
        {
            var service = await _serviceRepository.GetByIdAsync(businessId, id);
            return service == null ? null : MapToDto(service);
        }

        public async Task<IEnumerable<ServiceDto>> GetByBusinessIdAsync(int businessId)
        {
            var services = await _serviceRepository.GetByBusinessIdAsync(businessId);
            return services.Select(MapToDto);
        }

        public async Task<ServiceDto> AddAsync(int businessId, ServiceDto dto)
        {
            var existing = await _serviceRepository.GetBySKUAsync(businessId, dto.SKU);
            if (existing != null)
            {
                throw new InvalidOperationException("A service with this SKU already exists.");
            }
            var service = new Service
            {
                BusinessId = businessId,
                Name = dto.Name,
                SKU = dto.SKU,
                Category = dto.Category,
                BasePrice = dto.BasePrice,
                TaxRate = dto.TaxRate,
                Status = dto.Status,
                IconName = dto.IconName
            };

            var added = await _serviceRepository.AddAsync(service);
            return MapToDto(added);
        }

        public async Task<ServiceDto> UpdateAsync(int businessId, ServiceDto dto)
        {
            var existing = await _serviceRepository.GetBySKUAsync(businessId, dto.SKU);
            if (existing != null && existing.Id != dto.Id)
            {
                throw new InvalidOperationException("A service with this SKU already exists.");
            }
            var service = new Service
            {
                Id = dto.Id,
                BusinessId = businessId,
                Name = dto.Name,
                SKU = dto.SKU,
                Category = dto.Category,
                BasePrice = dto.BasePrice,
                TaxRate = dto.TaxRate,
                Status = dto.Status,
                IconName = dto.IconName
            };

            var updated = await _serviceRepository.UpdateAsync(service);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _serviceRepository.DeleteAsync(businessId, id);
        }

        private ServiceDto MapToDto(Service s)
        {
            return new ServiceDto
            {
                Id = s.Id,
                BusinessId = s.BusinessId,
                Name = s.Name,
                SKU = s.SKU,
                Category = s.Category,
                BasePrice = s.BasePrice,
                TaxRate = s.TaxRate,
                Status = s.Status,
                IconName = s.IconName
            };
        }
    }
}
