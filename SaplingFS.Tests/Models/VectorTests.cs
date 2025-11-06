using FluentAssertions;
using SaplingFS.Models;

namespace SaplingFS.Tests.Models;

public class VectorTests
{
    [Fact]
    public void Constructor_ShouldInitializeCoordinates()
    {
        // Arrange & Act
        var vector = new Vector(10, 20, 30);

        // Assert
        vector.X.Should().Be(10);
        vector.Y.Should().Be(20);
        vector.Z.Should().Be(30);
    }

    [Fact]
    public void DefaultConstructor_ShouldInitializeToZero()
    {
        // Arrange & Act
        var vector = new Vector();

        // Assert
        vector.X.Should().Be(0);
        vector.Y.Should().Be(0);
        vector.Z.Should().Be(0);
    }

    [Fact]
    public void Add_ShouldReturnNewVectorWithAddedCoordinates()
    {
        // Arrange
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(5, 15, 25);

        // Act
        var result = v1.Add(v2);

        // Assert
        result.X.Should().Be(15);
        result.Y.Should().Be(35);
        result.Z.Should().Be(55);
        v1.Should().Be(new Vector(10, 20, 30)); // Original unchanged
    }

    [Fact]
    public void Add_WithIndividualValues_ShouldReturnNewVector()
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var result = v.Add(5, 10, 15);

        // Assert
        result.X.Should().Be(15);
        result.Y.Should().Be(30);
        result.Z.Should().Be(45);
    }

    [Fact]
    public void Sub_ShouldReturnNewVectorWithSubtractedCoordinates()
    {
        // Arrange
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(5, 15, 25);

        // Act
        var result = v1.Sub(v2);

        // Assert
        result.X.Should().Be(5);
        result.Y.Should().Be(5);
        result.Z.Should().Be(5);
    }

    [Fact]
    public void Length_ShouldCalculateCorrectMagnitude()
    {
        // Arrange
        var v = new Vector(3, 4, 0);

        // Act
        var length = v.Length();

        // Assert
        length.Should().Be(5.0); // 3-4-5 triangle
    }

    [Fact]
    public void Normalize_ShouldReturnUnitVector()
    {
        // Arrange
        var v = new Vector(3, 4, 0);

        // Act
        var normalized = v.Normalize();

        // Assert
        normalized.Length().Should().BeApproximately(1.0, 0.0001);
        Math.Abs(normalized.X - 0.6).Should().BeLessThan(0.0001);
        Math.Abs(normalized.Y - 0.8).Should().BeLessThan(0.0001);
        normalized.Z.Should().Be(0);
    }

    [Fact]
    public void Normalize_ZeroVector_ShouldReturnZeroVector()
    {
        // Arrange
        var v = new Vector(0, 0, 0);

        // Act
        var normalized = v.Normalize();

        // Assert
        normalized.Should().Be(new Vector(0, 0, 0));
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]   // North: +Z
    [InlineData(1, -1, 0, 0)]  // South: -Z
    [InlineData(2, 0, 0, -1)]  // West: -X
    [InlineData(3, 0, 0, 1)]   // East: +X
    [InlineData(4, 0, 1, 0)]   // Up: +Y
    [InlineData(5, 0, -1, 0)]  // Down: -Y
    public void Shifted_ShouldMoveInCorrectDirection(int direction, int expectedX, int expectedY, int expectedZ)
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var shifted = v.Shifted(direction);

        // Assert
        shifted.X.Should().Be(10 + expectedX);
        shifted.Y.Should().Be(20 + expectedY);
        shifted.Z.Should().Be(30 + expectedZ);
    }

    [Fact]
    public void Shifted_InvalidDirection_ShouldReturnOriginalVector()
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var shifted = v.Shifted(99);

        // Assert
        shifted.Should().Be(v);
    }

    [Fact]
    public void Absolute_ShouldConvertChunkRelativeToAbsolute()
    {
        // Arrange
        var relativePos = new Vector(5, 10, 7);
        int chunkX = 2;
        int chunkZ = 3;

        // Act
        var absolute = relativePos.Absolute(chunkX, chunkZ);

        // Assert
        absolute.X.Should().Be(2 * 16 + 5); // 37
        absolute.Y.Should().Be(10);
        absolute.Z.Should().Be(3 * 16 + 7); // 55
    }

    [Fact]
    public void Relative_ShouldConvertAbsoluteToChunkRelative()
    {
        // Arrange
        var absolutePos = new Vector(37, 10, 55);
        int chunkX = 2;
        int chunkZ = 3;

        // Act
        var relative = absolutePos.Relative(chunkX, chunkZ);

        // Assert
        relative.X.Should().Be(5);
        relative.Y.Should().Be(10);
        relative.Z.Should().Be(7);
    }

    [Fact]
    public void AbsoluteAndRelative_ShouldBeInverse()
    {
        // Arrange
        var original = new Vector(5, 10, 7);
        int chunkX = 2;
        int chunkZ = 3;

        // Act
        var absolute = original.Absolute(chunkX, chunkZ);
        var backToRelative = absolute.Relative(chunkX, chunkZ);

        // Assert
        backToRelative.Should().Be(original);
    }

    [Fact]
    public void FromAngles_ShouldCalculateForwardVector()
    {
        // Arrange & Act
        var forward = Vector.FromAngles(0, 0);

        // Assert - Looking straight ahead (north)
        forward.Z.Should().BeGreaterThan(0);
        Math.Abs((double)forward.Y).Should().BeLessThan(0.0001);
    }

    [Fact]
    public void FromAngles_LookingDown_ShouldHaveNegativeY()
    {
        // Arrange & Act
        var forward = Vector.FromAngles(0, 90); // Looking straight down

        // Assert
        forward.Y.Should().BeLessThan(0);
    }

    [Fact]
    public void FromAngles_LookingUp_ShouldHavePositiveY()
    {
        // Arrange & Act
        var forward = Vector.FromAngles(0, -90); // Looking straight up

        // Assert
        forward.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToString_ShouldReturnCommaSeparatedCoordinates()
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var str = v.ToString();

        // Assert
        str.Should().Be("10,20,30");
    }

    [Fact]
    public void ToArray_ShouldReturnArrayOfCoordinates()
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var array = v.ToArray();

        // Assert
        array.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(10, 20, 30);

        // Act & Assert
        v1.Should().Be(v2);
        (v1 == v2).Should().BeTrue();
        (v1 != v2).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(10, 20, 31);

        // Act & Assert
        v1.Should().NotBe(v2);
        (v1 == v2).Should().BeFalse();
        (v1 != v2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldHaveSameHash()
    {
        // Arrange
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(10, 20, 30);

        // Act & Assert
        v1.GetHashCode().Should().Be(v2.GetHashCode());
    }

    [Fact]
    public void With_ShouldAllowModifyingIndividualComponents()
    {
        // Arrange
        var v = new Vector(10, 20, 30);

        // Act
        var modified = v with { Y = 100 };

        // Assert
        modified.X.Should().Be(10);
        modified.Y.Should().Be(100);
        modified.Z.Should().Be(30);
        v.Y.Should().Be(20); // Original unchanged
    }
}
