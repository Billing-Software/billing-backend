using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly BillingDbContext _context;

        public BillRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Bill?> GetByIdAsync(int businessId, int id)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_GetBillById";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    var pBusinessId = command.CreateParameter();
                    pBusinessId.ParameterName = "@BusinessId";
                    pBusinessId.Value = businessId;
                    command.Parameters.Add(pBusinessId);

                    var pId = command.CreateParameter();
                    pId.ParameterName = "@Id";
                    pId.Value = id;
                    command.Parameters.Add(pId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return null;

                        var bill = new Bill
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            BusinessId = Convert.ToInt32(reader["BusinessId"]),
                            BranchId = Convert.ToInt32(reader["BranchId"]),
                            CustomerId = Convert.ToInt32(reader["CustomerId"]),
                            CreatedByStaffId = reader["CreatedByStaffId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CreatedByStaffId"]),
                            BillNumber = Convert.ToString(reader["BillNumber"]) ?? string.Empty,
                            Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                            DiscountCode = reader["DiscountCode"] == DBNull.Value ? null : Convert.ToString(reader["DiscountCode"]),
                            DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                            TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            PaymentMethod = Convert.ToString(reader["PaymentMethod"]) ?? "Cash",
                            Status = Convert.ToString(reader["Status"]) ?? "Pending",
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };

                        if (await reader.NextResultAsync())
                        {
                            var items = new List<BillItem>();
                            while (await reader.ReadAsync())
                            {
                                items.Add(new BillItem
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    BillId = Convert.ToInt32(reader["BillId"]),
                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                    ServiceName = Convert.ToString(reader["ServiceName"]) ?? string.Empty,
                                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    LineTotal = Convert.ToDecimal(reader["LineTotal"])
                                });
                            }
                            bill.Items = items;
                        }

                        return bill;
                    }
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
        }

        public async Task<IEnumerable<Bill>> GetByBusinessIdAsync(int businessId)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_GetBillsByBusinessId";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    var pBusinessId = command.CreateParameter();
                    pBusinessId.ParameterName = "@BusinessId";
                    pBusinessId.Value = businessId;
                    command.Parameters.Add(pBusinessId);

                    var bills = new List<Bill>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var bill = new Bill
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                BusinessId = Convert.ToInt32(reader["BusinessId"]),
                                BranchId = Convert.ToInt32(reader["BranchId"]),
                                CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                CreatedByStaffId = reader["CreatedByStaffId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CreatedByStaffId"]),
                                BillNumber = Convert.ToString(reader["BillNumber"]) ?? string.Empty,
                                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                                DiscountCode = reader["DiscountCode"] == DBNull.Value ? null : Convert.ToString(reader["DiscountCode"]),
                                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                PaymentMethod = Convert.ToString(reader["PaymentMethod"]) ?? "Cash",
                                Status = Convert.ToString(reader["Status"]) ?? "Pending",
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            };
                            bills.Add(bill);
                        }
                    }
                    return bills;
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
        }

        public async Task<Bill> AddAsync(Bill bill, string itemsJson)
        {
            var pBusinessId = new SqlParameter("@BusinessId", bill.BusinessId);
            var pBranchId = new SqlParameter("@BranchId", bill.BranchId);
            var pCustomerId = new SqlParameter("@CustomerId", bill.CustomerId);
            var pCreatedByStaffId = new SqlParameter("@CreatedByStaffId", bill.CreatedByStaffId ?? (object)System.DBNull.Value);
            var pBillNumber = new SqlParameter("@BillNumber", bill.BillNumber);
            var pSubtotal = new SqlParameter("@Subtotal", bill.Subtotal);
            var pDiscountCode = new SqlParameter("@DiscountCode", bill.DiscountCode ?? (object)System.DBNull.Value);
            var pDiscountAmount = new SqlParameter("@DiscountAmount", bill.DiscountAmount);
            var pTaxAmount = new SqlParameter("@TaxAmount", bill.TaxAmount);
            var pTotalAmount = new SqlParameter("@TotalAmount", bill.TotalAmount);
            var pPaymentMethod = new SqlParameter("@PaymentMethod", bill.PaymentMethod);
            var pStatus = new SqlParameter("@Status", bill.Status);
            var pItemsJson = new SqlParameter("@ItemsJson", itemsJson);

            var results = await _context.Bills
                .FromSqlRaw("EXEC dbo.sp_CreateBill @BusinessId, @BranchId, @CustomerId, @CreatedByStaffId, @BillNumber, @Subtotal, @DiscountCode, @DiscountAmount, @TaxAmount, @TotalAmount, @PaymentMethod, @Status, @ItemsJson",
                    pBusinessId, pBranchId, pCustomerId, pCreatedByStaffId, pBillNumber, pSubtotal, pDiscountCode, pDiscountAmount, pTaxAmount, pTotalAmount, pPaymentMethod, pStatus, pItemsJson)
                .ToListAsync();

            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteBill @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
