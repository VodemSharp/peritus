using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

var owner = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_OWNER")
            ?? throw new InvalidOperationException("GITHUB_REPOSITORY_OWNER is not set");

var ownerType = Environment.GetEnvironmentVariable("GITHUB_OWNER_TYPE") ?? "user";
var apiOwnerSegment = ownerType == "org" ? "orgs" : "users";

var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GITHUB_TOKEN is not set");

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run delete-old-container-images.cs -- <package> [keepCount]");
    Environment.Exit(1);
}

var package = args[0];
var keepCount = args.Length > 1 && int.TryParse(args[1], out var k) ? k : 20;

var encodedPackage = Uri.EscapeDataString(package);
var baseUrl = $"https://api.github.com/{apiOwnerSegment}/{owner}/packages/container/{encodedPackage}/versions";

using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
client.DefaultRequestHeaders.UserAgent.ParseAdd("Epsy-Cleanup-Script");

var allVersions = new List<JsonElement>();

var pageUrl = $"{baseUrl}?per_page=100";
while (!string.IsNullOrEmpty(pageUrl))
{
    Console.WriteLine($"Fetching: {pageUrl}");

    using var response = await client.GetAsync(pageUrl);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    using var document = JsonDocument.Parse(json);
    var body = document.RootElement;

    if (body.ValueKind == JsonValueKind.Array)
    {
        allVersions.AddRange(body.EnumerateArray().Select(item => item.Clone()));
    }

    pageUrl = ExtractNextUrl(response.Headers);
}

var namedCount = 0;
var shaOnly = new List<(JsonElement Version, DateTimeOffset CreatedAt)>();

foreach (var version in allVersions)
{
    var tags = GetTags(version);
    var isProtected = tags.Any(tag => tag is not null && RegexHolder.ProtectedRegex.IsMatch(tag));

    if (isProtected)
    {
        namedCount++;
        continue;
    }

    var createdAt = version.TryGetProperty("created_at", out var createdProp)
                    && DateTimeOffset.TryParse(createdProp.GetString(), out var parsed)
        ? parsed
        : DateTimeOffset.MinValue;

    shaOnly.Add((version, createdAt));
}

var toDelete = shaOnly
    .OrderByDescending(x => x.CreatedAt)
    .Skip(keepCount)
    .Select(x => x.Version)
    .ToList();

Console.WriteLine($"Named tag versions (protected): {namedCount}");
Console.WriteLine($"SHA-only versions: {shaOnly.Count}");
Console.WriteLine($"Will delete: {toDelete.Count}");

var failedIds = new List<long>();

foreach (var version in toDelete)
{
    var id = version.GetProperty("id").GetInt64();
    var tags = GetTags(version);
    var tagsString = string.Join(", ", tags);
    var created = version.TryGetProperty("created_at", out var createdProp) ? createdProp.GetString() : null;

    Console.WriteLine($"  Deleting version {id} (tags: [{tagsString}], created: {created})");

    var deleteUrl = $"{baseUrl}/{id}";
    using var response = await client.DeleteAsync(deleteUrl);
    var status = (int)response.StatusCode;

    if (status is 204 or 404)
    {
        Console.WriteLine("    -> OK");
    }
    else
    {
        Console.WriteLine($"    -> FAILED ({status})");
        failedIds.Add(id);
    }
}

if (failedIds.Count > 0)
{
    Console.Error.WriteLine($"Failed to delete versions: {string.Join(" ", failedIds)}");
    Environment.Exit(1);
}

static List<string?> GetTags(JsonElement version)
{
    if (!version.TryGetProperty("metadata", out var metadata)
        || !metadata.TryGetProperty("container", out var container)
        || !container.TryGetProperty("tags", out var tags)
        || tags.ValueKind != JsonValueKind.Array)
    {
        return new List<string?>();
    }

    return tags.EnumerateArray()
        .Select(t => t.GetString())
        .Where(t => !string.IsNullOrEmpty(t))
        .ToList();
}

static string? ExtractNextUrl(HttpResponseHeaders headers)
{
    if (!headers.TryGetValues("Link", out var values))
    {
        return null;
    }

    // ReSharper disable once LoopCanBeConvertedToQuery
    foreach (var link in values)
    {
        var match = RegexHolder.LinkNextRegex.Match(link);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
    }

    return null;
}

internal static partial class RegexHolder
{
    [GeneratedRegex("^(latest|dev-latest|v[0-9])", RegexOptions.Compiled)]
    public static partial Regex ProtectedRegex { get; }

    [GeneratedRegex("""<([^>]+)>\s*;\s*rel="next""", RegexOptions.Compiled)]
    public static partial Regex LinkNextRegex { get; }
}
