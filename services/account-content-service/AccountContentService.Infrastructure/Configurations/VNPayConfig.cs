using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AccountContentService.Infrastructure.Configurations
{
    public class VNPayConfig
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string BaseUrlProd { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class VNPayLibrary
    {
        private SortedList<string, string> _requestData = new();
        private SortedList<string, string> _responseData = new();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var query = new StringBuilder();

            foreach (var kv in _requestData)
            {
                query.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}&");
            }

            string queryString = query.ToString().TrimEnd('&');

            string secureHash = HmacSHA512(hashSecret, queryString);

            return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rawData = new StringBuilder();

            foreach (var kv in _responseData)
            {
                if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    rawData.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}&");
                }
            }

            string data = rawData.ToString().TrimEnd('&');

            string computedHash = HmacSHA512(secretKey, data);

            return computedHash.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
