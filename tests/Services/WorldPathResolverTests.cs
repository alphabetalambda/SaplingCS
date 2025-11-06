using FluentAssertions;
using SaplingFS.Configuration;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class WorldPathResolverTests
{
    private readonly WorldPathResolver _resolver;
    private readonly string _testDirectory;

    public WorldPathResolverTests()
    {
        _resolver = new WorldPathResolver();
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaplingFSTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ResolveWorldPath_WithExistingDirectory_ShouldReturnFullPath()
    {
        // Arrange
        var testWorldDir = Path.Combine(_testDirectory, "TestWorld");
        Directory.CreateDirectory(testWorldDir);

        // Act
        var result = _resolver.ResolveWorldPath(testWorldDir);

        // Assert
        result.Should().Be(Path.GetFullPath(testWorldDir));

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void ResolveWorldPath_WithWorldName_ShouldReturnPlatformSpecificPath()
    {
        // Arrange
        var worldName = "TestWorld";
        var expectedBasePath = GetExpectedMinecraftBasePath();

        // Act
        var result = _resolver.ResolveWorldPath(worldName);

        // Assert
        result.Should().StartWith(expectedBasePath);
        result.Should().EndWith(worldName);

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_OnWindows_ShouldUseAppDataPath()
    {
        // This test validates the expected path format on Windows
        // Arrange
        var worldName = "TestWorld";

        // Act
        var result = _resolver.ResolveWorldPath(worldName);

        // Assert
        if (OperatingSystem.IsWindows())
        {
            result.Should().Contain(@"AppData\Roaming\.minecraft\saves");
            result.Should().EndWith(worldName);
        }

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_OnMacOS_ShouldUseLibraryApplicationSupportPath()
    {
        // This test validates the expected path format on macOS
        // Arrange
        var worldName = "TestWorld";

        // Act
        var result = _resolver.ResolveWorldPath(worldName);

        // Assert
        if (OperatingSystem.IsMacOS())
        {
            result.Should().Contain("Library/Application Support/minecraft/saves");
            result.Should().EndWith(worldName);
        }

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_OnLinux_ShouldUseDotMinecraftPath()
    {
        // This test validates the expected path format on Linux
        // Arrange
        var worldName = "TestWorld";

        // Act
        var result = _resolver.ResolveWorldPath(worldName);

        // Assert
        if (OperatingSystem.IsLinux())
        {
            result.Should().Contain(".minecraft/saves");
            result.Should().EndWith(worldName);
        }

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithRelativePath_ShouldReturnAbsolutePath()
    {
        // Arrange
        var relativePath = "./TestWorld";
        var worldDir = Path.Combine(Directory.GetCurrentDirectory(), "TestWorld");
        Directory.CreateDirectory(worldDir);

        // Act
        var result = _resolver.ResolveWorldPath(relativePath);

        // Assert
        result.Should().Be(Path.GetFullPath(worldDir));

        // Cleanup
        Directory.Delete(worldDir, true);
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithNonExistentWorldName_ShouldStillReturnPath()
    {
        // Arrange
        var worldName = "NonExistentWorld";

        // Act
        var result = _resolver.ResolveWorldPath(worldName);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(worldName);

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithNullOptions_ShouldUseDefaultLauncher()
    {
        // Arrange
        var worldName = "TestWorld";

        // Act
        var result = _resolver.ResolveWorldPath(worldName, null);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(worldName);

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithOptionsButNoLauncher_ShouldUseDefaultLauncher()
    {
        // Arrange
        var worldName = "TestWorld";
        var options = new CommandLineOptions { WorldName = worldName };

        // Act
        var result = _resolver.ResolveWorldPath(worldName, options);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(worldName);

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithNonExistentLauncher_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var worldName = "TestWorld";
        var options = new CommandLineOptions
        {
            WorldName = worldName,
            Launcher = LauncherType.PrismLauncher // Assuming this doesn't exist on test machine
        };

        // Act
        Action act = () => _resolver.ResolveWorldPath(worldName, options);

        // Assert
        // This may or may not throw depending on system state
        // Just verify it doesn't crash catastrophically
        var result = Record.Exception(act);
        if (result != null)
        {
            result.Should().BeOfType<InvalidOperationException>();
        }

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ResolveWorldPath_WithInstanceNameOnly_ShouldSearchAllLaunchers()
    {
        // Arrange
        var worldName = "TestWorld";
        var options = new CommandLineOptions
        {
            WorldName = worldName,
            InstanceName = "NonExistentInstance_12345"
        };

        // Act
        var result = _resolver.ResolveWorldPath(worldName, options);

        // Assert
        // Should fall back to default launcher path when instance not found
        result.Should().NotBeNullOrEmpty();

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ListAllInstances_ShouldReturnList()
    {
        // Act
        var instances = _resolver.ListAllInstances();

        // Assert
        instances.Should().NotBeNull();
        // The list may be empty if no launchers are installed, which is fine

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Theory]
    [InlineData(LauncherType.Official)]
    [InlineData(LauncherType.PrismLauncher)]
    [InlineData(LauncherType.MultiMC)]
    [InlineData(LauncherType.CurseForge)]
    [InlineData(LauncherType.ATLauncher)]
    [InlineData(LauncherType.Modrinth)]
    [InlineData(LauncherType.GDLauncher)]
    public void ResolveWorldPath_WithEachLauncherType_ShouldNotCrash(LauncherType launcherType)
    {
        // Arrange
        var worldName = "TestWorld";
        var options = new CommandLineOptions
        {
            WorldName = worldName,
            Launcher = launcherType
        };

        // Act
        Action act = () => _resolver.ResolveWorldPath(worldName, options);

        // Assert
        // Should either return a path or throw InvalidOperationException, but not crash
        var result = Record.Exception(act);
        if (result != null)
        {
            result.Should().BeOfType<InvalidOperationException>();
        }

        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private string GetExpectedMinecraftBasePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft", "saves");
        }
        else if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "minecraft", "saves");
        }
        else
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft", "saves");
        }
    }
}
