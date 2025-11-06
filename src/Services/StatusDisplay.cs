namespace SaplingFS.Services;

/// <summary>
/// Displays real-time progress information during file scanning and terrain generation.
/// </summary>
public class StatusDisplay
{
    private readonly object _lock = new();
    private string _currentPhase = "";
    private string _currentDetail = "";
    private int _filesScanned;
    private int _filesProcessed;
    private int _totalFiles;
    private DateTime _phaseStartTime = DateTime.Now;
    private System.Timers.Timer? _updateTimer;
    private bool _isActive;

    /// <summary>
    /// Starts the status display with automatic updates.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            _isActive = true;
            Console.CursorVisible = false;

            // Update display every 100ms
            _updateTimer = new System.Timers.Timer(100);
            _updateTimer.Elapsed += (sender, e) => Render();
            _updateTimer.Start();
        }
    }

    /// <summary>
    /// Stops the status display and restores console.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _isActive = false;
            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            // Clear the status lines
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            // Move cursor back up and clear
            if (Console.CursorTop >= 4)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 4);
            }

            Console.CursorVisible = true;
        }
    }

    /// <summary>
    /// Updates the current phase of operation.
    /// </summary>
    public void SetPhase(string phase)
    {
        lock (_lock)
        {
            _currentPhase = phase;
            _currentDetail = "";
            _phaseStartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Updates the detail message for the current phase.
    /// </summary>
    public void SetDetail(string detail)
    {
        lock (_lock)
        {
            _currentDetail = detail;
        }
    }

    /// <summary>
    /// Updates file scanning progress.
    /// </summary>
    public void UpdateFileScanning(int filesFound)
    {
        lock (_lock)
        {
            _filesScanned = filesFound;
        }
    }

    /// <summary>
    /// Sets the total number of files for terrain generation.
    /// </summary>
    public void SetTotalFiles(int total)
    {
        lock (_lock)
        {
            _totalFiles = total;
            _filesProcessed = 0;
        }
    }

    /// <summary>
    /// Updates terrain generation progress.
    /// </summary>
    public void UpdateTerrainGeneration(int filesProcessed)
    {
        lock (_lock)
        {
            _filesProcessed = filesProcessed;
        }
    }

    /// <summary>
    /// Renders the status display to the console.
    /// </summary>
    private void Render()
    {
        if (!_isActive) return;

        lock (_lock)
        {
            try
            {
                var elapsed = DateTime.Now - _phaseStartTime;
                var elapsedStr = elapsed.TotalSeconds < 60
                    ? $"{elapsed.TotalSeconds:F1}s"
                    : $"{elapsed.TotalMinutes:F1}m";

                // Save cursor position
                int originalTop = Console.CursorTop;
                int originalLeft = Console.CursorLeft;

                // Build status lines
                var lines = new List<string>();

                // Line 1: Phase and elapsed time
                lines.Add($"┌─ {_currentPhase} ({elapsedStr})");

                // Line 2: Progress bar and percentage (if applicable)
                if (_totalFiles > 0 && _filesProcessed > 0)
                {
                    double percentage = (double)_filesProcessed / _totalFiles * 100;
                    int barWidth = Math.Min(40, Console.WindowWidth - 20);
                    int filled = (int)(barWidth * _filesProcessed / _totalFiles);

                    string bar = "[" +
                                new string('█', filled) +
                                new string('░', barWidth - filled) +
                                "]";

                    string progressText = $"{_filesProcessed:N0} / {_totalFiles:N0} files";
                    lines.Add($"│ {bar} {percentage:F1}% ({progressText})");
                }
                else if (_filesScanned > 0)
                {
                    lines.Add($"│ Files found: {_filesScanned:N0}");
                }
                else
                {
                    lines.Add("│ Starting...");
                }

                // Line 3: Detail message
                if (!string.IsNullOrEmpty(_currentDetail))
                {
                    string detail = _currentDetail;
                    int maxWidth = Console.WindowWidth - 4;
                    if (detail.Length > maxWidth)
                    {
                        detail = detail.Substring(0, maxWidth - 3) + "...";
                    }
                    lines.Add($"│ {detail}");
                }
                else
                {
                    lines.Add("│");
                }

                // Line 4: Bottom border with ETA (if applicable)
                if (_totalFiles > 0 && _filesProcessed > 0 && elapsed.TotalSeconds > 1)
                {
                    double filesPerSecond = _filesProcessed / elapsed.TotalSeconds;
                    double remainingFiles = _totalFiles - _filesProcessed;
                    double etaSeconds = remainingFiles / filesPerSecond;

                    string etaStr = etaSeconds < 60
                        ? $"{etaSeconds:F0}s"
                        : etaSeconds < 3600
                            ? $"{etaSeconds / 60:F1}m"
                            : $"{etaSeconds / 3600:F1}h";

                    lines.Add($"└─ ETA: {etaStr} ({filesPerSecond:F0} files/s)");
                }
                else
                {
                    lines.Add("└─");
                }

                // Clear and write lines
                for (int i = 0; i < lines.Count; i++)
                {
                    Console.SetCursorPosition(0, originalTop + i);

                    // Pad line to clear previous content
                    string line = lines[i];
                    if (line.Length < Console.WindowWidth - 1)
                    {
                        line += new string(' ', Console.WindowWidth - 1 - line.Length);
                    }

                    Console.Write(line);
                }

                // Restore cursor position
                Console.SetCursorPosition(originalLeft, originalTop + lines.Count);
            }
            catch (Exception)
            {
                // Ignore rendering errors (e.g., if console is resized)
            }
        }
    }
}
