using FluentAssertions;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class RaycastServiceTests
{
    [Fact]
    public void Raycast_EmptyMapping_ShouldReturnNull()
    {
        // Arrange
        var service = new RaycastService();
        var mapping = new Dictionary<string, BlockMapping>();
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Raycast_BlockDirectlyAhead_ShouldReturnBlock()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(5, 0, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["5,0,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east toward block

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(targetBlock);
    }

    [Fact]
    public void Raycast_BlockBehind_ShouldReturnNull()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(-5, 0, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["-5,0,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east (block is west)

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Raycast_BlockBeyondRange_ShouldReturnNull()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(100, 0, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["100,0,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east
        int range = 50; // Block is at 100, beyond range

        // Act
        var result = service.Raycast(mapping, pos, fvec, range);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Raycast_BlockWithinRange_ShouldReturnBlock()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(30, 0, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["30,0,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east
        int range = 50; // Block is at 30, within range

        // Act
        var result = service.Raycast(mapping, pos, fvec, range);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(targetBlock);
    }

    [Fact]
    public void Raycast_MultipleBlocksInLine_ShouldReturnClosest()
    {
        // Arrange
        var service = new RaycastService();
        var closeBlock = new BlockMapping(new Vector(5, 0, 0), "/test/close.txt", "stone");
        var farBlock = new BlockMapping(new Vector(10, 0, 0), "/test/far.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["5,0,0"] = closeBlock,
            ["10,0,0"] = farBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0); // Looking east

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(closeBlock);
    }

    [Fact]
    public void Raycast_DiagonalDirection_ShouldFindBlock()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(5, 5, 5), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["5,5,5"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        // Normalized diagonal direction (approximately)
        var fvec = new Vector(1, 1, 1);

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(targetBlock);
    }

    [Fact]
    public void Raycast_VerticalDirection_ShouldFindBlock()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(0, 10, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["0,10,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(0, 1, 0); // Looking up

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(targetBlock);
    }

    [Fact]
    public void Raycast_StartingInsideBlock_ShouldReturnThatBlock()
    {
        // Arrange
        var service = new RaycastService();
        var targetBlock = new BlockMapping(new Vector(0, 0, 0), "/test/file.txt", "stone");
        var mapping = new Dictionary<string, BlockMapping>
        {
            ["0,0,0"] = targetBlock
        };
        var pos = new Vector(0, 0, 0);
        var fvec = new Vector(1, 0, 0);

        // Act
        var result = service.Raycast(mapping, pos, fvec);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(targetBlock);
    }
}
