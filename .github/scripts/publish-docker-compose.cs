using System.Diagnostics;

var appVersion = Environment.GetEnvironmentVariable("APP_VERSION")
                 ?? throw new InvalidOperationException("APP_VERSION is not set");

var outputDir = Environment.GetEnvironmentVariable("OUTPUT_DIR") ?? "artifacts/compose";

var projectPath = Environment.GetEnvironmentVariable("APPHOST_PROJECT_PATH")
                  ?? throw new InvalidOperationException("APPHOST_PROJECT_PATH is not set");

if (!File.Exists(projectPath))
{
    Console.Error.WriteLine($"AppHost project not found: {projectPath}");
    Environment.Exit(1);
}

Console.WriteLine("=== Generating Docker Compose artifacts ===");
Console.WriteLine($"APP_VERSION: {appVersion}");
Console.WriteLine($"Output directory: {outputDir}");

var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
Directory.CreateDirectory(tmpDir);

try
{
    var psi = new ProcessStartInfo("aspire")
    {
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
    };

    psi.ArgumentList.Add("publish");
    psi.ArgumentList.Add("--project");
    psi.ArgumentList.Add(projectPath);
    psi.ArgumentList.Add("--output-path");
    psi.ArgumentList.Add(tmpDir);
    psi.ArgumentList.Add("--non-interactive");
    psi.Environment["APP_VERSION"] = appVersion;

    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException("Failed to start aspire");

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"aspire publish exited with code {process.ExitCode}");
    }

    var parentDir = Path.GetDirectoryName(Path.GetFullPath(outputDir));
    if (!string.IsNullOrEmpty(parentDir))
    {
        Directory.CreateDirectory(parentDir);
    }

    var newOutputDir = $"{outputDir}.new";

    if (Directory.Exists(newOutputDir))
    {
        Directory.Delete(newOutputDir, recursive: true);
    }

    Directory.Move(tmpDir, newOutputDir);

    if (Directory.Exists(outputDir))
    {
        Directory.Delete(outputDir, recursive: true);
    }

    Directory.Move(newOutputDir, outputDir);
}
finally
{
    if (Directory.Exists(tmpDir))
    {
        Directory.Delete(tmpDir, recursive: true);
    }
}

Console.WriteLine("=== Generated files ===");
foreach (var file in Directory.EnumerateFiles(outputDir))
{
    Console.WriteLine($"  {Path.GetFileName(file)}");
}
