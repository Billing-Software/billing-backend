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
            try
            {
                var customer = await _customerRepository.GetByIdAsync(businessId, id);
                return customer == null ? null : MapToDto(customer);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CustomerService.GetByIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<CustomerDto>> GetByBusinessIdAsync(int businessId)
        {
            try
            {
                var customers = await _customerRepository.GetByBusinessIdAsync(businessId);
                return customers.Select(MapToDto);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CustomerService.GetByBusinessIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<CustomerDto> AddAsync(int businessId, CustomerDto dto)
        {
            try
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
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CustomerService.AddAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<CustomerDto> UpdateAsync(int businessId, CustomerDto dto)
        {
            try
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
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CustomerService.UpdateAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            try
            {
                return await _customerRepository.DeleteAsync(businessId, id);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CustomerService.DeleteAsync Error]: {ex.Message}");
                throw;
            }
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
