using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;

namespace RedirectionVerifier;

public static class RedirectTargetVerifier
{
    private const string LearnMicrosoftCom = "https://learn.microsoft.com";
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    /// <summary>
    /// Verifies that redirect targets in an entire redirection file are valid.
    /// </summary>
    public static async Task<bool> WriteResultsAsync(
        TextWriter writer,
        string redirectionFilePath)
        => await WriteResultsAsync(writer, redirectionFilePath, GetStatusCodeAsync);

    internal static async Task<bool> WriteResultsAsync(
        TextWriter writer,
        string redirectionFilePath,
        Func<Uri, Task<HttpStatusCode?>> statusCodeProvider)
    {
        ArgumentNullException.ThrowIfNull(writer, nameof(writer));
        ArgumentNullException.ThrowIfNull(redirectionFilePath, nameof(redirectionFilePath));
        ArgumentNullException.ThrowIfNull(statusCodeProvider, nameof(statusCodeProvider));

        if (!File.Exists(redirectionFilePath))
        {
            await writer.WriteLineAsync($"::error::Redirection file '{redirectionFilePath}' does not exist.");
            return false;
        }

        OpenPublishingRedirectionReader reader = new(redirectionFilePath);
        ImmutableArray<Redirection> redirections = await reader.MapConfigurationAsync();
        if (redirections.IsDefaultOrEmpty)
        {
            return true;
        }

        bool isValid = true;
        for (int i = 0; i < redirections.Length; i++)
        {
            string? redirectUrl = redirections[i].RedirectUrl;
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                await writer.WriteLineAsync($"::error file={redirectionFilePath}::Redirection at index {i} has an empty 'redirect_url'.");
                isValid = false;
                continue;
            }

            string redirectTarget = redirectUrl.StartsWith('/')
                ? $"{LearnMicrosoftCom}{redirectUrl}"
                : redirectUrl;

            bool hasValidUri = Uri.TryCreate(redirectTarget, UriKind.Absolute, out Uri? uri)
                && uri is not null
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!hasValidUri)
            {
                await writer.WriteLineAsync($"::error file={redirectionFilePath}::Invalid 'redirect_url' at index {i}: '{redirectUrl}'.");
                isValid = false;
                continue;
            }

            if (!await IsPublicHttpTargetAsync(uri!))
            {
                await writer.WriteLineAsync($"::error file={redirectionFilePath}::Disallowed 'redirect_url' target at index {i}: '{redirectUrl}'.");
                isValid = false;
                continue;
            }

            HttpStatusCode? statusCode = await statusCodeProvider(uri!);
            if (statusCode is null)
            {
                await writer.WriteLineAsync($"::error file={redirectionFilePath}::Unable to verify 'redirect_url' at index {i}: '{redirectUrl}'.");
                isValid = false;
                continue;
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                await writer.WriteLineAsync($"::error file={redirectionFilePath}::Redirect target returns 404 at index {i}: '{redirectUrl}'.");
                isValid = false;
            }
        }

        return isValid;
    }

    private static async Task<bool> IsPublicHttpTargetAsync(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out IPAddress? parsedAddress))
        {
            return !IsPrivateOrLocalAddress(parsedAddress);
        }

        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(uri.Host);
            return addresses.All(address => !IsPrivateOrLocalAddress(address));
        }
        catch (SocketException)
        {
            return true;
        }
    }

    private static bool IsPrivateOrLocalAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
            {
                return true;
            }

            byte[] bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }

    private static async Task<HttpStatusCode?> GetStatusCodeAsync(Uri uri)
    {
        try
        {
            using HttpRequestMessage headRequest = new(HttpMethod.Head, uri);
            using HttpResponseMessage headResponse = await s_httpClient.SendAsync(headRequest);
            if (headResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented or HttpStatusCode.NotFound)
            {
                using HttpRequestMessage getRequest = new(HttpMethod.Get, uri);
                using HttpResponseMessage getResponse = await s_httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);
                return getResponse.StatusCode;
            }

            return headResponse.StatusCode;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
}
