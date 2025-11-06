using FluentAssertions;
using SaplingFS.Services;

namespace SaplingFS.Tests.Services;

public class StatusDisplayTests
{
    [Fact]
    public void StatusDisplay_ShouldInitialize()
    {
        // Arrange & Act
        var statusDisplay = new StatusDisplay();

        // Assert
        statusDisplay.Should().NotBeNull();
    }

    [Fact]
    public void SetPhase_ShouldUpdateCurrentPhase()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var phase = "Test Phase";

        // Act
        statusDisplay.SetPhase(phase);

        // Assert - Should not throw, phase should be internally set
        // (We can't directly test the private field, but we can verify the method executes)
        var exception = Record.Exception(() => statusDisplay.SetPhase(phase));
        exception.Should().BeNull();
    }

    [Fact]
    public void SetDetail_ShouldUpdateDetailMessage()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var detail = "Processing file.txt";

        // Act
        statusDisplay.SetDetail(detail);

        // Assert
        var exception = Record.Exception(() => statusDisplay.SetDetail(detail));
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateFileScanning_ShouldAcceptFileCount()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var fileCount = 100;

        // Act
        statusDisplay.UpdateFileScanning(fileCount);

        // Assert
        var exception = Record.Exception(() => statusDisplay.UpdateFileScanning(fileCount));
        exception.Should().BeNull();
    }

    [Fact]
    public void SetTotalFiles_ShouldAcceptTotalCount()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var total = 1000;

        // Act
        statusDisplay.SetTotalFiles(total);

        // Assert
        var exception = Record.Exception(() => statusDisplay.SetTotalFiles(total));
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateTerrainGeneration_ShouldAcceptProcessedCount()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var processed = 50;

        // Act
        statusDisplay.UpdateTerrainGeneration(processed);

        // Assert
        var exception = Record.Exception(() => statusDisplay.UpdateTerrainGeneration(processed));
        exception.Should().BeNull();
    }

    [Fact]
    public void Start_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() => statusDisplay.Start());

        // Assert
        exception.Should().BeNull();

        // Cleanup
        statusDisplay.Stop();
    }

    [Fact]
    public void Stop_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        statusDisplay.Start();
        var exception = Record.Exception(() => statusDisplay.Stop());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void StartStop_ShouldBeCallableMultipleTimes()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            statusDisplay.Start();
            statusDisplay.Stop();
            statusDisplay.Start();
            statusDisplay.Stop();
        });

        exception.Should().BeNull();
    }

    [Fact]
    public void CompleteWorkflow_FileScanningPhase_ShouldExecuteWithoutErrors()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() =>
        {
            statusDisplay.Start();
            statusDisplay.SetPhase("Scanning filesystem");

            // Simulate file scanning progress
            for (int i = 100; i <= 500; i += 100)
            {
                statusDisplay.UpdateFileScanning(i);
                Thread.Sleep(10); // Small delay to allow rendering
            }

            statusDisplay.Stop();
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void CompleteWorkflow_TerrainGenerationPhase_ShouldExecuteWithoutErrors()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() =>
        {
            statusDisplay.Start();
            statusDisplay.SetPhase("Generating terrain");
            statusDisplay.SetTotalFiles(1000);

            // Simulate terrain generation progress
            for (int i = 100; i <= 1000; i += 100)
            {
                statusDisplay.UpdateTerrainGeneration(i);
                statusDisplay.SetDetail($"Mapping directory {i / 100}");
                Thread.Sleep(10); // Small delay to allow rendering
            }

            statusDisplay.Stop();
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateTerrainGeneration_WithoutSetTotalFiles_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        statusDisplay.Start();
        var exception = Record.Exception(() =>
        {
            statusDisplay.UpdateTerrainGeneration(50);
        });
        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void SetDetail_WithLongString_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        var longDetail = new string('x', 500);

        // Act
        statusDisplay.Start();
        var exception = Record.Exception(() => statusDisplay.SetDetail(longDetail));
        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void SetPhase_WithEmptyString_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() => statusDisplay.SetPhase(""));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateFileScanning_WithZero_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        statusDisplay.Start();
        var exception = Record.Exception(() => statusDisplay.UpdateFileScanning(0));
        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateTerrainGeneration_WithZero_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        statusDisplay.Start();
        statusDisplay.SetTotalFiles(100);
        var exception = Record.Exception(() => statusDisplay.UpdateTerrainGeneration(0));
        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void SetTotalFiles_WithZero_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() => statusDisplay.SetTotalFiles(0));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void ConcurrentUpdates_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();
        statusDisplay.Start();
        statusDisplay.SetPhase("Concurrent Test");
        statusDisplay.SetTotalFiles(1000);

        // Act
        var exception = Record.Exception(() =>
        {
            var tasks = new List<Task>();

            // Create multiple concurrent update tasks
            for (int i = 0; i < 10; i++)
            {
                int captured = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        statusDisplay.UpdateTerrainGeneration(captured * 100 + j * 10);
                        statusDisplay.SetDetail($"Thread {captured}, iteration {j}");
                        Thread.Sleep(1);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        });

        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void Stop_WithoutStart_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() => statusDisplay.Stop());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void UpdateOperations_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() =>
        {
            statusDisplay.SetPhase("Test");
            statusDisplay.SetDetail("Detail");
            statusDisplay.UpdateFileScanning(100);
            statusDisplay.SetTotalFiles(1000);
            statusDisplay.UpdateTerrainGeneration(500);
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void ProgressUpdates_IncreasingValues_ShouldNotThrow()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        statusDisplay.Start();
        var exception = Record.Exception(() =>
        {
            statusDisplay.SetTotalFiles(1000);
            for (int i = 0; i <= 1000; i += 50)
            {
                statusDisplay.UpdateTerrainGeneration(i);
            }
        });
        statusDisplay.Stop();

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void PhaseTransition_ShouldHandleMultiplePhases()
    {
        // Arrange
        var statusDisplay = new StatusDisplay();

        // Act
        var exception = Record.Exception(() =>
        {
            statusDisplay.Start();

            // Phase 1: Scanning
            statusDisplay.SetPhase("Scanning filesystem");
            statusDisplay.UpdateFileScanning(100);
            Thread.Sleep(20);

            // Phase 2: Generating
            statusDisplay.SetPhase("Generating terrain");
            statusDisplay.SetTotalFiles(1000);
            statusDisplay.UpdateTerrainGeneration(500);
            Thread.Sleep(20);

            // Phase 3: Writing
            statusDisplay.SetPhase("Writing data");
            statusDisplay.SetDetail("Finalizing...");
            Thread.Sleep(20);

            statusDisplay.Stop();
        });

        // Assert
        exception.Should().BeNull();
    }
}
