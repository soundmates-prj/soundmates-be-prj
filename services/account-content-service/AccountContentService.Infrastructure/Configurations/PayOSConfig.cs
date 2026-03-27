namespace AccountContentService.Infrastructure.Configurations;

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.payos.vn";
    public string SandboxBaseUrl { get; set; } = "https://api-sandbox.payos.vn";
    public string ReturnUrl { get; set; } = string.Empty;
}
