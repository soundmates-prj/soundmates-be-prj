namespace AuthQueryService.Domain.Entities.ReadModels
{
    /// <summary>
    /// Read model for Role projection in Query Service
    /// Represents denormalized role data optimized for read operations
    /// </summary>
    public sealed class RoleReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
