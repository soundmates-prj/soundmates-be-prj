namespace AuthService.Api.Models.Responses;

/// <summary>
/// Pagination response wrapper for REST API
/// Used for paginated list endpoints
/// </summary>
public class PageResponse<T>
{
    public List<T> Content { get; set; } = new();
    public long TotalElements { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
}
