using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly BillingDbContext _context;

        public SettingsRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<(WhatsAppSettings? Settings, IEnumerable<WhatsAppTemplate> Templates)> GetWhatsAppSettingsAsync(int businessId)
        {
            WhatsAppSettings? settings = null;
            var templates = new List<WhatsAppTemplate>();
            
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_GetWhatsAppSettings";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    var pBusinessId = command.CreateParameter();
                    pBusinessId.ParameterName = "@BusinessId";
                    pBusinessId.Value = businessId;
                    command.Parameters.Add(pBusinessId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // 1. Settings
                        if (await reader.ReadAsync())
                        {
                            settings = new WhatsAppSettings
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                BusinessId = Convert.ToInt32(reader["BusinessId"]),
                                ApiKey = reader["ApiKey"] == DBNull.Value ? null : Convert.ToString(reader["ApiKey"]),
                                IsConnected = Convert.ToBoolean(reader["IsConnected"]),
                                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["UpdatedAt"])
                            };
                        }

                        // 2. Templates
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                templates.Add(new WhatsAppTemplate
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    WhatsAppSettingsId = Convert.ToInt32(reader["WhatsAppSettingsId"]),
                                    TemplateName = Convert.ToString(reader["TemplateName"]) ?? string.Empty
                                });
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }

            return (settings, templates);
        }

        public async Task<WhatsAppSettings> UpdateWhatsAppSettingsAsync(int businessId, string? apiKey, bool isConnected)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pApiKey = new SqlParameter("@ApiKey", apiKey ?? (object)System.DBNull.Value);
            var pIsConnected = new SqlParameter("@IsConnected", isConnected);

            var results = await _context.WhatsAppSettings
                .FromSqlRaw("EXEC dbo.sp_UpdateWhatsAppSettings @BusinessId, @ApiKey, @IsConnected",
                    pBusinessId, pApiKey, pIsConnected)
                .ToListAsync();

            return results.First();
        }

        public async Task<WhatsAppTemplate> AddWhatsAppTemplateAsync(int businessId, string templateName)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pTemplateName = new SqlParameter("@TemplateName", templateName);

            var results = await _context.WhatsAppTemplates
                .FromSqlRaw("EXEC dbo.sp_AddWhatsAppTemplate @BusinessId, @TemplateName",
                    pBusinessId, pTemplateName)
                .ToListAsync();

            return results.First();
        }

        public async Task<bool> DeleteWhatsAppTemplateAsync(int businessId, int templateId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pTemplateId = new SqlParameter("@TemplateId", templateId);
            
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteWhatsAppTemplate @BusinessId, @TemplateId", pBusinessId, pTemplateId);
            return result > 0;
        }
    }
}
