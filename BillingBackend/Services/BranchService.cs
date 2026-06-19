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

        public BranchService(IBranchRepository branchRepository)
        {
            _branchRepository = branchRepository;
        }

        public async Task<BranchDto?> GetByIdAsync(int businessId, int id)
        {
            var branch = await _branchRepository.GetByIdAsync(businessId, id);
            return branch == null ? null : MapToDto(branch);
        }

        public async Task<IEnumerable<BranchDto>> GetByBusinessIdAsync(int businessId)
        {
            var branches = await _branchRepository.GetByBusinessIdAsync(businessId);
            return branches.Select(MapToDto);
        }

        public async Task<BranchDto> AddAsync(int businessId, BranchDto dto)
        {
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

        public async Task<BranchDto> UpdateAsync(int businessId, BranchDto dto)
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

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _branchRepository.DeleteAsync(businessId, id);
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
