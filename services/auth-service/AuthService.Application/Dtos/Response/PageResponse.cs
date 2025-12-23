namespace AuthService.Application.DTOs.Response
{
    public class PageResponse<T>
    {
        public List<T> Content { get; set; } = new();
        public long TotalElements { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }
}