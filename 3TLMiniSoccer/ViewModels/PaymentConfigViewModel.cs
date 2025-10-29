using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class PaymentConfigViewModel
    {
        public List<PaymentConfig> SEPayConfigs { get; set; } = new List<PaymentConfig>();
        public List<PaymentConfig> SystemConfigs { get; set; } = new List<PaymentConfig>();
        public List<PaymentConfig> AllConfigs { get; set; } = new List<PaymentConfig>();
    }

    public class PaymentConfigFormViewModel
    {
        public PaymentConfig Config { get; set; } = new PaymentConfig();
        public List<string> ConfigTypes { get; set; } = new List<string> { "SEPay", "System", "General" };
    }

    public class SEPayConfigViewModel
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty; // Token để kiểm tra giao dịch
        public string SecretKey { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }

}
