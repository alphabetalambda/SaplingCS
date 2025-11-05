# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SaplingFS is a voxel-based Minecraft filesystem that maps every file on your computer to a block in a Minecraft world. Breaking blocks in-game deletes their associated files. The project uses Bun runtime and operates by:
1. Scanning a filesystem and generating Minecraft terrain where each block represents a file
2. Monitoring the Minecraft world for block changes
3. Optionally deleting files when their corresponding blocks are destroyed

## Development Commands

### Running the Program
```bash
# Basic usage with a world name
bun main.js <world_name>

# Common options
bun main.js <world_name> --debug              # Colorful terrain for debugging directory groups
bun main.js <world_name> --path <path>        # Custom root path (default: C:\ or /)
bun main.js <world_name> --depth <number>     # Directory grouping depth (default: 2 on Windows, 3 on Unix)
bun main.js <world_name> --no-progress        # Don't save/load progress to disk
bun main.js <world_name> --blacklist <paths>  # Semicolon-separated paths to exclude
```

### Build Commands
```bash
# Install dependencies
bun install

# Build for Linux
bun build --compile --target=bun-linux-x64 ./main.js --outfile SaplingFS-linux

# Build for Windows
bun build --compile --target=bun-windows-x64 ./main.js --outfile SaplingFS-windows.exe
```

## Architecture

### Core Module Responsibilities

**main.js** - Entry point and orchestration
- Command-line argument parsing and validation
- World backup creation
- Progress persistence (mapping data to/from disk as compressed JSON)
- Clipboard monitoring for player position (raycasting to identify targeted blocks)
- Block change detection loop that monitors region files
- File deletion logic (when `--allow-delete` is enabled)

**worldGenTools.js** - Terrain generation
- `mapping` - Global object storing block-to-file relationships (`"x,y,z"` → `{pos, file, block}`)
- `buildRegionData()` - Primary terrain generation using breadth-first search algorithm
  - Groups files by top-level directories to create distinct terrain "islands"
  - Places trees (62 blocks each) and water bodies randomly
  - Smooths terrain by clustering lonely blocks
  - Converts grass to dirt when covered, adds ore veins
- `forMappedChunks()` - Iterator for processing chunks that contain mapped blocks
- Natural terrain rules (grass/dirt/stone conversion, water placement)

**parseWorld.js** - Minecraft region file I/O
- `regionToBlocks()` - Reads .mca files, decompresses chunks, parses NBT data into block arrays
- `blocksToRegion()` - Writes block arrays back to region files with proper NBT/compression
- Region file caching with checksums to detect changes
- `forRegion()` - Iterator for region files within given bounds
- Chunk hashing to detect modifications without full parsing

**fileTools.js** - Filesystem utilities
- `MappedFile` class - Represents a file with path, size, and depth
  - `getShortParent()` - Gets parent directory for grouping
  - `getShortPath()` - Creates abbreviated path for display
- `buildFileList()` - Recursive depth-first file system scanner
  - Skips directories with "cache" in name
  - Ignores empty files
  - Respects blacklist

**procTools.js** - Process management
- `getHandleOwners()` - Platform-specific file handle detection (lsof on Unix, handle.exe on Windows)
- `killProcess()` - Platform-specific process termination
- Used to kill processes before deleting files they have open

**Vector.js** - 3D vector math
- Basic operations: add, sub, length, normalize
- `shifted()` - Move one unit in cardinal directions
- `absolute()`/`relative()` - Convert between chunk-relative and absolute coordinates
- `fromAngles()` - Create forward vector from yaw/pitch for raycasting

### Key Algorithms

**Terrain Generation Flow:**
1. Start at (0, 32, 0) with open node list
2. For each file, pop a position from nodes, assign file to that block
3. Add adjacent positions to node list (with random suppression for organic shapes)
4. When parent directory changes, finalize current "terrain group":
   - Place queued trees and ponds
   - Start new group from random position in existing terrain
5. Second pass: smooth terrain, convert grass/dirt/stone, add ores
6. Write to region files

**Block Change Detection:**
1. Poll region files for checksum changes
2. For changed regions, iterate mapped chunks
3. Hash first chunk to detect changes
4. Parse chunk data into block array
5. Compare actual blocks vs expected mapping
6. When mismatch found: log removal, optionally kill processes and delete file

**3D DDA Raycasting** (main.js:179-238)
- Cast ray from player eye position in look direction
- Step through voxels using tMax/tDelta algorithm
- Return first mapped block hit (used for clipboard-based block identification)

### Data Persistence

**Progress Saving** (main.js:113-126)
- Mapping compressed as zlib JSON to `mapping/<world_name>.json.zlib`
- Vectors serialized as arrays `[x, y, z]`
- Files serialized as `[path, size, depth]`
- Auto-saves every 5 minutes unless `--no-progress` specified

### Safety Features

- World is backed up to `<world_path>_SaplingFS_backup` before any modifications
- `--allow-delete` requires current time in 24h format as confirmation
- 10-second countdown before enabling file deletion
- Deletion is disabled by default (purely cosmetic messages)

### Platform Differences

- Default root path: `C:\` on Windows, `/` on Unix
- Default parent depth: 2 on Windows, 3 on Unix
- Minecraft save location: `%APPDATA%\.minecraft\saves` on Windows, `~/.minecraft/saves` on Unix
- Process/file handle tools use platform-specific commands

## Important Notes

- This program is designed to be **potentially destructive** - it can delete files and kill processes
- The codebase uses Bun runtime (not Node.js) - note the use of `Bun.file()`, `Bun.hash()`, `Bun.write()`
- Minecraft region files (.mca) use NBT (Named Binary Tag) format with zlib compression
- Block coordinates use Minecraft conventions: Y=-64 to Y=320, chunks are 16×16×384 blocks
- All asynchronous operations use promises/async-await
