using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.Services
{
    public class PaymentConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly SystemConfigService _systemConfigService;

        public PaymentConfigService(ApplicationDbContext context, SystemConfigService systemConfigService)
        {
            _context = context;
            _systemConfigService = systemConfigService;
        }

        public async Task<List<PaymentConfig>> GetAllConfigsAsync()
        {
            return await _context.PaymentConfigs
                .OrderBy(c => c.ConfigType)
                .ThenBy(c => c.ConfigKey)
                .ToListAsync();
        }

        public async Task<PaymentConfig?> GetConfigByKeyAsync(string key)
        {
            return await _context.PaymentConfigs
                .FirstOrDefaultAsync(c => c.ConfigKey == key);
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            var config = await _context.PaymentConfigs
                .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive);
            
            return config?.ConfigValue;
        }

        public async Task<bool> DatGiaTriCauHinhAsync(string key, string value, string? description = null, string configType = "General")
        {
            try
            {
                var existingConfig = await _context.PaymentConfigs
                    .FirstOrDefaultAsync(c => c.ConfigKey == key);

                if (existingConfig != null)
                {
                    existingConfig.ConfigValue = value;
                    existingConfig.Description = description ?? existingConfig.Description;
                    existingConfig.ConfigType = configType;
                    existingConfig.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var newConfig = new PaymentConfig
                    {
                        ConfigKey = key,
                        ConfigValue = value,
                        Description = description,
                        ConfigType = configType,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.PaymentConfigs.Add(newConfig);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateConfigAsync(PaymentConfig config)
        {
            try
            {
                config.UpdatedAt = DateTime.Now;
                _context.PaymentConfigs.Update(config);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteConfigAsync(int configId)
        {
            try
            {
                var config = await _context.PaymentConfigs.FindAsync(configId);
                if (config != null)
                {
                    _context.PaymentConfigs.Remove(config);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetSEPayConfigAsync()
        {
            // Sử dụng SystemConfigs thay vì PaymentConfigs cho SEPay
            var configs = await _context.SystemConfigs
                .Where(c => c.ConfigType == "SEPay" && c.IsActive)
                .ToListAsync();

            return configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        }

        public async Task<bool> UpdateSEPayConfigAsync(Dictionary<string, string> config)
        {
            try
            {
                foreach (var kvp in config)
                {
                    await _systemConfigService.DatGiaTriCauHinhAsync(kvp.Key, kvp.Value, null, "SEPay");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetVietQRConfigAsync()
        {
            var configs = await _context.PaymentConfigs
                .Where(c => c.ConfigType == "VietQR" && c.IsActive)
                .ToListAsync();

            return configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        }

        public async Task<bool> UpdateVietQRConfigAsync(Dictionary<string, string> config)
        {
            try
            {
                foreach (var kvp in config)
                {
                    await DatGiaTriCauHinhAsync(kvp.Key, kvp.Value, null, "VietQR");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetSystemConfigAsync()
        {
            var configs = await _context.PaymentConfigs
                .Where(c => c.ConfigType == "System" && c.IsActive)
                .ToListAsync();

            return configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        }

        public async Task<bool> UpdateSystemConfigAsync(Dictionary<string, string> config)
        {
            try
            {
                foreach (var kvp in config)
                {
                    await DatGiaTriCauHinhAsync(kvp.Key, kvp.Value, null, "System");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsConfigActiveAsync(string key)
        {
            var config = await _context.PaymentConfigs
                .FirstOrDefaultAsync(c => c.ConfigKey == key);
            
            return config?.IsActive ?? false;
        }

        public async Task<List<PaymentConfig>> GetConfigsByTypeAsync(string configType)
        {
            return await _context.PaymentConfigs
                .Where(c => c.ConfigType == configType)
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();
        }

        public async Task<bool> BulkUpdateConfigsAsync(List<PaymentConfig> configs)
        {
            try
            {
                foreach (var config in configs)
                {
                    config.UpdatedAt = DateTime.Now;
                }
                
                _context.PaymentConfigs.UpdateRange(configs);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
