using AuthService.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Response
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public int? ErrorCode { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
            => new ApiResponse<T> { Success = true, Data = data, Message = message };

        public static ApiResponse<T> FailureResponse(string message, int? errorCode = null)
            => new ApiResponse<T> { Success = false, Message = message, ErrorCode = errorCode };

        public static ApiResponse<T> Error(ApiStatusCode apiCode, string message)
            => new ApiResponse<T> { Success = false, Message = message, ErrorCode = (int)apiCode };
    }
}
