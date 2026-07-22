using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IBusinessRepository _businessRepository;

        public BranchService(IBranchRepository branchRepository, IBusinessRepository businessRepository)
        {
            _branchRepository = branchRepository;
            _businessRepository = businessRepository;
        }

        public async Task<BranchDto?> GetByIdAsync(int businessId, int id)
        {
            try
            {
                var branch = await _branchRepository.GetByIdAsync(businessId, id);
                return branch == null ? null : MapToDto(branch);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BranchService.GetByIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<BranchDto>> GetByBusinessIdAsync(int businessId)
        {
            try
            {
                var branches = await _branchRepository.GetByBusinessIdAsync(businessId);
                return branches.Select(MapToDto);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BranchService.GetByBusinessIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<BranchDto> AddAsync(int businessId, BranchDto dto)
        {
            try
            {
                var business = await _businessRepository.GetByIdAsync(businessId);
                if (business == null)
                {
                    throw new KeyNotFoundException("Business entity not found.");
                }

                var existingBranches = await _branchRepository.GetByBusinessIdAsync(businessId);
                int currentCount = existingBranches.Count();

                if (business.AllowedBranches != -1 && currentCount >= business.AllowedBranches)
                {
                    throw new InvalidOperationException($"Branch limit reached. Your current plan allows up to {business.AllowedBranches} branch(es). Please upgrade your plan to add more branches.");
                }

                var branch = new Branch
                {
                    BusinessId = businessId,
                    Name = dto.Name,
                    Address = dto.Address,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive
                };

                var added = await _branchRepository.AddAsync(branch);
                return MapToDto(added);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BranchService.AddAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<BranchDto> UpdateAsync(int businessId, BranchDto dto)
        {
            try
            {
                var branch = new Branch
                {
                    Id = dto.Id,
                    BusinessId = businessId,
                    Name = dto.Name,
                    Address = dto.Address,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive
                };

                var updated = await _branchRepository.UpdateAsync(branch);
                return MapToDto(updated);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BranchService.UpdateAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            try
            {
                return await _branchRepository.DeleteAsync(businessId, id);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BranchService.DeleteAsync Error]: {ex.Message}");
                throw;
            }
        }

        private BranchDto MapToDto(Branch b)
        {
            return new BranchDto
            {
                Id = b.Id,
                BusinessId = b.BusinessId,
                Name = b.Name,
                Address = b.Address,
                City = b.City,
                PostalCode = b.PostalCode,
                Phone = b.Phone,
                IsActive = b.IsActive
            };
        }
    }
}
