using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.Services
{
    public class SystemConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemConfigService> _logger;

        public SystemConfigService(ApplicationDbContext context, ILogger<SystemConfigService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SystemConfig>> LayTatCaCauHinhAsync()
        {
            return await _context.SystemConfigs
                .OrderBy(c => c.ConfigType)
                .ThenBy(c => c.ConfigKey)
                .ToListAsync();
        }

        public async Task<List<SystemConfig>> LayCauHinhTheoLoaiAsync(string configType)
        {
            return await _context.SystemConfigs
                .Where(c => c.ConfigType == configType)
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();
        }

        public async Task<SystemConfig?> LayCauHinhTheoKhoaAsync(string configKey)
        {
            return await _context.SystemConfigs
                .FirstOrDefaultAsync(c => c.ConfigKey == configKey);
        }

        public async Task<SystemConfig?> LayCauHinhTheoIdAsync(int configId)
        {
            return await _context.SystemConfigs
                .FirstOrDefaultAsync(c => c.ConfigId == configId);
        }

        public async Task<string> LayGiaTriCauHinhAsync(string configKey, string defaultValue = "")
        {
            var config = await LayCauHinhTheoKhoaAsync(configKey);
            return config?.ConfigValue ?? defaultValue;
        }

        public async Task<bool> DatGiaTriCauHinhAsync(string configKey, string configValue, string? description = null, string configType = "General", string dataType = "String")
        {
            try
            {
                var existingConfig = await LayCauHinhTheoKhoaAsync(configKey);
                
                if (existingConfig != null)
                {
                    existingConfig.ConfigValue = configValue;
                    existingConfig.Description = description ?? existingConfig.Description;
                    existingConfig.ConfigType = configType;
                    existingConfig.DataType = dataType;
                    existingConfig.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var newConfig = new SystemConfig
                    {
                        ConfigKey = configKey,
                        ConfigValue = configValue,
                        Description = description,
                        ConfigType = configType,
                        DataType = dataType,
                        IsActive = true,
                        IsEncrypted = false,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    _context.SystemConfigs.Add(newConfig);
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CapNhatCauHinhAsync(SystemConfig config)
        {
            try
            {
                config.UpdatedAt = DateTime.Now;
                _context.SystemConfigs.Update(config);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaCauHinhAsync(int configId)
        {
            try
            {
                var config = await _context.SystemConfigs.FindAsync(configId);
                if (config != null)
                {
                    _context.SystemConfigs.Remove(config);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> KiemTraCauHinhTonTaiAsync(string configKey)
        {
            return await _context.SystemConfigs
                .AnyAsync(c => c.ConfigKey == configKey);
        }

        public async Task<bool> CapNhatNhieuCauHinhAsync(List<SystemConfig> configs)
        {
            try
            {
                foreach (var config in configs)
                {
                    _context.SystemConfigs.Update(config);
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multiple configs");
                return false;
            }
        }

        // Additional methods for AdminController
        public async Task<List<SystemConfig>> GetConfigsByTypeAsync(string configType)
        {
            return await LayCauHinhTheoLoaiAsync(configType);
        }

        public async Task<List<SystemConfig>> GetAllConfigsAsync()
        {
            return await LayTatCaCauHinhAsync();
        }

        public async Task<bool> SetConfigValueAsync(string configKey, string configValue, string? description = null, string configType = "General")
        {
            return await DatGiaTriCauHinhAsync(configKey, configValue, description, configType);
        }

        public async Task<bool> DeleteConfigAsync(int configId)
        {
            return await XoaCauHinhAsync(configId);
        }

        public async Task<SystemConfig?> GetConfigByIdAsync(int configId)
        {
            return await LayCauHinhTheoIdAsync(configId);
        }

        public async Task<SystemConfig?> GetConfigByKeyAsync(string configKey)
        {
            return await LayCauHinhTheoKhoaAsync(configKey);
        }

        public async Task<bool> UpdateConfigsAsync(List<SystemConfig> configs)
        {
            return await CapNhatNhieuCauHinhAsync(configs);
        }

        public async Task<bool> UpdateConfigAsync(SystemConfig config)
        {
            return await CapNhatCauHinhAsync(config);
        }
    }
}
