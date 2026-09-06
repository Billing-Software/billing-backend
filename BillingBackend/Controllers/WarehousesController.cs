using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingBackend.Controllers
{
    [Route("api/warehouses")]
    public class WarehousesController : BaseApiController
    {
        private readonly BillingDbContext _context;

        public WarehousesController(BillingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.BusinessId == CurrentBusinessId)
                .OrderBy(w => w.Name)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    BranchId = w.BranchId,
                    Name = w.Name,
                    Code = w.Code,
                    Address = w.Address,
                    ContactPerson = w.ContactPerson,
                    Phone = w.Phone,
                    IsPrimary = w.IsPrimary,
                    IsActive = w.IsActive
                })
                .ToListAsync();

            return Ok(warehouses);
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] WarehouseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var code = dto.Code.Trim().ToUpperInvariant();
            var exists = await _context.Warehouses
                .AnyAsync(w => w.BusinessId == CurrentBusinessId && w.Code == code);

            if (exists) return Conflict(new { message = $"Warehouse with code '{code}' already exists." });

            var warehouse = new Warehouse
            {
                BusinessId = CurrentBusinessId,
                BranchId = dto.BranchId,
                Name = dto.Name.Trim(),
                Code = code,
                Address = dto.Address?.Trim(),
                ContactPerson = dto.ContactPerson?.Trim(),
                Phone = dto.Phone?.Trim(),
                IsPrimary = dto.IsPrimary,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            dto.Id = warehouse.Id;
            return CreatedAtAction(nameof(GetWarehouses), new { id = warehouse.Id }, dto);
        }
    }
}
