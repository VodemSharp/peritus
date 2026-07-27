using System.Diagnostics;

var outputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT")
                 ?? throw new InvalidOperationException("GITHUB_OUTPUT is not set");

// This script requires full git history. Detect shallow clones early.
if (await IsShallowRepositoryAsync())
{
    Console.Error.WriteLine("Error: shallow clone detected. fetch-depth: 0 is required.");
    Environment.Exit(1);
}

if (!await FetchCandidateTagAsync())
{
    Console.WriteLine("Candidate tag does not exist. Will build fresh.");
    AppendOutput(outputFile, "safe=false");
    return;
}

var candidateSha = (await RunGitAsync("rev-parse", "candidate")).Trim();
var headSha = (await RunGitAsync("rev-parse", "HEAD")).Trim();

// Case 1: HEAD is exactly the candidate commit (fast-forward or direct tag)
if (headSha == candidateSha)
{
    Console.WriteLine($"Release commit matches candidate ({candidateSha})");
    AppendOutput(outputFile, "safe=true", $"promote_sha={candidateSha}");
    return;
}

// Case 2: HEAD is a merge commit that merged candidate (dev → main merge).
// HEAD^2 resolves to the second parent (the dev branch tip).
// NOTE: This only works for true merge commits. If main is updated with
// squash merges or additional commits after the merge, this branch will NOT
// fire and the release will fall back to building fresh.
var headCandidateParent = await RunGitOptionalAsync("rev-parse", "HEAD^2");

if (headCandidateParent == candidateSha)
{
    Console.WriteLine($"Release is a merge of candidate ({candidateSha})");
    AppendOutput(outputFile, "safe=true", $"promote_sha={candidateSha}");
    return;
}

// Case 3: Squash merge — candidate is an ancestor with exactly 1 commit ahead.
// This handles dev → main squash merges where all candidate changes are
// flattened into a single commit on main, with no extra commits on top.
if (await IsAncestorAsync("candidate", "HEAD"))
{
    var aheadCount = (await RunGitAsync("rev-list", "--count", "candidate..HEAD")).Trim();
    if (int.TryParse(aheadCount, out var count) && count == 1)
    {
        Console.WriteLine($"Release is a squash merge of candidate ({candidateSha})");
        AppendOutput(outputFile, "safe=true", $"promote_sha={candidateSha}");
        return;
    }

    Console.WriteLine($"Candidate is ancestor but {aheadCount} commits ahead (expected 1). Will build fresh.");
}

// Case 4: Anything else — build fresh
Console.WriteLine("Release commit does not match candidate. Will build fresh.");
AppendOutput(outputFile, "safe=false");

static async Task<bool> IsShallowRepositoryAsync()
{
    var result = await RunGitOptionalAsync("rev-parse", "--is-shallow-repository");
    return result == "true";
}

static async Task<bool> FetchCandidateTagAsync()
{
    var result = await RunGitRawAsync(["fetch", "origin", "+refs/tags/candidate:refs/tags/candidate"]);
    return result.ExitCode == 0;
}

static async Task<bool> IsAncestorAsync(string ancestor, string descendant)
{
    var result = await RunGitRawAsync(["merge-base", "--is-ancestor", ancestor, descendant]);
    return result.ExitCode == 0;
}

static async Task<string> RunGitAsync(params string[] args)
{
    var result = await RunGitRawAsync(args);

    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"git {string.Join(" ", args)} exited with code {result.ExitCode}");
    }

    return result.Stdout;
}

static async Task<string?> RunGitOptionalAsync(params string[] args)
{
    try
    {
        return await RunGitAsync(args);
    }
    catch
    {
        return null;
    }
}

static async Task<(int ExitCode, string Stdout)> RunGitRawAsync(string[] args)
{
    var psi = new ProcessStartInfo("git")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = false,
    };

    foreach (var arg in args)
    {
        psi.ArgumentList.Add(arg);
    }

    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException("Failed to start git");

    var stdout = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return (process.ExitCode, stdout);
}

static void AppendOutput(string path, params string[] lines)
{
    foreach (var line in lines)
    {
        File.AppendAllText(path, line + Environment.NewLine);
    }
}
