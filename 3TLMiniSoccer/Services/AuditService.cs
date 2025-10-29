using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using System.Text.Json;

namespace _3TLMiniSoccer.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Audit Logs

        public async Task<List<AuditLog>> LayTatCaNhatKyKiemTraAsync()
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<AuditLog?> LayNhatKyKiemTraTheoIdAsync(int logId)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .FirstOrDefaultAsync(al => al.LogId == logId);
        }

        public async Task<List<AuditLog>> LayNhatKyKiemTraTheoNguoiDungAsync(int userId)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> LayNhatKyKiemTraTheoBangAsync(string tableName)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.TableName == tableName)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> LayNhatKyKiemTraTheoHanhDongAsync(string action)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.Action == action)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> LayNhatKyKiemTraTheoKhoangThoiGianAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<AuditLog> TaoNhatKyKiemTraAsync(AuditLog auditLog)
        {
            auditLog.CreatedAt = DateTime.Now;
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public async Task<bool> GhiNhanHanhDongAsync(int? userId, string action, string tableName, int? recordId, string? oldValues = null, string? newValues = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreatedAt = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> GhiNhanHanhDongNguoiDungAsync(int userId, string action, string tableName, int? recordId, string? oldValues = null, string? newValues = null)
        {
            return await GhiNhanHanhDongAsync(userId, action, tableName, recordId, oldValues, newValues);
        }

        public async Task<bool> GhiNhanHanhDongHeThongAsync(string action, string tableName, int? recordId, string? oldValues = null, string? newValues = null)
        {
            return await GhiNhanHanhDongAsync(null, action, tableName, recordId, oldValues, newValues);
        }

        #endregion

        #region System Settings

        public async Task<List<SystemConfig>> LayTatCaCauHinhHeThongAsync()
        {
            return await _context.SystemConfigs
                .OrderBy(sc => sc.ConfigKey)
                .ToListAsync();
        }

        public async Task<SystemConfig?> LayCauHinhHeThongTheoKhoaAsync(string key)
        {
            return await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.ConfigKey == key);
        }

        public async Task<SystemConfig> TaoCauHinhHeThongAsync(SystemConfig setting)
        {
            setting.UpdatedAt = DateTime.Now;
            _context.SystemConfigs.Add(setting);
            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<bool> CapNhatCauHinhHeThongAsync(SystemConfig setting)
        {
            try
            {
                setting.UpdatedAt = DateTime.Now;
                _context.SystemConfigs.Update(setting);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaCauHinhHeThongAsync(int settingId)
        {
            try
            {
                var setting = await _context.SystemConfigs.FindAsync(settingId);
                if (setting != null)
                {
                    _context.SystemConfigs.Remove(setting);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> LayGiaTriCauHinhAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.ConfigKey == key);
            return setting?.ConfigValue ?? defaultValue;
        }

        public async Task<bool> DatGiaTriCauHinhAsync(string key, string value, int? updatedBy = null)
        {
            try
            {
                var setting = await _context.SystemConfigs
                    .FirstOrDefaultAsync(sc => sc.ConfigKey == key);

                if (setting != null)
                {
                    setting.ConfigValue = value;
                    setting.UpdatedAt = DateTime.Now;
                }
                else
                {
                    setting = new SystemConfig
                    {
                        ConfigKey = key,
                        ConfigValue = value,
                        DataType = "String",
                        UpdatedAt = DateTime.Now
                    };
                    _context.SystemConfigs.Add(setting);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<T> LayGiaTriCauHinhAsync<T>(string key, T defaultValue = default(T))
        {
            var setting = await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.ConfigKey == key);

            if (setting == null)
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<T>(setting.ConfigValue) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<bool> DatGiaTriCauHinhAsync<T>(string key, T value, int? updatedBy = null)
        {
            try
            {
                var jsonValue = JsonSerializer.Serialize(value);
                return await DatGiaTriCauHinhAsync(key, jsonValue, updatedBy);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private string? GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].FirstOrDefault();
        }

        #endregion
    }
}
