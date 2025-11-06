# macOS File Access Issues

## Overview

When running SaplingFS on macOS, you may encounter access restrictions when scanning system directories due to macOS security features introduced in macOS Mojave (10.14) and later.

## Common Access Issues

### 1. System Integrity Protection (SIP)

macOS System Integrity Protection prevents access to critical system directories, even with `sudo`:

**Protected Directories:**
- `/System`
- `/usr` (except `/usr/local`)
- `/bin`
- `/sbin`
- `/var` (partially)

**Error Messages:**
```
Operation not permitted
```

**Solution:** SaplingFS automatically skips directories it cannot access. To avoid scanning these directories entirely, use the `--blacklist` option:

```bash
./SaplingFS MyWorld --path / --blacklist "/System;/usr;/bin;/sbin"
```

### 2. Privacy Protection (TCC - Transparency, Consent, and Control)

macOS requires explicit user permission to access certain user directories:

**Protected User Directories:**
- `~/Desktop`
- `~/Documents`
- `~/Downloads`
- `~/Pictures`
- `~/Movies`
- `~/Music`
- `~/Library/Safari`
- `~/Library/Mail`

**Error Messages:**
```
Access denied to directory: /Users/username/Desktop
```

**Solution:**

1. **Grant Full Disk Access** (Recommended for scanning entire filesystem):
   - Open **System Settings** → **Privacy & Security** → **Full Disk Access**
   - Click the **+** button
   - Navigate to the SaplingFS executable and add it
   - You may need to unlock with your password

2. **Grant Specific Permissions:**
   - When SaplingFS first tries to access a protected directory, macOS will show a permission dialog
   - Click "OK" to grant access
   - This grants access only to that specific directory

3. **Use `--blacklist` to Skip Protected Directories:**
   ```bash
   ./SaplingFS MyWorld --blacklist "/Users/username/Library"
   ```

### 3. Terminal/iTerm Permissions

If running SaplingFS from Terminal or iTerm2, the terminal application itself needs permission:

**Solution:**
1. Open **System Settings** → **Privacy & Security** → **Full Disk Access**
2. Add **Terminal** or **iTerm2** to the list
3. Restart the terminal application

### 4. External Volumes and Network Drives

Some external volumes may have restricted permissions:

**Common Issues:**
- Time Machine backups (`/Volumes/Time Machine Backups`)
- Network drives with limited credentials
- Encrypted volumes

**Solution:**
```bash
# Skip external volumes
./SaplingFS MyWorld --path /Users/username --blacklist "/Volumes"

# Or specify only local directories
./SaplingFS MyWorld --path /Users/username/Projects
```

## Recommended Usage Patterns

### Safe Scanning (No System Files)

```bash
# Scan only your home directory
./SaplingFS MyWorld --path /Users/username

# Scan specific project directories
./SaplingFS MyWorld --path /Users/username/Projects
```

### Full System Scan

```bash
# Scan entire filesystem (requires Full Disk Access)
./SaplingFS MyWorld --path / --blacklist "/System;/usr;/bin;/sbin;/Volumes"
```

### Development/Testing

```bash
# Scan a safe test directory
mkdir ~/SaplingTest
./SaplingFS MyWorld --path ~/SaplingTest
```

## Granting Full Disk Access Step-by-Step

1. **Build or locate the SaplingFS executable:**
   ```bash
   dotnet build -c Release
   # Executable will be at: src/bin/Release/net9.0/SaplingFS
   ```

2. **Copy to a permanent location:**
   ```bash
   mkdir -p ~/Applications
   cp src/bin/Release/net9.0/SaplingFS ~/Applications/
   ```

3. **Grant Full Disk Access:**
   - Open **System Settings** (macOS Ventura+) or **System Preferences** (older)
   - Navigate to **Privacy & Security** → **Full Disk Access**
   - Click the lock icon and authenticate
   - Click the **+** button
   - Press **Cmd+Shift+G** to open "Go to folder"
   - Enter the path to your executable (e.g., `/Users/username/Applications/SaplingFS`)
   - Select the file and click **Open**
   - Enable the checkbox next to SaplingFS

4. **Verify access:**
   ```bash
   ~/Applications/SaplingFS MyWorld --path /Users/username
   ```

## Troubleshooting

### "Operation not permitted" despite Full Disk Access

**Cause:** Full Disk Access may not be properly registered.

**Solution:**
1. Remove SaplingFS from Full Disk Access list
2. Re-add it
3. Restart your Mac
4. Try again

### "Developer cannot be verified" error

**Cause:** macOS Gatekeeper blocks unsigned executables.

**Solution:**
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine ~/Applications/SaplingFS

# Or allow the app in System Settings
# System Settings → Privacy & Security → Security → "Open Anyway"
```

### FileScanner silently skips directories

**Cause:** This is expected behavior. SaplingFS silently skips directories it cannot access to avoid cluttering output.

**Verification:** The status screen shows the count of files found. If the count seems low, check permissions or add directories to blacklist.

## Testing File Access

To test which directories are accessible:

```bash
# Test access to a specific directory
ls -la /path/to/directory

# Check your current permissions
groups

# View SIP status
csrutil status
```

## Performance Considerations

Scanning directories with many permission issues can slow down the file scanner:

- **Blacklist known problematic directories** to improve performance
- **Grant Full Disk Access** to avoid permission checks for each directory
- **Use specific paths** instead of scanning from root (`/`)

## Security Notes

**WARNING:** Granting Full Disk Access allows SaplingFS to:
- Read any file on your system
- Delete files when `--allow-delete` is enabled
- Access sensitive data in protected directories

**Recommendations:**
- Only grant Full Disk Access if you need to scan the entire filesystem
- Never use `--allow-delete` with root or system directories
- Always create a backup before running SaplingFS
- Review the `--blacklist` option to exclude sensitive directories

## Platform Differences

### macOS vs. Linux vs. Windows

| Feature | macOS | Linux | Windows |
|---------|-------|-------|---------|
| System Protection | SIP + TCC | SELinux/AppArmor | UAC |
| Protected Dirs | `/System`, `/usr`, user dirs | `/sys`, `/proc`, `/root` | `C:\Windows`, `C:\Program Files` |
| Permission Model | POSIX + ACLs + TCC | POSIX + capabilities | NTFS ACLs |
| Easy Full Access | Requires UI permission | Requires `sudo` | Requires Administrator |

## See Also

- [Apple's Privacy & Security Documentation](https://support.apple.com/guide/mac-help/control-access-to-files-and-folders-on-mac-mchld5a35146/mac)
- [System Integrity Protection](https://support.apple.com/en-us/HT204899)
- [FileScanner.cs Implementation](../src/Services/FileScanner.cs)
