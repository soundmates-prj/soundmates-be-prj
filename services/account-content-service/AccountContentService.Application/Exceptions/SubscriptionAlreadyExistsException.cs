using System;

namespace AccountContentService.Application.Exceptions;

/// <summary>
/// Thrown when the user already has an active subscription and tries to purchase another.
/// </summary>
public class SubscriptionAlreadyExistsException : Exception
{
    public SubscriptionAlreadyExistsException()
        : base("Bạn đã có gói đăng ký đang hoạt động. Không thể mua thêm gói mới khi gói hiện tại còn hiệu lực.")
    {
    }

    public SubscriptionAlreadyExistsException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the subscription plan referenced in the request does not exist or is inactive.
/// </summary>
public class SubscriptionPlanNotFoundException : Exception
{
    public SubscriptionPlanNotFoundException()
        : base("Gói đăng ký không tồn tại hoặc hiện không khả dụng.")
    {
    }

    public SubscriptionPlanNotFoundException(string message) : base(message) { }
}
