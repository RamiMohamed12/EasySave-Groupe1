var builder = WebApplication.CreateBuilder(args);

string logsDirectory = builder.Configuration["EASYLOG_LOGS_DIRECTORY"]
    ?? Environment.GetEnvironmentVariable("EASYLOG_LOGS_DIRECTORY")
    ?? "/logs";
string apiKey = builder.Configuration["EASYLOG_API_KEY"]
    ?? Environment.GetEnvironmentVariable("EASYLOG_API_KEY")
    ?? string.Empty;

builder.Services.AddSingleton(new EasyLogFileStore(logsDirectory));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "EasyLog.Server",
    timeUtc = DateTime.UtcNow
}));

app.MapPost("/api/logs", async (
    HttpRequest request,
    LogEntry? entry,
    EasyLogFileStore store,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(request, apiKey))
    {
        return Results.Unauthorized();
    }

    if (!EasyLogFileStore.IsValid(entry, out string errorMessage))
    {
        return Results.BadRequest(errorMessage);
    }

    await store.AppendAsync(entry!, cancellationToken).ConfigureAwait(false);
    return Results.Accepted();
});

app.MapGet("/api/logs/{date}", async (
    string date,
    HttpRequest request,
    EasyLogFileStore store,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(request, apiKey))
    {
        return Results.Unauthorized();
    }

    string? content = await store.ReadDailyLogAsync(date, cancellationToken).ConfigureAwait(false);
    if (content is null)
    {
        return Results.BadRequest("Date must use yyyy-MM-dd format.");
    }

    if (content.Length == 0)
    {
        return Results.NotFound();
    }

    return Results.Text(content, "application/x-ndjson");
});

app.Run();

static bool IsAuthorized(HttpRequest request, string configuredApiKey)
{
    if (string.IsNullOrWhiteSpace(configuredApiKey))
    {
        return true;
    }

    return request.Headers.TryGetValue(CentralLogClient.ApiKeyHeaderName, out var receivedApiKey)
        && string.Equals(receivedApiKey.ToString(), configuredApiKey, StringComparison.Ordinal);
}
