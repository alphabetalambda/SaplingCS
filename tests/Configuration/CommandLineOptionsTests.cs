using FluentAssertions;
using SaplingFS.Configuration;
using SaplingFS.Models;

namespace SaplingFS.Tests.Configuration;

public class CommandLineOptionsTests
{
    [Fact]
    public void Parse_WithLauncherOption_ShouldSetLauncherType()
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher", "PrismLauncher" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().Be(LauncherType.PrismLauncher);
    }

    [Fact]
    public void Parse_WithInstanceOption_ShouldSetInstanceName()
    {
        // Arrange
        var args = new[] { "MyWorld", "--instance", "MyModpack" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.InstanceName.Should().Be("MyModpack");
    }

    [Fact]
    public void Parse_WithBothLauncherAndInstance_ShouldSetBoth()
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher", "MultiMC", "--instance", "TestInstance" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().Be(LauncherType.MultiMC);
        options.InstanceName.Should().Be("TestInstance");
    }

    [Theory]
    [InlineData("Official", LauncherType.Official)]
    [InlineData("PrismLauncher", LauncherType.PrismLauncher)]
    [InlineData("MultiMC", LauncherType.MultiMC)]
    [InlineData("CurseForge", LauncherType.CurseForge)]
    [InlineData("ATLauncher", LauncherType.ATLauncher)]
    [InlineData("Modrinth", LauncherType.Modrinth)]
    [InlineData("GDLauncher", LauncherType.GDLauncher)]
    public void Parse_WithValidLauncherNames_ShouldParseCorrectly(string launcherName, LauncherType expectedType)
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher", launcherName };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().Be(expectedType);
    }

    [Fact]
    public void Parse_WithCaseInsensitiveLauncher_ShouldParse()
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher", "prismlauncher" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().Be(LauncherType.PrismLauncher);
    }

    [Fact]
    public void Parse_WithInvalidLauncherName_ShouldNotSetLauncher()
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher", "InvalidLauncher" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().BeNull();
    }

    [Fact]
    public void Parse_WithoutLauncherOption_ShouldHaveNullLauncher()
    {
        // Arrange
        var args = new[] { "MyWorld" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().BeNull();
    }

    [Fact]
    public void Parse_WithoutInstanceOption_ShouldHaveNullInstance()
    {
        // Arrange
        var args = new[] { "MyWorld" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.InstanceName.Should().BeNull();
    }

    [Fact]
    public void Parse_WithAllOptions_ShouldParseEverything()
    {
        // Arrange
        var args = new[]
        {
            "MyWorld",
            "--debug",
            "--path", "/test/path",
            "--depth", "3",
            "--launcher", "PrismLauncher",
            "--instance", "Modded",
            "--no-progress",
            "--blacklist", "/exclude/this;/and/this"
        };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.WorldName.Should().Be("MyWorld");
        options.Debug.Should().BeTrue();
        options.RootPath.Should().Be("/test/path");
        options.ParentDepth.Should().Be(3);
        options.Launcher.Should().Be(LauncherType.PrismLauncher);
        options.InstanceName.Should().Be("Modded");
        options.NoProgress.Should().BeTrue();
        options.Blacklist.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WithLauncherButNoValue_ShouldNotSetLauncher()
    {
        // Arrange
        var args = new[] { "MyWorld", "--launcher" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.Launcher.Should().BeNull();
    }

    [Fact]
    public void Parse_WithInstanceButNoValue_ShouldNotSetInstance()
    {
        // Arrange
        var args = new[] { "MyWorld", "--instance" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.InstanceName.Should().BeNull();
    }

    [Fact]
    public void Parse_WithInstanceNameContainingSpaces_ShouldPreserveSpaces()
    {
        // Arrange
        var args = new[] { "MyWorld", "--instance", "My Modpack Instance" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        options.InstanceName.Should().Be("My Modpack Instance");
    }

    [Fact]
    public void Validate_WithValidWorldName_ShouldReturnTrue()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            WorldName = "TestWorld",
            RootPath = "/test",
            ParentDepth = 2
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyWorldName_ShouldReturnFalse()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            WorldName = "",
            RootPath = "/test",
            ParentDepth = 2
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PrintUsage_ShouldNotThrow()
    {
        // Act
        Action act = () => CommandLineOptions.PrintUsage();

        // Assert
        act.Should().NotThrow();
    }
}
