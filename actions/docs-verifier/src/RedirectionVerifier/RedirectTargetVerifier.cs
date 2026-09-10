using System.Collections.Immutable;
using System.Net;
using System.Text.Json;

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

        List<int> redirectUrlLineNumbers = await GetRedirectUrlLineNumbersAsync(redirectionFilePath);

        bool isValid = true;
        for (int i = 0; i < redirections.Length; i++)
        {
            int? lineNumber = i < redirectUrlLineNumbers.Count ? redirectUrlLineNumbers[i] : null;
            string? redirectUrl = redirections[i].RedirectUrl;
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                await WriteErrorAsync(writer, redirectionFilePath, lineNumber, "Redirection has an empty 'redirect_url'.");
                isValid = false;
                continue;
            }

            if (redirectUrl[0] != '/')
            {
                continue;
            }

            string redirectTarget = $"{LearnMicrosoftCom}{redirectUrl}";

            bool hasValidUri = Uri.TryCreate(redirectTarget, UriKind.Absolute, out Uri? uri)
                && uri is not null
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!hasValidUri)
            {
                await WriteErrorAsync(writer, redirectionFilePath, lineNumber, $"Invalid 'redirect_url': '{redirectUrl}'.");
                isValid = false;
                continue;
            }

            HttpStatusCode? statusCode = await statusCodeProvider(uri!);
            if (statusCode is null)
            {
                await WriteErrorAsync(writer, redirectionFilePath, lineNumber, $"Unable to verify 'redirect_url': '{redirectUrl}'.");
                isValid = false;
                continue;
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                await WriteErrorAsync(writer, redirectionFilePath, lineNumber, $"Redirect target returns 404: '{redirectUrl}'.");
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

    private static async Task<List<int>> GetRedirectUrlLineNumbersAsync(string redirectionFilePath)
    {
        byte[] content = await File.ReadAllBytesAsync(redirectionFilePath);
        var lineStarts = new List<int> { 0 };
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == (byte)'\n')
            {
                lineStarts.Add(i + 1);
            }
        }

        int GetLineNumber(long tokenStartIndex)
        {
            int index = (int)tokenStartIndex;
            int lineStartIndex = lineStarts.BinarySearch(index);
            if (lineStartIndex < 0)
            {
                lineStartIndex = ~lineStartIndex - 1;
            }

            return lineStartIndex + 1;
        }

        var lineNumbers = new List<int>();
        var reader = new Utf8JsonReader(content, new JsonReaderOptions { AllowTrailingCommas = true });
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("redirect_url"))
            {
                continue;
            }

            if (!reader.Read())
            {
                break;
            }

            lineNumbers.Add(GetLineNumber(reader.TokenStartIndex));
        }

        return lineNumbers;
    }

    private static Task WriteErrorAsync(TextWriter writer, string filePath, int? lineNumber, string message)
        => lineNumber.HasValue
            ? writer.WriteLineAsync($"::error file={filePath},line={lineNumber.Value}::{message}")
            : writer.WriteLineAsync($"::error file={filePath}::{message}");
}
