namespace AuthService.Application.Results;

/// <summary>
/// Role Result for Application layer
/// Pure data contract - no Domain dependencies
/// Used for role information in responses
/// </summary>
public class RoleResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}


