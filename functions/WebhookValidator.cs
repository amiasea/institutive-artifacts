using System.Security.Cryptography;
using System.Text;

namespace Amiasea.Functions;

public static class WebhookValidator
{
    /// <summary>
    /// Validates an HCP Terraform HMAC-SHA512 signature from the
    /// X-TFE-Notification-Signature header.
    /// </summary>
    public static bool ValidateTerraformSignature(
        string payload,
        string? signatureHeader,
        string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret) ||
            string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var key = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA512(key);

        var computedSignature =
            Convert.ToHexString(hmac.ComputeHash(payloadBytes))
                .ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signatureHeader));
    }

    /// <summary>
    /// Validates a GitHub HMAC-SHA256 signature from the
    /// X-Hub-Signature-256 header.
    /// </summary>
    public static bool ValidateGitHubSignature(
        string payload,
        string? signatureHeader,
        string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret) ||
            string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        const string prefix = "sha256=";

        if (!signatureHeader.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var providedSignature =
            signatureHeader[prefix.Length..];

        var key = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(key);

        var computedSignature =
            Convert.ToHexString(hmac.ComputeHash(payloadBytes))
                .ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(providedSignature));
    }
}