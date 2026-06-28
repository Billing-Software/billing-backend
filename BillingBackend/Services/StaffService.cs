using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;

        public StaffService(IStaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
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
            var staff = new StaffMember
            {
                BusinessId = businessId,
                UserId = dto.UserId,
                Name = dto.Name,
                EmpCode = dto.EmpCode,
                Contact = dto.Contact,
                Role = dto.Role,
                Status = dto.Status
            };

            var added = await _staffRepository.AddAsync(staff, dto.Password ?? "123456");
            return MapToDto(added);
        }

        public async Task<StaffDto> UpdateAsync(int businessId, StaffDto dto)
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
                Status = dto.Status
            };

            var updated = await _staffRepository.UpdateAsync(staff, dto.Password);
            return MapToDto(updated);
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
                Status = s.Status
            };
        }
    }
}
