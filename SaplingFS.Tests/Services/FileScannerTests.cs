using FluentAssertions;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class FileScannerTests
{
    private readonly string _testDirectory;

    public FileScannerTests()
    {
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaplingFSTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void BuildFileList_EmptyDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var scanner = new FileScanner();

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().BeEmpty();

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_WithFiles_ShouldReturnFileList()
    {
        // Arrange
        var scanner = new FileScanner();
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "test content 1");
        File.WriteAllText(file2, "test content 2");

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Path == file1);
        result.Should().Contain(f => f.Path == file2);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_WithNestedDirectories_ShouldScanRecursively()
    {
        // Arrange
        var scanner = new FileScanner();
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(subDir, "file2.txt");
        File.WriteAllText(file1, "test");
        File.WriteAllText(file2, "test");

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Path == file1);
        result.Should().Contain(f => f.Path == file2);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_WithEmptyFiles_ShouldIgnoreThem()
    {
        // Arrange
        var scanner = new FileScanner();
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(file1, "content");
        File.WriteAllText(file2, "");

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be(file1);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_WithBlacklist_ShouldSkipBlacklistedPaths()
    {
        // Arrange
        var scanner = new FileScanner();
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(subDir, "file2.txt");
        File.WriteAllText(file1, "test");
        File.WriteAllText(file2, "test");

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string> { subDir });

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be(file1);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_WithCacheInName_ShouldSkipDirectory()
    {
        // Arrange
        var scanner = new FileScanner();
        var cacheDir = Path.Combine(_testDirectory, "cache");
        Directory.CreateDirectory(cacheDir);
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(cacheDir, "cached.txt");
        File.WriteAllText(file1, "test");
        File.WriteAllText(file2, "test");

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be(file1);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void BuildFileList_ShouldSetCorrectFileSize()
    {
        // Arrange
        var scanner = new FileScanner();
        var file = Path.Combine(_testDirectory, "file.txt");
        var content = "This is test content";
        File.WriteAllText(file, content);

        // Act
        var result = scanner.BuildFileList(_testDirectory, new List<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Size.Should().Be(content.Length);

        // Cleanup
        Directory.Delete(_testDirectory, true);
    }
}
