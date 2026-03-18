namespace AccountContentService.Api.Common
{
    public static class RequestContext
    {
        public static string GetIpAddress(HttpContext context)
        {
            if (context == null)
                return "127.0.0.1";

            // Ưu tiên lấy từ header (khi deploy qua proxy/ngrok)
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrEmpty(ip))
            {
                // Nếu có nhiều IP (proxy chain) → lấy cái đầu
                return ip.Split(',')[0];
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }
    }
}
