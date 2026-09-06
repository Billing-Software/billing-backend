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
    [Route("api/customers/{customerId}/ledger")]
    public class CustomerLedgerController : BaseApiController
    {
        private readonly BillingDbContext _context;

        public CustomerLedgerController(BillingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerLedgerDto>>> GetLedgerEntries(int customerId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == CurrentBusinessId);

            if (customer == null) return NotFound(new { message = "Customer not found." });

            var entries = await _context.CustomerLedgers
                .Where(cl => cl.CustomerId == customerId && cl.BusinessId == CurrentBusinessId)
                .OrderByDescending(cl => cl.TransactionDate)
                .Select(cl => new CustomerLedgerDto
                {
                    Id = cl.Id,
                    CustomerId = cl.CustomerId,
                    CustomerName = cl.Customer.Name,
                    BillId = cl.BillId,
                    BillNumber = cl.Bill != null ? cl.Bill.BillNumber : null,
                    TransactionType = cl.TransactionType,
                    Amount = cl.Amount,
                    RunningBalance = cl.RunningBalance,
                    PaymentMode = cl.PaymentMode,
                    ReferenceNumber = cl.ReferenceNumber,
                    Notes = cl.Notes,
                    RecordedByStaffId = cl.RecordedByStaffId,
                    RecordedByStaffName = cl.RecordedByStaff != null ? cl.RecordedByStaff.Name : null,
                    TransactionDate = cl.TransactionDate,
                    CreatedAt = cl.CreatedAt
                })
                .ToListAsync();

            return Ok(entries);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerLedgerDto>> AddLedgerEntry(int customerId, [FromBody] CreateLedgerEntryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == CurrentBusinessId);

            if (customer == null) return NotFound(new { message = "Customer not found." });

            var latestEntry = await _context.CustomerLedgers
                .Where(cl => cl.CustomerId == customerId && cl.BusinessId == CurrentBusinessId)
                .OrderByDescending(cl => cl.TransactionDate)
                .ThenByDescending(cl => cl.Id)
                .FirstOrDefaultAsync();

            decimal currentBalance = latestEntry?.RunningBalance ?? 0m;
            decimal newBalance = currentBalance;
            if (dto.TransactionType == "Credit")
            {
                newBalance -= dto.Amount;
            }
            else if (dto.TransactionType == "Debit")
            {
                newBalance += dto.Amount;
            }

            var entry = new CustomerLedger
            {
                BusinessId = CurrentBusinessId,
                CustomerId = customerId,
                BillId = dto.BillId,
                TransactionType = dto.TransactionType,
                Amount = dto.Amount,
                RunningBalance = newBalance,
                PaymentMode = dto.PaymentMode ?? "Cash",
                ReferenceNumber = dto.ReferenceNumber?.Trim(),
                Notes = dto.Notes?.Trim(),
                RecordedByStaffId = null,
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerLedgers.Add(entry);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLedgerEntries), new { customerId }, new CustomerLedgerDto
            {
                Id = entry.Id,
                CustomerId = entry.CustomerId,
                CustomerName = customer.Name,
                BillId = entry.BillId,
                TransactionType = entry.TransactionType,
                Amount = entry.Amount,
                RunningBalance = entry.RunningBalance,
                PaymentMode = entry.PaymentMode,
                ReferenceNumber = entry.ReferenceNumber,
                Notes = entry.Notes,
                TransactionDate = entry.TransactionDate,
                CreatedAt = entry.CreatedAt
            });
        }
    }
}
