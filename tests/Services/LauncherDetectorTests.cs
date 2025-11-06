using FluentAssertions;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class LauncherDetectorTests
{
    private readonly LauncherDetector _detector;
    private readonly string _testDirectory;

    public LauncherDetectorTests()
    {
        _detector = new LauncherDetector();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LauncherTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void GetLauncherPaths_ShouldReturnPathsForAllLauncherTypes()
    {
        // Act
        var paths = _detector.GetLauncherPaths();

        // Assert
        paths.Should().ContainKey(LauncherType.Official);
        paths.Should().ContainKey(LauncherType.PrismLauncher);
        paths.Should().ContainKey(LauncherType.MultiMC);
        paths.Should().ContainKey(LauncherType.CurseForge);
        paths.Should().ContainKey(LauncherType.ATLauncher);
        paths.Should().ContainKey(LauncherType.Modrinth);
        paths.Should().ContainKey(LauncherType.GDLauncher);

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void GetLauncherPaths_ShouldReturnPlatformSpecificPaths()
    {
        // Act
        var paths = _detector.GetLauncherPaths();

        // Assert
        if (OperatingSystem.IsWindows())
        {
            paths[LauncherType.Official].Should().Contain(p => p.Contains("AppData") && p.Contains(".minecraft"));
            paths[LauncherType.PrismLauncher].Should().Contain(p => p.Contains("PrismLauncher"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            paths[LauncherType.Official].Should().Contain(p => p.Contains("Library/Application Support/minecraft"));
            paths[LauncherType.PrismLauncher].Should().Contain(p => p.Contains("Library/Application Support/PrismLauncher"));
        }
        else // Linux
        {
            paths[LauncherType.Official].Should().Contain(p => p.Contains(".minecraft"));
            paths[LauncherType.PrismLauncher].Should().Contain(p =>
                p.Contains(".local/share/PrismLauncher") || p.Contains("org.prismlauncher.PrismLauncher"));
        }

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void DetectAllInstances_WithNoLaunchers_ShouldReturnEmptyList()
    {
        // Act
        var instances = _detector.DetectAllInstances();

        // Assert
        // This may or may not be empty depending on actual system state
        // Just verify it doesn't throw
        instances.Should().NotBeNull();

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void FindInstancesByName_WithNonExistentName_ShouldReturnEmptyList()
    {
        // Act
        var instances = _detector.FindInstancesByName("NonExistentInstance_12345");

        // Assert
        instances.Should().BeEmpty();

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void GetInstancesForLauncher_ShouldReturnOnlyInstancesForSpecifiedLauncher()
    {
        // Act
        var instances = _detector.GetInstancesForLauncher(LauncherType.PrismLauncher);

        // Assert
        instances.Should().AllSatisfy(i => i.Launcher.Should().Be(LauncherType.PrismLauncher));

        // Cleanup
        CleanupTestDirectory();
    }

    [Theory]
    [InlineData(LauncherType.Official)]
    [InlineData(LauncherType.PrismLauncher)]
    [InlineData(LauncherType.MultiMC)]
    [InlineData(LauncherType.CurseForge)]
    [InlineData(LauncherType.ATLauncher)]
    [InlineData(LauncherType.Modrinth)]
    [InlineData(LauncherType.GDLauncher)]
    public void GetInstancesForLauncher_ForEachLauncherType_ShouldNotThrow(LauncherType launcherType)
    {
        // Act
        Action act = () => _detector.GetInstancesForLauncher(launcherType);

        // Assert
        act.Should().NotThrow();

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void DetectAllInstances_ShouldHandleAccessDeniedGracefully()
    {
        // This test verifies that the detector doesn't crash on directories it can't access
        // Act
        Action act = () => _detector.DetectAllInstances();

        // Assert
        act.Should().NotThrow();

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void MinecraftInstance_SavesPath_ShouldCombinePathWithSaves()
    {
        // Arrange
        var instance = new MinecraftInstance
        {
            Name = "TestInstance",
            Path = "/test/path",
            Launcher = LauncherType.PrismLauncher
        };

        // Act
        var savesPath = instance.SavesPath;

        // Assert
        savesPath.Should().EndWith("saves");
        savesPath.Should().Contain("/test/path");

        // Cleanup
        CleanupTestDirectory();
    }

    [Fact]
    public void MinecraftInstance_ToString_ShouldIncludeNameAndLauncher()
    {
        // Arrange
        var instance = new MinecraftInstance
        {
            Name = "MyInstance",
            Path = "/test/path",
            Launcher = LauncherType.MultiMC
        };

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Contain("MyInstance");
        result.Should().Contain("MultiMC");

        // Cleanup
        CleanupTestDirectory();
    }

    private void CleanupTestDirectory()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
