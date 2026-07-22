using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
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

        public StaffService(IStaffRepository staffRepository, IBusinessRepository businessRepository)
        {
            _staffRepository = staffRepository;
            _businessRepository = businessRepository;
        }

        public async Task<StaffDto?> GetByIdAsync(int businessId, int id)
        {
            try
            {
                var staff = await _staffRepository.GetByIdAsync(businessId, id);
                return staff == null ? null : MapToDto(staff);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffService.GetByIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<StaffDto>> GetByBusinessIdAsync(int businessId)
        {
            try
            {
                var staffs = await _staffRepository.GetByBusinessIdAsync(businessId);
                return staffs.Select(MapToDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffService.GetByBusinessIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<StaffDto> AddAsync(int businessId, StaffDto dto)
        {
            try
            {
                // Enforce active subscription plan staff count limits
                var business = await _businessRepository.GetByIdAsync(businessId);
                if (business == null)
                {
                    throw new KeyNotFoundException("Business account details not found.");
                }

                var existingStaffs = await _staffRepository.GetByBusinessIdAsync(businessId);
                int currentCount = existingStaffs.Count();

                if (business.AllowedStaff != -1 && currentCount >= business.AllowedStaff)
                {
                    throw new InvalidOperationException($"Staff registration limit reached. Your current plan allows up to {business.AllowedStaff} staff profile(s). Please upgrade your subscription plan to register more team members.");
                }

                var staff = new StaffMember
                {
                    BusinessId = businessId,
                    UserId = dto.UserId,
                    Name = dto.Name,
                    EmpCode = dto.EmpCode,
                    Contact = dto.Contact,
                    Role = dto.Role,
                    Status = dto.Status,
                    BranchId = dto.BranchId
                };

                var added = await _staffRepository.AddAsync(staff, dto.Password ?? "123456");
                
                // Reload complete staff to retrieve branch details eager loaded
                var reloaded = await _staffRepository.GetByIdAsync(businessId, added.Id);
                return MapToDto(reloaded ?? added);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffService.AddAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<StaffDto> UpdateAsync(int businessId, StaffDto dto)
        {
            try
            {
                var staff = new StaffMember
                {
                    Id = dto.Id,
                    BusinessId = businessId,
                    UserId = dto.UserId,
                    Name = dto.Name,
                    EmpCode = dto.EmpCode,
                    Contact = dto.Contact,
                    Role = dto.Role,
                    Status = dto.Status,
                    BranchId = dto.BranchId
                };

                var updated = await _staffRepository.UpdateAsync(staff, dto.Password);
                
                // Reload complete staff details
                var reloaded = await _staffRepository.GetByIdAsync(businessId, updated.Id);
                return MapToDto(reloaded ?? updated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffService.UpdateAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            try
            {
                return await _staffRepository.DeleteAsync(businessId, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffService.DeleteAsync Error]: {ex.Message}");
                throw;
            }
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
