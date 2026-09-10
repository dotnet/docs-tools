using System.Collections.Immutable;
using System.Net;

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
    {
        ArgumentNullException.ThrowIfNull(writer, nameof(writer));
        ArgumentNullException.ThrowIfNull(redirectionFilePath, nameof(redirectionFilePath));

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

            HttpStatusCode? statusCode = await GetStatusCodeAsync(uri!);
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
