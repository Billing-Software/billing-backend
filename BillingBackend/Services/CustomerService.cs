using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<CustomerDto?> GetByIdAsync(int businessId, int id)
        {
            var customer = await _customerRepository.GetByIdAsync(businessId, id);
            return customer == null ? null : MapToDto(customer);
        }

        public async Task<IEnumerable<CustomerDto>> GetByBusinessIdAsync(int businessId)
        {
            var customers = await _customerRepository.GetByBusinessIdAsync(businessId);
            return customers.Select(MapToDto);
        }

        public async Task<CustomerDto> AddAsync(int businessId, CustomerDto dto)
        {
            if (dto.IsWalkIn)
            {
                var existing = await _customerRepository.GetByBusinessIdAsync(businessId);
                var walkIn = existing.FirstOrDefault(c => c.IsWalkIn);
                if (walkIn != null)
                {
                    return MapToDto(walkIn);
                }
            }

            var customer = new Customer
            {
                BusinessId = businessId,
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                IsWalkIn = dto.IsWalkIn
            };

            var added = await _customerRepository.AddAsync(customer);
            return MapToDto(added);
        }

        public async Task<CustomerDto> UpdateAsync(int businessId, CustomerDto dto)
        {
            var customer = new Customer
            {
                Id = dto.Id,
                BusinessId = businessId,
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                IsWalkIn = dto.IsWalkIn
            };

            var updated = await _customerRepository.UpdateAsync(customer);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _customerRepository.DeleteAsync(businessId, id);
        }

        private CustomerDto MapToDto(Customer c)
        {
            return new CustomerDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                IsWalkIn = c.IsWalkIn
            };
        }
    }
}
