using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BillingDbContext _context;

        public UserRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<UserRegisterResultDto?> RegisterUserAndBusinessAsync(RegisterDto registerDto, byte[] passwordHash, byte[] passwordSalt)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_RegisterUserAndBusiness";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Username", registerDto.Username));
                    command.Parameters.Add(new SqlParameter("@Email", registerDto.Email));
                    command.Parameters.Add(new SqlParameter("@PasswordHash", passwordHash));
                    command.Parameters.Add(new SqlParameter("@PasswordSalt", passwordSalt));
                    command.Parameters.Add(new SqlParameter("@Role", registerDto.Role));
                    command.Parameters.Add(new SqlParameter("@LegalName", registerDto.LegalName));
                    command.Parameters.Add(new SqlParameter("@TradingName", registerDto.TradingName ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Address", registerDto.BusinessAddress ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@City", registerDto.BusinessCity ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@State", registerDto.BusinessState ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@PostalCode", registerDto.BusinessPostalCode ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Country", registerDto.BusinessCountry ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Phone", registerDto.BusinessPhone ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@GstIn", registerDto.GstIn ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@LogoUrl", registerDto.LogoUrl ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Website", registerDto.Website ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@BusinessEmail", registerDto.BusinessEmail ?? (object)System.DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@DefaultTaxRate", registerDto.DefaultTaxRate));
                    command.Parameters.Add(new SqlParameter("@PricesIncludeTax", registerDto.PricesIncludeTax));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserRegisterResultDto
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                Username = Convert.ToString(reader["Username"]) ?? string.Empty,
                                Email = Convert.ToString(reader["Email"]) ?? string.Empty,
                                Role = Convert.ToString(reader["Role"]) ?? string.Empty,
                                BusinessId = Convert.ToInt32(reader["BusinessId"]),
                                BusinessName = Convert.ToString(reader["BusinessName"]) ?? string.Empty,
                                DefaultBranchId = Convert.ToInt32(reader["DefaultBranchId"])
                            };
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }

            return null;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
