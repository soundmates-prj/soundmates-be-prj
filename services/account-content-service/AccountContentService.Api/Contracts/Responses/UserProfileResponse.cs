namespace AccountContentService.Api.Contracts.Responses
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
    }
}
