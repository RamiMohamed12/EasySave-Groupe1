using System.Net.Http.Json;
using System.Text.Json;

public class CentralLogClient
{
    public const string ApiKeyHeaderName = "X-EasyLog-Api-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;

    public CentralLogClient()
        : this(new HttpClient())
    {
    }

    public CentralLogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendLogAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        string serverUrl = RuntimeStoragePaths.GetCentralLogServerUrl();
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new InvalidOperationException("Central log server URL is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint(serverUrl, "api/logs"))
        {
            Content = JsonContent.Create(entry, options: JsonOptions)
        };
        AddApiKeyHeader(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string details = string.IsNullOrWhiteSpace(responseBody) ? response.ReasonPhrase ?? response.StatusCode.ToString() : responseBody;
            throw new InvalidOperationException($"Central log server rejected the entry ({(int)response.StatusCode}): {details}");
        }
    }

    public async Task<string> GetDailyLogAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        string serverUrl = RuntimeStoragePaths.GetCentralLogServerUrl();
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new InvalidOperationException("Central log server URL is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildEndpoint(serverUrl, $"api/logs/{date:yyyy-MM-dd}"));
        AddApiKeyHeader(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return string.Empty;
        }

        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string details = string.IsNullOrWhiteSpace(responseBody) ? response.ReasonPhrase ?? response.StatusCode.ToString() : responseBody;
            throw new InvalidOperationException($"Central log server could not return the daily log ({(int)response.StatusCode}): {details}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Uri BuildEndpoint(string serverUrl, string relativePath)
    {
        return new Uri($"{serverUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}", UriKind.Absolute);
    }

    private static void AddApiKeyHeader(HttpRequestMessage request)
    {
        string apiKey = RuntimeStoragePaths.GetCentralLogApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add(ApiKeyHeaderName, apiKey);
        }
    }
}
