using FluentAssertions;
using SaplingFS.Models;

namespace SaplingFS.Tests.Models;

public class MappedFileTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var file = new MappedFile("/home/user/documents/file.txt", 1024, 2);

        // Assert
        file.Path.Should().Be("/home/user/documents/file.txt");
        file.Size.Should().Be(1024);
        file.Depth.Should().Be(2);
    }

    [Theory]
    [InlineData("/home/user/documents/file.txt", 2, "/home/user")]
    [InlineData("C:\\Users\\John\\Documents\\file.txt", 2, "C:\\Users\\John")]
    [InlineData("/var/log/system.log", 1, "/var")]
    [InlineData("/file.txt", 0, "")]
    public void GetShortParent_ShouldReturnCorrectParentPath(string path, int depth, string expected)
    {
        // Arrange
        var file = new MappedFile(path, 100, depth);

        // Act
        var parent = file.GetShortParent(depth);

        // Assert
        parent.Should().Be(expected);
    }

    [Fact]
    public void GetShortParent_DepthGreaterThanActual_ShouldReturnFullParent()
    {
        // Arrange
        var file = new MappedFile("/home/user/file.txt", 100, 1);

        // Act
        var parent = file.GetShortParent(5);

        // Assert
        parent.Should().Be("/home/user");
    }

    [Theory]
    [InlineData("/home/user/documents/file.txt", 2, "/home/user/.../file.txt")]
    [InlineData("C:\\Users\\John\\Documents\\file.txt", 2, "C:\\Users\\John/.../file.txt")]
    [InlineData("/var/log/system.log", 1, "/var/.../system.log")]
    public void GetShortPath_ShouldReturnAbbreviatedPath(string path, int depth, string expected)
    {
        // Arrange
        var file = new MappedFile(path, 100, depth);

        // Act
        var shortPath = file.GetShortPath(depth);

        // Assert
        shortPath.Should().Be(expected);
    }

    [Fact]
    public void GetShortPath_FileInRoot_ShouldReturnFileName()
    {
        // Arrange
        var file = new MappedFile("/file.txt", 100, 0);

        // Act
        var shortPath = file.GetShortPath(0);

        // Assert
        shortPath.Should().Be("file.txt");
    }

    [Fact]
    public void ToString_ShouldReturnPathAndSize()
    {
        // Arrange
        var file = new MappedFile("/home/user/file.txt", 2048, 2);

        // Act
        var str = file.ToString();

        // Assert
        str.Should().Be("/home/user/file.txt (2048 bytes)");
    }

    [Fact]
    public void Equality_SamePathAndSize_ShouldBeEqual()
    {
        // Arrange
        var file1 = new MappedFile("/home/user/file.txt", 1024, 2);
        var file2 = new MappedFile("/home/user/file.txt", 1024, 2);

        // Act & Assert
        file1.Should().Be(file2);
    }

    [Fact]
    public void Equality_DifferentPath_ShouldNotBeEqual()
    {
        // Arrange
        var file1 = new MappedFile("/home/user/file1.txt", 1024, 2);
        var file2 = new MappedFile("/home/user/file2.txt", 1024, 2);

        // Act & Assert
        file1.Should().NotBe(file2);
    }

    [Fact]
    public void Immutability_OriginalShouldNotChangeWhenCreatingNewInstance()
    {
        // Arrange
        var file = new MappedFile("/home/user/file.txt", 1024, 2);

        // Act
        var modified = new MappedFile(file.Path, 2048, file.Depth);

        // Assert
        modified.Path.Should().Be("/home/user/file.txt");
        modified.Size.Should().Be(2048);
        modified.Depth.Should().Be(2);
        file.Size.Should().Be(1024); // Original unchanged
    }
}
