using FluentAssertions;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class InstanceSelectorTests
{
    private readonly InstanceSelector _selector;

    public InstanceSelectorTests()
    {
        _selector = new InstanceSelector();
    }

    [Fact]
    public void AutoSelectOrPrompt_WithEmptyList_ShouldReturnNull()
    {
        // Arrange
        var instances = new List<MinecraftInstance>();

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, null, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AutoSelectOrPrompt_WithSingleInstance_ShouldReturnThatInstance()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "TestInstance",
                Path = "/test/path",
                Launcher = LauncherType.PrismLauncher
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, null, null);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("TestInstance");
    }

    [Fact]
    public void AutoSelectOrPrompt_WithPreferredLauncher_ShouldFilterByLauncher()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "PrismInstance",
                Path = "/prism/path",
                Launcher = LauncherType.PrismLauncher
            },
            new MinecraftInstance
            {
                Name = "MultiMCInstance",
                Path = "/multimc/path",
                Launcher = LauncherType.MultiMC
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, LauncherType.PrismLauncher, null);

        // Assert
        result.Should().NotBeNull();
        result!.Launcher.Should().Be(LauncherType.PrismLauncher);
        result.Name.Should().Be("PrismInstance");
    }

    [Fact]
    public void AutoSelectOrPrompt_WithPreferredInstanceName_ShouldFilterByName()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "Vanilla",
                Path = "/vanilla/path",
                Launcher = LauncherType.PrismLauncher
            },
            new MinecraftInstance
            {
                Name = "Modded",
                Path = "/modded/path",
                Launcher = LauncherType.PrismLauncher
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, null, "Vanilla");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Vanilla");
    }

    [Fact]
    public void AutoSelectOrPrompt_WithBothFilters_ShouldFilterByBoth()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "TestInstance",
                Path = "/prism/path",
                Launcher = LauncherType.PrismLauncher
            },
            new MinecraftInstance
            {
                Name = "TestInstance",
                Path = "/multimc/path",
                Launcher = LauncherType.MultiMC
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, LauncherType.MultiMC, "TestInstance");

        // Assert
        result.Should().NotBeNull();
        result!.Launcher.Should().Be(LauncherType.MultiMC);
        result.Name.Should().Be("TestInstance");
    }

    [Fact]
    public void AutoSelectOrPrompt_WithNonMatchingFilters_ShouldReturnNull()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "PrismInstance",
                Path = "/prism/path",
                Launcher = LauncherType.PrismLauncher
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, LauncherType.MultiMC, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AutoSelectOrPrompt_WithCaseInsensitiveInstanceName_ShouldMatch()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "MyInstance",
                Path = "/test/path",
                Launcher = LauncherType.PrismLauncher
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, null, "myinstance");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("MyInstance");
    }

    [Fact]
    public void SelectInstance_WithEmptyList_ShouldThrowException()
    {
        // Arrange
        var instances = new List<MinecraftInstance>();

        // Act
        Action act = () => _selector.SelectInstance(instances);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No instances provided to select from");
    }

    [Fact]
    public void SelectInstance_WithSingleInstance_ShouldReturnThatInstance()
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "OnlyInstance",
                Path = "/test/path",
                Launcher = LauncherType.Official
            }
        };

        // Act
        var result = _selector.SelectInstance(instances);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("OnlyInstance");
    }

    [Theory]
    [InlineData(LauncherType.Official)]
    [InlineData(LauncherType.PrismLauncher)]
    [InlineData(LauncherType.MultiMC)]
    [InlineData(LauncherType.CurseForge)]
    [InlineData(LauncherType.ATLauncher)]
    [InlineData(LauncherType.Modrinth)]
    [InlineData(LauncherType.GDLauncher)]
    public void AutoSelectOrPrompt_WithEachLauncherType_ShouldWork(LauncherType launcherType)
    {
        // Arrange
        var instances = new List<MinecraftInstance>
        {
            new MinecraftInstance
            {
                Name = "TestInstance",
                Path = "/test/path",
                Launcher = launcherType
            }
        };

        // Act
        var result = _selector.AutoSelectOrPrompt(instances, launcherType, null);

        // Assert
        result.Should().NotBeNull();
        result!.Launcher.Should().Be(launcherType);
    }
}
