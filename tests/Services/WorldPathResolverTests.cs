using FluentAssertions;
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
