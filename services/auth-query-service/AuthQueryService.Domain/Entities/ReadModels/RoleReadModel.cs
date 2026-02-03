using System.Collections.Generic;

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
        
        /// <summary>
        /// Optional list of user ids that belong to this role.
        /// This exists to match the persisted MongoDB schema where a "Users"
        /// array is present on each role document.
        /// </summary>
        public List<Guid> Users { get; set; } = new();
    }
}
