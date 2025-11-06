using System.Diagnostics;
using FluentAssertions;
using SaplingFS.Models;
using SaplingFS.Services;

namespace SaplingFS.Tests;

/// <summary>
/// Performance benchmarks comparing key operations.
/// These tests demonstrate the performance characteristics of the C# implementation.
/// </summary>
public class PerformanceBenchmarks
{
    [Fact]
    public void Benchmark_VectorOperations_Should_Be_Fast()
    {
        // Arrange
        const int iterations = 1_000_000;
        var v1 = new Vector(10, 20, 30);
        var v2 = new Vector(5, 15, 25);

        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var result = v1.Add(v2);
            var length = result.Length();
            _ = result.Normalize();
        }
        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Vector operations: {opsPerSecond:N0} ops/sec");
        Console.WriteLine($"Time for {iterations:N0} operations: {sw.ElapsedMilliseconds}ms");

        // Should complete 1M operations in under 100ms
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
        opsPerSecond.Should().BeGreaterThan(10_000_000); // 10M+ ops/sec
    }

    [Fact]
    public void Benchmark_RaycastPerformance_Should_Be_Efficient()
    {
        // Arrange
        const int iterations = 10_000;
        var service = new RaycastService();

        // Create a mapping with 1000 blocks in a grid
        var mapping = new Dictionary<string, BlockMapping>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int z = 0; z < 10; z++)
                {
                    var pos = new Vector(x * 5, y * 5, z * 5);
                    mapping[pos.ToString()] = new BlockMapping(pos, $"/test/{x}_{y}_{z}.txt", "stone");
                }
            }
        }

        var startPos = new Vector(0, 0, 0);
        var direction = new Vector(1, 0, 0);

        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = service.Raycast(mapping, startPos, direction);
        }
        sw.Stop();

        // Assert
        var raysPerSecond = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Raycast operations: {raysPerSecond:N0} rays/sec");
        Console.WriteLine($"Time for {iterations:N0} raycasts: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average time per raycast: {sw.Elapsed.TotalMilliseconds / iterations:F3}ms");

        // Should complete 10k raycasts in under 500ms
        sw.ElapsedMilliseconds.Should().BeLessThan(500);
        raysPerSecond.Should().BeGreaterThan(20_000); // 20k+ rays/sec
    }

    [Fact]
    public void Benchmark_MappingDictionaryOperations_Should_Scale()
    {
        // Arrange
        const int entries = 100_000;
        var mapping = new Dictionary<string, BlockMapping>();

        // Act - Insert
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < entries; i++)
        {
            var pos = new Vector(i % 1000, (i / 1000) % 100, i / 100_000);
            mapping[pos.ToString()] = new BlockMapping(pos, $"/test/file{i}.txt", "stone");
        }
        sw.Stop();
        var insertTime = sw.ElapsedMilliseconds;

        // Act - Lookup
        sw.Restart();
        for (int i = 0; i < entries; i++)
        {
            var pos = new Vector(i % 1000, (i / 1000) % 100, i / 100_000);
            _ = mapping.TryGetValue(pos.ToString(), out var block);
        }
        sw.Stop();
        var lookupTime = sw.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"Dictionary insert: {entries:N0} entries in {insertTime}ms");
        Console.WriteLine($"Insert rate: {entries / (insertTime / 1000.0):N0} inserts/sec");
        Console.WriteLine($"Dictionary lookup: {entries:N0} lookups in {lookupTime}ms");
        Console.WriteLine($"Lookup rate: {entries / (lookupTime / 1000.0):N0} lookups/sec");

        // Calculate memory usage
        var memoryPerEntry = GC.GetTotalMemory(true) / entries;
        Console.WriteLine($"Estimated memory per entry: ~{memoryPerEntry} bytes");

        // Should handle 100k operations quickly
        insertTime.Should().BeLessThan(1000); // Under 1 second
        lookupTime.Should().BeLessThan(500);  // Under 0.5 seconds
    }

    [Fact]
    public void Benchmark_StringOperations_Should_Be_Fast()
    {
        // Arrange
        const int iterations = 1_000_000;
        var vector = new Vector(123, 456, 789);

        // Act - ToString
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = vector.ToString();
        }
        sw.Stop();
        var toStringTime = sw.ElapsedMilliseconds;

        // Act - String interpolation (key creation)
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = $"{vector.X},{vector.Y},{vector.Z}";
        }
        sw.Stop();
        var interpolationTime = sw.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"ToString(): {iterations:N0} calls in {toStringTime}ms");
        Console.WriteLine($"String interpolation: {iterations:N0} operations in {interpolationTime}ms");

        toStringTime.Should().BeLessThan(500);
        interpolationTime.Should().BeLessThan(500);
    }

    [Fact]
    public void Benchmark_MemoryAllocation_For_BlockMapping()
    {
        // Arrange
        const int entries = 10_000;
        var mappings = new List<BlockMapping>(entries);

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        // Act
        for (int i = 0; i < entries; i++)
        {
            var pos = new Vector(i, i + 1, i + 2);
            var mapping = new BlockMapping(pos, $"/test/file{i}.txt", "stone");
            mappings.Add(mapping);
        }

        // Force GC after to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);

        // Assert
        var memoryUsed = memoryAfter - memoryBefore;
        var bytesPerEntry = memoryUsed / entries;

        Console.WriteLine($"Memory used for {entries:N0} BlockMapping entries: {memoryUsed:N0} bytes");
        Console.WriteLine($"Average bytes per entry: {bytesPerEntry}");
        Console.WriteLine($"Projected memory for 100k entries: {bytesPerEntry * 100_000 / 1024 / 1024:F2} MB");

        // Each entry should be relatively compact (includes overhead)
        bytesPerEntry.Should().BeLessThan(250); // Should be under 250 bytes per entry
    }

    [Fact]
    public void Benchmark_ParallelProcessing_Advantage()
    {
        // Arrange
        const int workItems = 1000;
        var items = Enumerable.Range(0, workItems).ToList();

        // Sequential processing
        var sw = Stopwatch.StartNew();
        foreach (var item in items)
        {
            ExpensiveOperation(item);
        }
        sw.Stop();
        var sequentialTime = sw.ElapsedMilliseconds;

        // Parallel processing
        sw.Restart();
        Parallel.ForEach(items, item =>
        {
            ExpensiveOperation(item);
        });
        sw.Stop();
        var parallelTime = sw.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"Sequential processing: {sequentialTime}ms");
        Console.WriteLine($"Parallel processing: {parallelTime}ms");
        Console.WriteLine($"Speedup: {(double)sequentialTime / parallelTime:F2}x");

        // Parallel should be faster (accounting for overhead)
        parallelTime.Should().BeLessThan(sequentialTime);
    }

    private static void ExpensiveOperation(int n)
    {
        // Simulate some work
        var result = 0.0;
        for (int i = 0; i < 10000; i++)
        {
            result += Math.Sqrt(n * i);
        }
    }
}
