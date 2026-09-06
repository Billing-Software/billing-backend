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
    [Route("api/coupons")]
    public class DiscountCouponsController : BaseApiController
    {
        private readonly BillingDbContext _context;

        public DiscountCouponsController(BillingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiscountCouponDto>>> GetAllCoupons()
        {
            var coupons = await _context.DiscountCoupons
                .Where(c => c.BusinessId == CurrentBusinessId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new DiscountCouponDto
                {
                    Id = c.Id,
                    CouponCode = c.CouponCode,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    MinimumOrderAmount = c.MinimumOrderAmount,
                    MaximumDiscountAmount = c.MaximumDiscountAmount,
                    ValidFrom = c.ValidFrom,
                    ValidUntil = c.ValidUntil,
                    UsageLimit = c.UsageLimit,
                    UsedCount = c.UsedCount,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(coupons);
        }

        [HttpPost]
        public async Task<ActionResult<DiscountCouponDto>> CreateCoupon([FromBody] DiscountCouponDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var code = dto.CouponCode.Trim().ToUpperInvariant();
            var exists = await _context.DiscountCoupons
                .AnyAsync(c => c.BusinessId == CurrentBusinessId && c.CouponCode == code);

            if (exists) return Conflict(new { message = $"Coupon code '{code}' already exists." });

            var coupon = new DiscountCoupon
            {
                BusinessId = CurrentBusinessId,
                CouponCode = code,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinimumOrderAmount = dto.MinimumOrderAmount,
                MaximumDiscountAmount = dto.MaximumDiscountAmount,
                ValidFrom = dto.ValidFrom != default ? dto.ValidFrom : DateTime.UtcNow,
                ValidUntil = dto.ValidUntil != default ? dto.ValidUntil : DateTime.UtcNow.AddMonths(3),
                UsageLimit = dto.UsageLimit,
                UsedCount = 0,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.DiscountCoupons.Add(coupon);
            await _context.SaveChangesAsync();

            dto.Id = coupon.Id;
            return CreatedAtAction(nameof(GetAllCoupons), new { id = coupon.Id }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.DiscountCoupons
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == CurrentBusinessId);

            if (coupon == null) return NotFound();

            _context.DiscountCoupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
