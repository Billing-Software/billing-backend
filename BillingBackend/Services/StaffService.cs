using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using BillingBackend.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IBranchRepository _branchRepository;

        public StaffService(
            IStaffRepository staffRepository,
            IBusinessRepository businessRepository,
            IBranchRepository branchRepository)
        {
            _staffRepository = staffRepository;
            _businessRepository = businessRepository;
            _branchRepository = branchRepository;
        }

        public async Task<StaffDto?> GetByIdAsync(int businessId, int id)
        {
            var staff = await _staffRepository.GetByIdAsync(businessId, id);
            return staff == null ? null : MapToDto(staff);
        }

        public async Task<IEnumerable<StaffDto>> GetByBusinessIdAsync(int businessId)
        {
            var staffs = await _staffRepository.GetByBusinessIdAsync(businessId);
            return staffs.Select(MapToDto);
        }

        public async Task<StaffDto> AddAsync(int businessId, StaffDto dto)
        {
            // Enforce active subscription plan staff count limits
            var business = await _businessRepository.GetByIdAsync(businessId);
            if (business == null)
                throw new KeyNotFoundException("Business account details not found.");

            var existingStaffs = await _staffRepository.GetByBusinessIdAsync(businessId);
            int currentCount = existingStaffs.Count();

            if (business.AllowedStaff != -1 && currentCount >= business.AllowedStaff)
                throw new InvalidOperationException($"Staff registration limit reached. Your current plan allows up to {business.AllowedStaff} staff profile(s). Please upgrade your subscription plan to register more team members.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new InvalidOperationException("A strong temporary password is required for new staff. Never use default passwords.");

            // Never trust client BusinessId/UserId: server is authoritative.
            // Validate BranchId belongs to this business (IDOR guard).
            int? branchId = null;
            if (dto.BranchId.HasValue)
            {
                var branch = await _branchRepository.GetByIdAsync(businessId, dto.BranchId.Value);
                if (branch == null)
                    throw new InvalidOperationException("Selected branch does not belong to this business.");
                branchId = branch.Id;
            }

            var staff = new StaffMember
            {
                BusinessId = businessId,
                UserId = null, // always created server-side
                Name = dto.Name.Trim(),
                EmpCode = dto.EmpCode.Trim(),
                Contact = string.IsNullOrWhiteSpace(dto.Contact) ? null : dto.Contact.Trim(),
                Role = PasswordPolicy.StaffRoles.Contains(dto.Role?.Trim() ?? "") ? dto.Role!.Trim() : "Staff",
                Status = dto.Status,
                BranchId = branchId
            };

            var added = await _staffRepository.AddAsync(staff, dto.Password);

            // Reload complete staff to retrieve branch details eager loaded
            var reloaded = await _staffRepository.GetByIdAsync(businessId, added.Id);
            return MapToDto(reloaded ?? added);
        }

        public async Task<StaffDto> UpdateAsync(int businessId, StaffDto dto)
        {
            int? branchId = null;
            if (dto.BranchId.HasValue)
            {
                var branch = await _branchRepository.GetByIdAsync(businessId, dto.BranchId.Value);
                if (branch == null)
                    throw new InvalidOperationException("Selected branch does not belong to this business.");
                branchId = branch.Id;
            }

            var staff = new StaffMember
            {
                Id = dto.Id,
                BusinessId = businessId,
                UserId = null, // never trust client UserId; repository keeps existing linkage
                Name = dto.Name.Trim(),
                EmpCode = dto.EmpCode.Trim(),
                Contact = string.IsNullOrWhiteSpace(dto.Contact) ? null : dto.Contact.Trim(),
                Role = PasswordPolicy.StaffRoles.Contains(dto.Role?.Trim() ?? "") ? dto.Role!.Trim() : "Staff",
                Status = dto.Status,
                BranchId = branchId
            };

            // Preserve existing UserId linkage server-side.
            var existing = await _staffRepository.GetByIdAsync(businessId, dto.Id);
            if (existing == null)
                throw new KeyNotFoundException("Staff member not found.");
            staff.UserId = existing.UserId;

            var updated = await _staffRepository.UpdateAsync(staff, dto.Password);

            // Reload complete staff details
            var reloaded = await _staffRepository.GetByIdAsync(businessId, updated.Id);
            return MapToDto(reloaded ?? updated);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _staffRepository.DeleteAsync(businessId, id);
        }

        private StaffDto MapToDto(StaffMember s)
        {
            return new StaffDto
            {
                Id = s.Id,
                BusinessId = s.BusinessId,
                UserId = s.UserId,
                Name = s.Name,
                EmpCode = s.EmpCode,
                Contact = s.Contact,
                Role = s.Role,
                TotalBills = s.TotalBills,
                RevenueGenerated = s.RevenueGenerated,
                Status = s.Status,
                BranchId = s.BranchId,
                BranchName = s.Branch?.Name
            };
        }
    }
}
