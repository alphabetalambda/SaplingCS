using FluentAssertions;
using SaplingFS.Models;

namespace SaplingFS.Tests.Models;

public class BlockMappingTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var position = new Vector(10, 64, 20);
        var filePath = "/home/user/file.txt";
        var block = "grass_block";

        // Act
        var mapping = new BlockMapping(position, filePath, block);

        // Assert
        mapping.Position.Should().Be(position);
        mapping.FilePath.Should().Be(filePath);
        mapping.Block.Should().Be(block);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var mapping = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );

        // Act
        var str = mapping.ToString();

        // Assert
        str.Should().Be("\"grass_block\" at (10,64,20): \"/home/user/file.txt\"");
    }

    [Fact]
    public void With_ShouldAllowModifyingBlock()
    {
        // Arrange
        var original = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );

        // Act
        var modified = original with { Block = "dirt" };

        // Assert
        modified.Position.Should().Be(original.Position);
        modified.FilePath.Should().Be(original.FilePath);
        modified.Block.Should().Be("dirt");
        original.Block.Should().Be("grass_block"); // Original unchanged
    }

    [Fact]
    public void With_ShouldAllowModifyingPosition()
    {
        // Arrange
        var original = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );

        // Act
        var modified = original with { Position = new Vector(15, 65, 25) };

        // Assert
        modified.Position.Should().Be(new Vector(15, 65, 25));
        modified.FilePath.Should().Be(original.FilePath);
        modified.Block.Should().Be(original.Block);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var mapping1 = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );
        var mapping2 = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );

        // Act & Assert
        mapping1.Should().Be(mapping2);
    }

    [Fact]
    public void Equality_DifferentBlock_ShouldNotBeEqual()
    {
        // Arrange
        var mapping1 = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );
        var mapping2 = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "dirt"
        );

        // Act & Assert
        mapping1.Should().NotBe(mapping2);
    }

    [Fact]
    public void Immutability_ShouldNotAllowDirectModification()
    {
        // Arrange
        var mapping = new BlockMapping(
            new Vector(10, 64, 20),
            "/home/user/file.txt",
            "grass_block"
        );

        // Act - Create modified version
        var modified = mapping with { Block = "stone" };

        // Assert - Original should be unchanged
        mapping.Block.Should().Be("grass_block");
        modified.Block.Should().Be("stone");
        mapping.Should().NotBeSameAs(modified);
    }
}
