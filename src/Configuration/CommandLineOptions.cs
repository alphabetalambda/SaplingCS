namespace SaplingFS.Configuration;

/// <summary>
/// Command-line options for SaplingFS.
/// </summary>
public class CommandLineOptions
{
    public string WorldName { get; set; } = string.Empty;
    public bool Debug { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public int ParentDepth { get; set; }
    public bool NoProgress { get; set; }
    public List<string> Blacklist { get; set; } = [];
    public bool AllowDelete { get; set; }

    /// <summary>
    /// Parses command-line arguments into a CommandLineOptions object.
    /// </summary>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        // First argument is world name
        if (args.Length > 0 && !args[0].StartsWith("--"))
        {
            options.WorldName = args[0];
        }

        // Platform-specific defaults
        var isWindows = OperatingSystem.IsWindows();
        options.RootPath = isWindows ? "C:\\" : "/";
        options.ParentDepth = isWindows ? 2 : 3;

        // Parse flags and options
        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--debug":
                    options.Debug = true;
                    break;

                case "--no-progress":
                    options.NoProgress = true;
                    break;

                case "--path":
                    if (i + 1 < args.Length)
                    {
                        options.RootPath = args[++i];
                    }
                    break;

                case "--depth":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var depth))
                    {
                        options.ParentDepth = depth;
                    }
                    break;

                case "--blacklist":
                    if (i + 1 < args.Length)
                    {
                        options.Blacklist = args[++i].Split(';').ToList();
                    }
                    break;

                case "--allow-delete":
                    if (i + 1 < args.Length)
                    {
                        var timeString = DateTime.Now.ToString("HH:mm");
                        options.AllowDelete = args[++i] == timeString;
                    }
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Validates the parsed options and returns true if valid.
    /// </summary>
    public bool Validate()
    {
        return !string.IsNullOrEmpty(WorldName) &&
               !string.IsNullOrEmpty(RootPath) &&
               ParentDepth > 0;
    }

    /// <summary>
    /// Prints usage information to the console.
    /// </summary>
    public static void PrintUsage()
    {
        Console.Error.WriteLine(@"Usage: SaplingFS <world> [options]

Options:
    --debug                 Generates colorful terrain to help debug directory grouping.
    --path <string>         Root path from which to look for files.
    --depth <number>        Depth from absolute root at which to split directory groups.
    --no-progress           Don't save/load current world progress to/from disk.
    --blacklist <path;...>  Semicolon-separated paths to blacklist from the scan.

    --allow-delete <hh:mm>  Enables actually deleting files when blocks are altered.
                            For confirmation, requires current system time in 24h format.
                            WARNING: THIS WILL IRREVERSIBLY DELETE FILES ON YOUR SYSTEM.");
    }
}
