using LiveSessionService.Application.Features.Results;
using LiveSessionService.Api.Models.Responses;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods to convert Application Result to API Response
/// This is where we bridge Application layer to API layer
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Convert Result{T} to ApiResponse{T}
    /// </summary>
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return ApiResponse<T>.SuccessResponse(
                result.Data!,
                result.ErrorMessage);
        }

        return ApiResponse<T>.FailureResponse(
            result.ErrorMessage ?? "An error occurred",
            result.ErrorCode);
    }

    /// <summary>
    /// Convert Result to ApiResponse{bool}
    /// </summary>
    public static ApiResponse<bool> ToApiResponse(this Result result)
    {
        if (result.IsSuccess)
        {
            return ApiResponse<bool>.SuccessResponse(true);
        }

        return ApiResponse<bool>.FailureResponse(
            result.ErrorMessage ?? "An error occurred",
            result.ErrorCode);
    }
}

