using System.Diagnostics;

var registry = Environment.GetEnvironmentVariable("REGISTRY_ENDPOINT") ?? "ghcr.io";

var repository = Environment.GetEnvironmentVariable("REGISTRY_REPOSITORY")
                 ?? throw new InvalidOperationException("REGISTRY_REPOSITORY is not set");

var sourceTag = Environment.GetEnvironmentVariable("SOURCE_TAG")
                ?? throw new InvalidOperationException("SOURCE_TAG is not set");

var targetTags = Environment.GetEnvironmentVariable("TARGET_TAGS")
                 ?? throw new InvalidOperationException("TARGET_TAGS is not set");

var skipPull = bool.TryParse(Environment.GetEnvironmentVariable("SKIP_PULL"), out var skip) && skip;

var images = Environment.GetEnvironmentVariable("IMAGES") ?? "api,migrator";

var prefix = $"{registry}/{repository}";
var imageList = images.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var tags = targetTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

static async Task RunDockerAsync(params string[] args)
{
    var psi = new ProcessStartInfo("docker")
    {
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
    };

    foreach (var arg in args)
    {
        psi.ArgumentList.Add(arg);
    }

    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException("Failed to start docker");

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"docker {string.Join(" ", args)} exited with code {process.ExitCode}");
    }
}

foreach (var image in imageList)
{
    var source = $"{prefix}/{image}:{sourceTag}";

    if (skipPull)
    {
        await RunDockerAsync("inspect", source);
    }
    else
    {
        Console.WriteLine($"Pulling {source}...");
        await RunDockerAsync("pull", source);
    }

    foreach (var tag in tags)
    {
        var target = $"{prefix}/{image}:{tag}";
        Console.WriteLine($"Tagging {source} -> {target}...");

        await RunDockerAsync("tag", source, target);
        Console.WriteLine($"Pushing {target}...");

        await RunDockerAsync("push", target);
    }
}
