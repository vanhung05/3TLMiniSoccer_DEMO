using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class SystemConfigViewModel
    {
        public List<SystemConfig> AllConfigs { get; set; } = new();
        public List<SystemConfig> SEPayConfigs { get; set; } = new();
        public List<SystemConfig> SystemConfigs { get; set; } = new();
        public List<SystemConfig> GeneralConfigs { get; set; } = new();
        
        // SEPay specific data
        public List<BankInfo> SupportedBanks { get; set; } = new();
        
        // Statistics
        public int TotalConfigs => AllConfigs.Count;
        public int SEPayCount => SEPayConfigs.Count;
        public int SystemCount => SystemConfigs.Count;
        public int GeneralCount => GeneralConfigs.Count;
    }
}
