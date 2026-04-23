namespace AccountContentService.Infrastructure.Configurations;

public class SePayConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://my.sepay.vn";
}
