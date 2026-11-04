using System.Security.Cryptography;
using System.Text;

namespace CountMeIn.Features.Checks;

/// <summary>Stream ids, id validation and hashing in one place. Ids reach the filesystem (folder-per-stream), so they are an allowlist.</summary>
public static class Streams
{
    public const int MaxIdLength = 64;
    public const int MaxTitleLength = 120;
    public const int MaxPitchLength = 1000;
    public const int MaxNameLength = 60;
    public const int MaxEmailLength = 200;
    public const int MaxGoal = 100000;
    public const string OwnerKeyHeader = "X-Owner-Key";

    public static string Check(string checkId) => $"check-{checkId}";

    /// <summary>Short url-safe id: letters, digits, '-' and '_', at most 64 characters.</summary>
    public static bool IsValidId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength) return false;
        foreach (var c in id)
        {
            var ok = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_';
            if (!ok) return false;
        }
        return true;
    }

    public static string Sha256Hex(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    /// <summary>The anonymous handle the public sees for a person: first 16 hex chars of SHA-256("checkId:personId").</summary>
    public static string PersonHandle(string checkId, string personId) => Sha256Hex($"{checkId}:{personId}")[..16];

    /// <summary>A fresh owner key: 32 url-safe characters from 24 cryptographically random bytes.</summary>
    public static string NewOwnerKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_');

    /// <summary>Does the key the caller sent hash to the stored hash? Constant-time; a missing key never matches.</summary>
    public static bool OwnerKeyMatches(string? sentKey, string? storedHash)
    {
        if (string.IsNullOrEmpty(sentKey) || string.IsNullOrEmpty(storedHash)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Sha256Hex(sentKey)), Encoding.UTF8.GetBytes(storedHash));
    }

    public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");
}
