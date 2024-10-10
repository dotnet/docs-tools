using System.Text;

namespace DotNetDocs.Tools.GitHubCommunications;

/// <summary>
/// This basic class represents a GraphQL post packet.
/// </summary>
/// <remarks>
/// This class represents a query packet. It has the shape expected
/// for a GraphQL query.
/// </remarks>
public static class GitHubEventPayloadValidator
{
    /// <summary>
    /// Validates the body matches the hash of the signature.
    /// </summary>
    /// <param name="payloadBody">Payload body.</param>
    /// <param name="payloadSignature">The signature of the request.</param>
    /// <param name="log">Logging object.</param>
    /// <returns><see langword="true"/> when validated; otherwise <see langword="false"/>.</returns>
    public static bool IsSecure(string payloadBody, string payloadSignature, string hashSecret)
    {
        const string ShaPrefix = "sha256=";

        if (!payloadSignature.StartsWith(ShaPrefix)) return false;

        payloadSignature = payloadSignature.Substring(ShaPrefix.Length);

        byte[] secret = Encoding.ASCII.GetBytes(hashSecret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadBody);

        using System.Security.Cryptography.HMACSHA256 sha256 = new(secret);
        string result = ToHexString(sha256.ComputeHash(payloadBytes));

        if (string.Equals(result, payloadSignature, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;

        static string ToHexString(byte[] bytes)
        {
            StringBuilder builder = new(bytes.Length * 2);

            foreach (byte b in bytes)
                builder.AppendFormat("{0:x2}", b);

            return builder.ToString();
        }
    }
}
