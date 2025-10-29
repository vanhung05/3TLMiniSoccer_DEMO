namespace _3TLMiniSoccer.Models
{
    public class OAuthUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty; // "Google" or "Facebook"
        public string Locale { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }

    public class OAuthLoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public OAuthUserInfo? UserInfo { get; set; }
        public string? RedirectUrl { get; set; }
    }
}
