# Third-Party Launcher Support

SaplingFS now supports multiple Minecraft launchers across Windows, macOS, and Linux!

## Supported Launchers

- **Official** - Official Minecraft Launcher
- **PrismLauncher** - Popular open-source launcher (fork of MultiMC)
- **MultiMC** - Classic multi-instance launcher
- **CurseForge** - Mod-focused launcher
- **ATLauncher** - Modpack launcher
- **Modrinth** - Modern launcher with mod management
- **GDLauncher** - Feature-rich launcher

## Usage

### Basic Usage (Official Launcher)

```bash
SaplingFS MyWorld --path ~/Documents --debug
```

### Specify a Launcher

```bash
SaplingFS MyWorld --launcher PrismLauncher --path ~/Documents --debug
```

If multiple instances exist for that launcher, you'll be prompted to select one interactively.

### Specify Both Launcher and Instance

```bash
SaplingFS MyWorld --launcher PrismLauncher --instance "1.20.1 Modded" --path ~/Documents --debug
```

This will use the "1.20.1 Modded" instance from Prism Launcher.

### Auto-Detection by Instance Name

```bash
SaplingFS MyWorld --instance "Vanilla 1.20" --path ~/Documents --debug
```

This searches all launchers for an instance named "Vanilla 1.20".

## Launcher Paths by Platform

### Windows

| Launcher | Path |
|----------|------|
| Official | `%APPDATA%\.minecraft` |
| Prism Launcher | `%APPDATA%\PrismLauncher` |
| Prism (Scoop) | `%HOMEPATH%\scoop\persist\prismlauncher` |
| MultiMC | `%LOCALAPPDATA%\MultiMC` or `C:\MultiMC` |
| CurseForge | `%USERPROFILE%\Documents\Curseforge\Minecraft` |
| ATLauncher | `%USERPROFILE%\ATLauncher` |
| Modrinth | `%APPDATA%\com.modrinth.theseus` |
| GDLauncher | `%APPDATA%\gdlauncher_next` |

### macOS

| Launcher | Path |
|----------|------|
| Official | `~/Library/Application Support/minecraft` |
| Prism Launcher | `~/Library/Application Support/PrismLauncher` |
| MultiMC | `~/Library/Application Support/MultiMC` |
| CurseForge | `~/Documents/Curseforge/Minecraft` |
| ATLauncher | `~/Library/Application Support/ATLauncher` |
| Modrinth | `~/Library/Application Support/com.modrinth.theseus` |
| GDLauncher | `~/Library/Application Support/gdlauncher_next` |

### Linux

| Launcher | Path |
|----------|------|
| Official | `~/.minecraft` |
| Prism Launcher | `~/.local/share/PrismLauncher` |
| Prism (Flatpak) | `~/.var/app/org.prismlauncher.PrismLauncher/data/PrismLauncher` |
| MultiMC | `~/.local/share/multimc` or `~/.multimc` |
| CurseForge | `~/.local/share/curseforge/minecraft` |
| ATLauncher | `~/.local/share/ATLauncher` |
| ATLauncher (Flatpak) | `~/.var/app/com.atlauncher.ATLauncher/data/ATLauncher` |
| Modrinth | `~/.local/share/com.modrinth.theseus` |
| Modrinth (Flatpak) | `~/.var/app/com.modrinth.ModrinthApp/data/ModrinthApp` |
| GDLauncher | `~/.local/share/gdlauncher_next` |

## Instance Structure

Different launchers organize instances differently:

- **Official Launcher**: No instances, worlds are in `.minecraft/saves/`
- **Prism/MultiMC**: Instances in `instances/[name]/.minecraft/`
- **CurseForge**: Instances in `Instances/[name]/`
- **ATLauncher**: Instances in `instances/[name]/`
- **Modrinth**: Profiles in `profiles/[name]/`
- **GDLauncher**: Instances in `instances/[name]/`

## Interactive Instance Selection

When multiple instances are found and you haven't specified which one to use, SaplingFS will show an interactive menu:

```
Multiple Minecraft instances detected:

  PrismLauncher:
    [1] Vanilla 1.20.1
    [2] Fabric 1.20.1
    [3] Forge 1.19.2

  MultiMC:
    [4] Testing Instance
    [5] Skyblock

Select an instance (1-5):
```

Simply enter the number of the instance you want to use.

## Examples

### Example 1: Prism Launcher with Auto-Selection

```bash
# If you only have one Prism Launcher instance
SaplingFS MyWorld --launcher PrismLauncher --path ~/Documents
```

### Example 2: CurseForge with Specific Modpack

```bash
# Use a specific CurseForge modpack instance
SaplingFS MyWorld --launcher CurseForge --instance "All the Mods 9" --path ~/Downloads
```

### Example 3: Search All Launchers

```bash
# Find and use any instance named "Vanilla"
SaplingFS MyWorld --instance Vanilla --path ~/Documents --debug
```

## Troubleshooting

### "No instances found for launcher"

This means the launcher isn't installed in the standard location, or no instances have been created yet. Try:
1. Verify the launcher is installed
2. Create at least one instance in that launcher
3. Check the paths listed above

### "Multiple instances found. Please specify one with --instance"

Use the `--instance` flag to specify which instance to use:

```bash
SaplingFS MyWorld --launcher PrismLauncher --instance "MyModpack"
```

### Portable Installations

For portable launcher installations (where the launcher isn't in the standard location), you can directly specify the world path:

```bash
SaplingFS "/path/to/portable/launcher/instances/MyInstance/.minecraft/saves/MyWorld" --path ~/Documents
```

## Notes

- Flatpak and Snap versions of launchers store data in sandboxed locations
- The program automatically detects and searches these alternative paths
- Instance names are case-insensitive when using `--instance`
- World names must still exist within the selected instance's saves folder
