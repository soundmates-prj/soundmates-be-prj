using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AccountContentService.Infrastructure.Integrations.PaymentGateway
{
    public static class VNPayHelper
    {
        public static bool VerifySignature(
            Dictionary<string, string> data,
            string secret)
        {
            if (!data.ContainsKey("vnp_SecureHash"))
                return false;

            var vnpSecureHash = data["vnp_SecureHash"];

            // Determine hash algorithm: default SHA256, use SHA512 only when explicitly specified
            // vnp_SecureHashType can be "SHA256" or "SHA512" (VNPay sandbox often uses SHA256)
            var hashType = "SHA256";
            if (data.TryGetValue("vnp_SecureHashType", out var secureHashType)
                && !string.IsNullOrEmpty(secureHashType))
            {
                hashType = secureHashType.ToUpperInvariant();
            }

            // Remove hash fields
            var filtered = data
                .Where(k => k.Key != "vnp_SecureHash" && k.Key != "vnp_SecureHashType")
                .OrderBy(k => k.Key)
                .ToDictionary(k => k.Key, v => v.Value);

            var rawData = string.Join("&",
                filtered.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            var hash = HmacSHA512(secret, rawData);

            return hash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
