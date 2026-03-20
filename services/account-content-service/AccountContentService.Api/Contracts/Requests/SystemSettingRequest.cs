namespace AccountContentService.Api.Contracts.Requests
{
    public class SystemSettingRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public string SettingType { get; set; } = string.Empty;

        public string Descreption { get; set; } = string.Empty;
    }
}
