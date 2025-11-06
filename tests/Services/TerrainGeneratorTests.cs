using FluentAssertions;
using NSubstitute;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class TerrainGeneratorTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithEmptyMapping()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();

        // Act
        var generator = new TerrainGenerator(worldParser);

        // Assert
        generator.Mapping.Should().BeEmpty();
    }

    [Fact]
    public void TerrainBounds_InitialState_ShouldBeZero()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);

        // Act
        var bounds = generator.TerrainBounds;

        // Assert
        bounds.Min.Should().Be(new Vector(0, 0, 0));
        bounds.Max.Should().Be(new Vector(0, 0, 0));
    }

    [Fact]
    public async Task BuildRegionDataAsync_WithEmptyFileList_ShouldNotCrash()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var fileList = new List<MappedFile>();
        var worldPath = Path.GetTempPath();

        // Act
        var act = async () => await generator.BuildRegionDataAsync(fileList, 2, worldPath, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BuildRegionDataAsync_WithSingleFile_ShouldCreateMapping()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var file = new MappedFile("/test/file.txt", 100, 2);
        var fileList = new List<MappedFile> { file };
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(fileList, 2, worldPath, false);

            // Assert
            generator.Mapping.Should().NotBeEmpty();
            generator.Mapping.Values.Should().Contain(m => m.FilePath == file.Path);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public async Task BuildRegionDataAsync_DebugMode_ShouldUseDebugPalette()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var file = new MappedFile("/test/file.txt", 100, 2);
        var fileList = new List<MappedFile> { file };
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(fileList, 2, worldPath, debug: true);

            // Assert
            generator.Mapping.Should().NotBeEmpty();
            var firstBlock = generator.Mapping.Values.First().Block;
            firstBlock.Should().EndWith("_wool"); // Debug palette uses wool blocks
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public async Task BuildRegionDataAsync_MultipleFilesFromSameParent_ShouldGroupTogether()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var files = new List<MappedFile>
        {
            new MappedFile("/home/user/file1.txt", 100, 2),
            new MappedFile("/home/user/file2.txt", 100, 2),
            new MappedFile("/home/user/file3.txt", 100, 2)
        };
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(files, 2, worldPath, debug: true);

            // Assert
            generator.Mapping.Should().HaveCount(3);
            // All should use same debug color (same terrain group)
            var blocks = generator.Mapping.Values.Select(m => m.Block).Distinct();
            blocks.Should().HaveCount(1);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public async Task BuildRegionDataAsync_MultipleFilesFromDifferentParents_ShouldCreateSeparateGroups()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var files = new List<MappedFile>
        {
            new MappedFile("/home/user1/file1.txt", 100, 2),
            new MappedFile("/home/user2/file2.txt", 100, 2)
        };
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(files, 2, worldPath, debug: true);

            // Assert
            generator.Mapping.Should().HaveCount(2);
            // Should use different debug colors (different terrain groups)
            var blocks = generator.Mapping.Values.Select(m => m.Block).Distinct();
            blocks.Should().HaveCountGreaterThan(1);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public async Task BuildRegionDataAsync_ShouldUpdateTerrainBounds()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var file = new MappedFile("/test/file.txt", 100, 2);
        var fileList = new List<MappedFile> { file };
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(fileList, 2, worldPath, false);

            // Assert
            var bounds = generator.TerrainBounds;
            // Bounds should have changed from initial (0,0,0)
            // At minimum, one of Min or Max should be non-zero
            var hasNonZeroBounds = bounds.Min != new Vector(0, 0, 0) || bounds.Max != new Vector(0, 0, 0);
            hasNonZeroBounds.Should().BeTrue("terrain bounds should be updated after generation");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public async Task BuildRegionDataAsync_WithManyFiles_ShouldCreateContiguousTerrain()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var files = Enumerable.Range(0, 10)
            .Select(i => new MappedFile($"/test/file{i}.txt", 100, 2))
            .ToList();
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(files, 2, worldPath, false);

            // Assert
            // Terrain generator may create slightly more blocks than input files due to smoothing
            generator.Mapping.Should().HaveCountGreaterThanOrEqualTo(10);
            generator.Mapping.Should().HaveCountLessThanOrEqualTo(15); // Allow some extra blocks

            // Check that blocks are relatively close to each other (contiguous terrain)
            var positions = generator.Mapping.Values.Select(m => m.Position).ToList();
            var minX = positions.Min(p => p.X);
            var maxX = positions.Max(p => p.X);
            var minZ = positions.Min(p => p.Z);
            var maxZ = positions.Max(p => p.Z);

            // Terrain should not be too spread out for 10 blocks
            (maxX - minX).Should().BeLessThan(50);
            (maxZ - minZ).Should().BeLessThan(50);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }

    [Fact]
    public void Mapping_ShouldBeReadOnly()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);

        // Act
        var mapping = generator.Mapping;

        // Assert
        mapping.Should().BeAssignableTo<IReadOnlyDictionary<string, BlockMapping>>();
    }

    [Fact]
    public async Task BuildRegionDataAsync_BlockPositions_ShouldBeWithinWorldBounds()
    {
        // Arrange
        var worldParser = Substitute.For<WorldParser>();
        var generator = new TerrainGenerator(worldParser);
        var files = Enumerable.Range(0, 5)
            .Select(i => new MappedFile($"/test/file{i}.txt", 100, 2))
            .ToList();
        var worldPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(worldPath, "region"));

        try
        {
            // Act
            await generator.BuildRegionDataAsync(files, 2, worldPath, false);

            // Assert
            var positions = generator.Mapping.Values.Select(m => m.Position);
            positions.Should().AllSatisfy(pos =>
            {
                pos.X.Should().BeGreaterThanOrEqualTo(-16 * 20);
                pos.X.Should().BeLessThanOrEqualTo(16 * 20);
                pos.Y.Should().BeGreaterThanOrEqualTo(-64);
                pos.Y.Should().BeLessThanOrEqualTo(320);
                pos.Z.Should().BeGreaterThanOrEqualTo(-16 * 20);
                pos.Z.Should().BeLessThanOrEqualTo(16 * 20);
            });
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(worldPath))
                Directory.Delete(worldPath, true);
        }
    }
}
