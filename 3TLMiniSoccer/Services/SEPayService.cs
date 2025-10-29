using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class SEPayService
    {
        private readonly SystemConfigService _systemConfigService;
        private readonly HttpClient _httpClient;

        public SEPayService(SystemConfigService systemConfigService, HttpClient httpClient)
        {
            _systemConfigService = systemConfigService;
            _httpClient = httpClient;
        }

        public async Task<string> CreatePaymentUrlAsync(decimal amount, string orderInfo, string orderId, string returnUrl)
        {
            // SEPay chỉ sử dụng QR code, không cần payment URL
            // Trả về QR code URL thay thế
            return await CreateQRCodeUrlAsync(amount, orderInfo, orderId);
        }

        public async Task<string> CreateQRCodeUrlAsync(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                var config = await GetSEPayConfigAsync();
                
                // Mã sử dụng API của Sepay để tạo QR Code
                var bankCode = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_BANK_CODE, "BIDV");
                var accountNumber = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_ACCOUNT_NUMBER, "0353425450");
                
                System.Diagnostics.Debug.WriteLine($"SEPay QR Config - BankCode: {bankCode}, AccountNumber: {accountNumber}");
                
                // Tạo URL QR theo format của SEPay API
                var qrUrl = $"https://qr.sepay.vn/img?bank={bankCode}&acc={accountNumber}&template=compact&amount={(int)amount}&des={Uri.EscapeDataString(orderInfo)}";
                
                System.Diagnostics.Debug.WriteLine($"SEPay QR URL generated: {qrUrl}");
                
                return qrUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateQRCodeUrlAsync: {ex.Message}");
                // Fallback to default values
                var qrUrl = $"https://qr.sepay.vn/img?bank=BIDV&acc=0353425450&template=compact&amount={(int)amount}&des={Uri.EscapeDataString(orderInfo)}";
                System.Diagnostics.Debug.WriteLine($"SEPay QR URL fallback: {qrUrl}");
                return qrUrl;
            }
        }

        public async Task<bool> VerifyPaymentAsync(Dictionary<string, string> sepayData)
        {
            try
            {
                var config = await GetSEPayConfigAsync();
                var signature = sepayData.GetValueOrDefault("signature", "");
                
                // Remove signature from data for verification
                var dataForVerification = sepayData.Where(x => x.Key != "signature")
                    .ToDictionary(x => x.Key, x => x.Value);
                
                var calculatedSignature = CreateSignature(dataForVerification);
                
                return signature.Equals(calculatedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public async Task<PaymentResult> ProcessPaymentResponseAsync(Dictionary<string, string> sepayData)
        {
            var result = new PaymentResult();
            
            try
            {
                var isValid = await VerifyPaymentAsync(sepayData);
                if (!isValid)
                {
                    result.Success = false;
                    result.Message = "Invalid payment signature";
                    return result;
                }

                var status = sepayData.GetValueOrDefault("status", "");
                var transactionId = sepayData.GetValueOrDefault("transaction_id", "");
                var orderId = sepayData.GetValueOrDefault("order_id", "");
                var amount = decimal.Parse(sepayData.GetValueOrDefault("amount", "0"));
                var paymentDate = DateTime.Parse(sepayData.GetValueOrDefault("payment_date", DateTime.Now.ToString()));

                result.OrderId = orderId;
                result.Amount = amount;
                result.TransactionId = transactionId;
                result.PaymentDate = paymentDate;

                if (status == "success" || status == "completed")
                {
                    result.Success = true;
                    result.Message = "Payment successful";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Payment failed with status: {status}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error processing payment: {ex.Message}";
            }

            return result;
        }

        public async Task<bool> IsSEPayEnabledAsync()
        {
            var config = await _systemConfigService.LayGiaTriCauHinhAsync(PaymentConfigKeys.SEPAY_API_TOKEN);
            return !string.IsNullOrEmpty(config);
        }

        public async Task<Dictionary<string, string>> GetSEPayConfigAsync()
        {
            var configs = await _systemConfigService.LayCauHinhTheoLoaiAsync("SEPay");
            return configs.Where(c => c.IsActive).ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        }

        public string CreateSignature(Dictionary<string, string> data)
        {
            try
            {
                // SEPay không cần signature với cấu hình hiện tại
                // Chỉ cần API Token để kiểm tra giao dịch
                return "";
            }
            catch
            {
                return "";
            }
        }

        public string GenerateOrderId()
        {
            return $"SEP{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }

        // Lấy danh sách ngân hàng từ SEPay API
        public async Task<List<BankInfo>> GetSupportedBanksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://qr.sepay.vn/banks.json");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"SEPay API Response: {content}");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var banksData = JsonSerializer.Deserialize<BanksResponse>(content, options);
                    
                    if (banksData?.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found {banksData.Data.Count} banks, {banksData.Data.Count(b => b.Supported)} supported");
                        // Chỉ trả về các ngân hàng được hỗ trợ
                        return banksData.Data.Where(b => b.Supported).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting banks from SEPay API: {ex.Message}");
            }

            // Fallback: trả về danh sách ngân hàng mặc định
            return GetDefaultBanks();
        }

        private List<BankInfo> GetDefaultBanks()
        {
            return new List<BankInfo>
            {
                new BankInfo { Code = "BIDV", Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam", ShortName = "BIDV", Supported = true },
                new BankInfo { Code = "VCB", Name = "Ngân hàng TMCP Ngoại Thương Việt Nam", ShortName = "Vietcombank", Supported = true },
                new BankInfo { Code = "MB", Name = "Ngân hàng TMCP Quân đội", ShortName = "MBBank", Supported = true },
                new BankInfo { Code = "ACB", Name = "Ngân hàng TMCP Á Châu", ShortName = "ACB", Supported = true },
                new BankInfo { Code = "VPB", Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng", ShortName = "VPBank", Supported = true },
                new BankInfo { Code = "TPB", Name = "Ngân hàng TMCP Tiên Phong", ShortName = "TPBank", Supported = true },
                new BankInfo { Code = "STB", Name = "Ngân hàng TMCP Sài Gòn Thương Tín", ShortName = "Sacombank", Supported = true },
                new BankInfo { Code = "TCB", Name = "Ngân hàng TMCP Kỹ thương Việt Nam", ShortName = "Techcombank", Supported = true }
            };
        }

        // Thêm phương thức kiểm tra giao dịch theo SEPay API
        public async Task<SEPayTransactionResponse> CheckTransactionStatusAsync(string orderId)
        {
            var response = new SEPayTransactionResponse();
            
            try
            {
                var config = await GetSEPayConfigAsync();
                var apiUrl = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_API_URL, "");
                var apiToken = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_API_TOKEN, "");
                
                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiToken))
                {
                    response.Success = false;
                    response.Message = "SEPay API not configured";
                    return response;
                }

                // Gọi API kiểm tra giao dịch như trong script
                var requestUrl = $"{apiUrl}/transactions/list?order_id={orderId}&limit=5";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var httpResponse = await _httpClient.GetAsync(requestUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    if (data != null && data.ContainsKey("transactions"))
                    {
                        var transactions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(data["transactions"].ToString()!);
                        
                        if (transactions != null && transactions.Count > 0)
                        {
                            var latestTransaction = transactions[0];
                            
                            response.Success = true;
                            response.TransactionId = latestTransaction.GetValueOrDefault("transaction_id", "").ToString()!;
                            response.Amount = decimal.Parse(latestTransaction.GetValueOrDefault("amount_in", "0").ToString()!);
                            response.Status = latestTransaction.GetValueOrDefault("status", "").ToString()!;
                            response.TransactionDate = latestTransaction.GetValueOrDefault("transaction_date", "").ToString()!;
                            response.Message = "Transaction found";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No transactions found";
                        }
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "Invalid API response";
                    }
                }
                else
                {
                    response.Success = false;
                    response.Message = $"API request failed: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error checking transaction: {ex.Message}";
            }

            return response;
        }

        // Kiểm tra giao dịch theo số tài khoản như trong script
        public async Task<SEPayTransactionResponse> CheckTransactionByAccountAsync(string accountNumber)
        {
            var response = new SEPayTransactionResponse();
            
            try
            {
                var config = await GetSEPayConfigAsync();
                var apiUrl = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_API_URL, "");
                var apiToken = config.GetValueOrDefault(PaymentConfigKeys.SEPAY_API_TOKEN, "");
                
                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiToken))
                {
                    response.Success = false;
                    response.Message = "SEPay API not configured";
                    return response;
                }

                // Gọi API kiểm tra giao dịch theo số tài khoản như trong script
                var requestUrl = $"{apiUrl}/transactions/list?account_number={accountNumber}&limit=5";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var httpResponse = await _httpClient.GetAsync(requestUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    if (data != null && data.ContainsKey("transactions"))
                    {
                        var transactions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(data["transactions"].ToString()!);
                        
                        if (transactions != null && transactions.Count > 0)
                        {
                            var latestTransaction = transactions[0];
                            
                            response.Success = true;
                            response.TransactionId = latestTransaction.GetValueOrDefault("transaction_id", "").ToString()!;
                            response.Amount = decimal.Parse(latestTransaction.GetValueOrDefault("amount_in", "0").ToString()!);
                            response.Status = latestTransaction.GetValueOrDefault("status", "").ToString()!;
                            response.TransactionDate = latestTransaction.GetValueOrDefault("transaction_date", "").ToString()!;
                            response.Message = "Transaction found";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No transactions found";
                        }
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "Invalid API response";
                    }
                }
                else
                {
                    response.Success = false;
                    response.Message = $"API request failed: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error checking transaction: {ex.Message}";
            }

            return response;
        }
    }
}
