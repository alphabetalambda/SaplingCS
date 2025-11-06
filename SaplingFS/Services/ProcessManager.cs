using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SaplingFS.Services;

/// <summary>
/// Manages process operations including finding and killing processes that hold file handles.
/// </summary>
public class ProcessManager
{
    private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets a list of process IDs that own a handle to the given file.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>List of process IDs</returns>
    public async Task<List<int>> GetHandleOwnersAsync(string filePath)
    {
        try
        {
            var (command, arguments) = _isWindows
                ? ("handle.exe", $"-p -u \"{filePath}\"")
                : ("lsof", $"-F p \"{filePath}\"");

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return [];

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (_isWindows)
            {
                return ParseWindowsHandleOutput(output, filePath);
            }
            else
            {
                return ParseLsofOutput(output);
            }
        }
        catch (Exception)
        {
            return [];
        }
    }

    private List<int> ParseWindowsHandleOutput(string output, string filePath)
    {
        var pids = new List<int>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains(filePath, StringComparison.OrdinalIgnoreCase))
            {
                // Parse PID from handle.exe output
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
                {
                    pids.Add(pid);
                }
            }
        }

        return pids;
    }

    private List<int> ParseLsofOutput(string output)
    {
        var pids = new List<int>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // lsof -F p output format: "p<PID>"
            if (line.StartsWith('p') && int.TryParse(line[1..], out var pid))
            {
                pids.Add(pid);
            }
        }

        return pids;
    }

    /// <summary>
    /// Kills the process with the specified PID.
    /// </summary>
    /// <param name="pid">Process ID to kill</param>
    /// <param name="signal">Signal number (Unix only, default is SIGKILL=9)</param>
    public async Task KillProcessAsync(int pid, int signal = 9)
    {
        try
        {
            if (_isWindows)
            {
                // Use taskkill on Windows
                var startInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {pid} /F",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            else
            {
                // Use kill on Unix-like systems
                var startInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-{signal} {pid}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to kill process {pid}: {ex.Message}");
        }
    }
}
