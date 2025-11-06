# SaplingFS - .NET Edition

**Voxel-based Entropy-oriented Minecraft File System**

Maps every file on your computer to a block in a Minecraft world. Breaking blocks in-game deletes their associated files.

![License](https://img.shields.io/badge/license-MIT-blue)
![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)
![Tests](https://img.shields.io/badge/tests-106%20passing-brightgreen)

---

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Minecraft Java Edition
- A test world (make a backup first!)

### Build & Run

```bash
# Build the solution
dotnet build SaplingFS.sln

# Run with a world name
dotnet run --project src/SaplingFS.csproj -- <world_name>

# Example with options
dotnet run --project src/SaplingFS.csproj -- test_world --debug --path ~/Documents --depth 3
```

### Compile to Executable

You can compile SaplingFS into a standalone executable that doesn't require .NET to be installed:

> **Note:** The first time you publish for a specific platform, `dotnet publish` will download the runtime for that platform (~50-70 MB). This can take 1-3 minutes depending on your connection. Subsequent builds will be much faster.

#### **Linux (x64)**
```bash
# Build self-contained executable for Linux
dotnet publish src/SaplingFS.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Executable will be at:
# src/bin/Release/net9.0/linux-x64/publish/SaplingFS

# Run it:
./src/bin/Release/net9.0/linux-x64/publish/SaplingFS MyWorld
```

#### **macOS (ARM64 - M1/M2/M3/M4)** ⭐ Recommended for Apple Silicon
```bash
# Build self-contained executable for macOS ARM
dotnet publish src/SaplingFS.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Executable will be at:
# src/bin/Release/net9.0/osx-arm64/publish/SaplingFS

# Run it:
./src/bin/Release/net9.0/osx-arm64/publish/SaplingFS MyWorld
```

> **For M-series Macs:** Use `osx-arm64` above. The Intel version (`osx-x64`) will work via Rosetta but will be slower.

#### **macOS (x64 - Intel)**
```bash
# Build self-contained executable for macOS Intel
dotnet publish src/SaplingFS.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Executable will be at:
# src/bin/Release/net9.0/osx-x64/publish/SaplingFS

# Run it:
./src/bin/Release/net9.0/osx-x64/publish/SaplingFS MyWorld
```

#### **Windows (x64)**
```bash
# Build self-contained executable for Windows
dotnet publish src/SaplingFS.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Executable will be at:
# src\bin\Release\net9.0\win-x64\publish\SaplingFS.exe

# Run it:
.\src\bin\Release\net9.0\win-x64\publish\SaplingFS.exe MyWorld
```

#### **Publish Options Explained**

- `--self-contained true` - Bundles .NET runtime (no .NET installation required)
- `-p:PublishSingleFile=true` - Creates a single executable file
- `-p:PublishTrimmed=true` - Removes unused code to reduce size (~50-70 MB)
- `-c Release` - Optimized release build

#### **Without Self-Contained (Requires .NET)**

If you want a smaller executable that requires .NET to be installed:

```bash
# Framework-dependent (much smaller, ~500 KB)
dotnet publish src/SaplingFS.csproj -c Release -r osx-arm64 -p:PublishSingleFile=true

# Run it (requires .NET 9.0 installed):
./src/bin/Release/net9.0/osx-arm64/publish/SaplingFS MyWorld
```

#### **Troubleshooting Publish**

**"Restore is taking a long time"**
- **First-time:** Downloading runtime packages takes 1-3 minutes (50-70 MB)
- **Check progress:** Add `-v:n` for normal verbosity to see what's downloading
  ```bash
  dotnet publish src/SaplingFS.csproj -c Release -r osx-arm64 --self-contained true -v:n
  ```
- **Subsequent builds:** Will be much faster as packages are cached

**"Hung on restore"**
- Press `Ctrl+C` to cancel, then try again
- Clear NuGet cache if needed: `dotnet nuget locals all --clear`
- Check internet connection

**"Executable too large"**
- Self-contained executables are 50-70 MB (includes .NET runtime)
- Use framework-dependent build for ~500 KB (requires .NET installed)
- `PublishTrimmed=true` already removes unused code

### Common Options

```
--debug                 Colorful terrain for debugging directory groups
--path <path>          Root path to scan (default: C:\ on Windows, / on Unix)
--depth <number>       Directory grouping depth (default: 2 on Windows, 3 on Unix)
--no-progress          Don't save/load progress
--blacklist <paths>    Semicolon-separated paths to exclude
--allow-delete <time>  Enable file deletion (requires current time in HH:mm format)
```

---

## 📁 Project Structure

```
SaplingFS/
├── SaplingFS.sln              # Solution file
├── src/                       # Main application
│   ├── Models/               # Data models (Vector, BlockMapping, etc.)
│   ├── Services/             # Core services (WorldParser, TerrainGenerator, etc.)
│   ├── Configuration/        # Command-line options
│   └── Program.cs            # Entry point
├── tests/                     # Unit tests (106 tests, 100% passing)
│   ├── Models/               # Model tests
│   ├── Services/             # Service tests
│   └── PerformanceBenchmarks.cs
├── docs/                      # Documentation
│   ├── PERFORMANCE_ANALYSIS.md
│   ├── BENCHMARK_RESULTS.md
│   └── CONTINUATION_NOTES.md
├── legacy-js/                 # Original JavaScript implementation
└── README.md                  # This file
```

---

## 🎮 How It Works

1. **Scan**: Recursively scans your filesystem
2. **Generate**: Creates Minecraft terrain using BFS algorithm
   - Each file becomes a block
   - Files from same directory cluster together
   - Trees and ponds separate directory groups
3. **Monitor**: Watches clipboard for player position
   - Copy position in-game: `/data get entity @s Pos`
   - SaplingFS identifies which file you're looking at
4. **Track**: Monitors world for block changes
   - Detects when blocks are destroyed
   - Optionally deletes the associated file (with `--allow-delete`)

---

## 🏗️ Architecture

### Key Components

**Models** (`src/Models/`)
- `Vector` - 3D coordinate math with conversion utilities
- `BlockMapping` - Maps positions to files and blocks
- `MappedFile` - File metadata with path abbreviation
- `RegionFileCache` - Caches region file data with checksums

**Services** (`src/Services/`)
- `WorldParser` - NBT parsing and region file I/O
- `TerrainGenerator` - BFS terrain generation with smoothing
- `FileScanner` - Recursive filesystem scanning
- `RaycastService` - 3D DDA raycasting for block identification
- `ClipboardMonitor` - Player position tracking
- `BlockChangeMonitor` - Region file change detection
- `ProcessManager` - Cross-platform process/handle management
- `StatusDisplay` - Real-time progress tracking with ETA calculations

### Algorithms

**Terrain Generation** (BFS)
- Start at (0, 32, 0)
- Expand outward, grouping by parent directory
- Random suppression for organic shapes
- Tree placement (62 blocks each)
- Water bodies between groups
- Terrain smoothing and ore veins

**Block Change Detection**
- SHA256 checksums for region files
- Chunk-level hash comparison
- Parallel processing of changes

**Raycasting** (3D DDA)
- Cast ray from player eye position
- Step through voxels to find mapped blocks
- Used for clipboard-based identification

---

## ⚡ Performance

The C# implementation is **2-10x faster** than the JavaScript version:

| Operation | C# (.NET 9) | JavaScript (Bun) | Speedup |
|-----------|-------------|------------------|---------|
| Vector Operations | 28.9M ops/sec | ~3M ops/sec | **9.6x** |
| Raycasting | 8.7M rays/sec | ~0.8M rays/sec | **10.9x** |
| Dictionary Lookups | 7.7M ops/sec | ~2M ops/sec | **3.9x** |
| Terrain Generation (50k files) | ~60s | ~180s | **3.0x** |
| Memory Usage | 222 bytes/entry | 400-500 bytes | **56% less** |

**Why C# is faster:**
- Compiled to native code (JIT)
- Value types for vectors (no heap allocation)
- True multi-threading (uses all CPU cores)
- Strongly typed (no runtime type checking)

See [docs/BENCHMARK_RESULTS.md](docs/BENCHMARK_RESULTS.md) for detailed analysis.

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test category
dotnet test --filter "FullyQualifiedName~Models"
dotnet test --filter "FullyQualifiedName~Services"
dotnet test --filter "FullyQualifiedName~PerformanceBenchmarks"
```

**Test Coverage:**
- ✅ 106 tests, 100% passing
- ✅ Models: Vector, MappedFile, BlockMapping
- ✅ Services: FileScanner, TerrainGenerator, RaycastService, StatusDisplay
- ✅ Performance benchmarks

---

## 🔒 Safety Features

- **World Backup**: Automatically created before modifications
- **Confirmation Required**: `--allow-delete` requires current time
- **10-Second Countdown**: Warning before enabling deletion
- **Deletion Disabled by Default**: Cosmetic mode unless explicitly enabled

---

## 🚧 Development

### Building from Source

```bash
# Clone the repository
git clone <repo-url>
cd SaplingCS

# Restore dependencies
dotnet restore

# Build
dotnet build SaplingFS.sln

# Run tests
dotnet test
```

### Project Dependencies

- **fNbt** (1.0.0) - NBT format parsing
- **TextCopy** (6.2.1) - Cross-platform clipboard access
- **xUnit** (2.9.2) - Testing framework
- **FluentAssertions** (8.8.0) - Assertion library
- **NSubstitute** (5.3.0) - Mocking framework

---

## 📚 Documentation

- [Performance Analysis](docs/PERFORMANCE_ANALYSIS.md) - Technical comparison with JavaScript
- [Benchmark Results](docs/BENCHMARK_RESULTS.md) - Actual measured performance
- [macOS File Access Guide](docs/MACOS_FILE_ACCESS.md) - Handling system permissions on macOS
- [Migration Analysis](docs/DOTNET_MIGRATION_ANALYSIS.md) - Original migration plan
- [Continuation Notes](docs/CONTINUATION_NOTES.md) - Development session notes
- [CLAUDE.md](CLAUDE.md) - Guide for AI assistants

---

## 🔄 Comparison with JavaScript Version

The original JavaScript implementation is preserved in `legacy-js/` for reference.

**Use C# (.NET) when:**
- ✅ Production deployment (>10,000 files)
- ✅ Performance is critical
- ✅ Multi-core systems
- ✅ Memory efficiency matters

**Use JavaScript (Bun) when:**
- ✅ Quick prototyping
- ✅ Small workloads (<5,000 files)
- ✅ Rapid iteration
- ✅ Cross-platform scripting

Both produce identical Minecraft worlds - the difference is runtime performance.

---

## ⚠️ Warning

**SaplingFS can irreversibly delete files on your system.**

- Only use `--allow-delete` if you understand the risks
- Always test with a backup world first
- Use blacklists to exclude important directories
- The program kills processes holding file handles before deletion

---

## 📝 License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Original concept and JavaScript implementation
- Ported to C# .NET 9.0 with performance optimizations
- Uses fNbt library for Minecraft NBT parsing
- Inspired by the desire to visualize filesystems in 3D

---

## 🐛 Known Issues

- Large filesystems (>100k files) may take several minutes to generate
- Minecraft must be closed during terrain generation
- Some region file parsing errors may occur with corrupted worlds

---

## 🤝 Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass (`dotnet test`)
5. Submit a pull request

---

## 📧 Contact

For questions, issues, or feature requests, please open an issue on GitHub.

---

**Made with ❤️ and .NET 9.0**
