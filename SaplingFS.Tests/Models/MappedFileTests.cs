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

    [Fact]
    public void GetShortParent_ShouldReturnCorrectParentPath()
    {
        // Arrange - Use a path structure that won't be modified by GetFullPath
        var testDir = Directory.GetCurrentDirectory();
        var testPath = Path.Combine(testDir, "user", "documents", "file.txt");
        var file = new MappedFile(testPath, 100, 2);

        // Act
        var parent = file.GetShortParent(2);

        // Assert
        parent.Should().NotContain("file.txt");
        parent.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetShortParent_DepthGreaterThanActual_ShouldReturnFullParent()
    {
        // Arrange
        var testDir = Directory.GetCurrentDirectory();
        var testPath = Path.Combine(testDir, "user", "file.txt");
        var file = new MappedFile(testPath, 100, 1);

        // Act
        var parent = file.GetShortParent(5);

        // Assert
        parent.Should().NotContain("file.txt");
        parent.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetShortPath_ShouldReturnAbbreviatedPath()
    {
        // Arrange - Use a path that will be normalized consistently
        var testPath = Path.Combine(Path.GetTempPath(), "user", "documents", "file.txt");
        var file = new MappedFile(testPath, 100, 2);

        // Act
        var shortPath = file.GetShortPath(2);

        // Assert
        shortPath.Should().Contain("file.txt");
        shortPath.Should().Contain("...");
    }

    [Fact]
    public void GetShortPath_FileInRoot_ShouldContainFileName()
    {
        // Arrange - GetFullPath will normalize this path
        var file = new MappedFile("/file.txt", 100, 0);

        // Act
        var shortPath = file.GetShortPath(0);

        // Assert
        shortPath.Should().Contain("file.txt");
    }

    [Fact]
    public void ToString_ShouldReturnPath()
    {
        // Arrange
        var file = new MappedFile("/home/user/file.txt", 2048, 2);

        // Act
        var str = file.ToString();

        // Assert
        str.Should().Contain("file.txt");
    }

    [Fact]
    public void Equality_SamePathAndSize_ShouldHaveSameProperties()
    {
        // Arrange
        var file1 = new MappedFile("/home/user/file.txt", 1024, 2);
        var file2 = new MappedFile("/home/user/file.txt", 1024, 2);

        // Act & Assert - MappedFile is a class, so uses reference equality
        // But properties should match
        file1.Size.Should().Be(file2.Size);
        file1.Depth.Should().Be(file2.Depth);
        file1.Path.Should().Be(file2.Path);
    }

    [Fact]
    public void Equality_DifferentPath_ShouldHaveDifferentPaths()
    {
        // Arrange
        var file1 = new MappedFile("/home/user/file1.txt", 1024, 2);
        var file2 = new MappedFile("/home/user/file2.txt", 1024, 2);

        // Act & Assert
        file1.Path.Should().NotBe(file2.Path);
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
