# TypeBeat Deployment Guide

This guide explains how to build, package, and deploy TypeBeat with auto-update functionality.

## Prerequisites

- .NET 8.0 SDK
- PowerShell 5.1 or higher
- Clowd.Squirrel (installed automatically by build script)

## Building a Release

### 1. Update Version Number

Edit `TypeBeat.Desktop\TypeBeat.Desktop.csproj` and update the version:

```xml
<Version>1.0.1</Version>
<FileVersion>1.0.1</FileVersion>
```

### 2. Run the Build Script

```powershell
.\build-release.ps1 -Version "1.0.1"
```

This will:
- Build the application in Release configuration
- Create a self-contained package
- Generate Squirrel installer and update packages
- Output everything to `.\Releases` directory

## Distribution Methods

### Method 1: GitHub Releases (Recommended)

1. Create a new release on GitHub
2. Upload all files from the `Releases` folder
3. Update the `UpdateManager` URL in `TypeBeat.Game\Updates\UpdateManager.cs`:

```csharp
this.updateUrl = updateUrl ?? "https://github.com/YourUsername/TypeBeat/releases";
```

### Method 2: Custom Update Server

1. Set up a web server (nginx, IIS, or any HTTP server)
2. Upload the contents of `Releases` folder to your server
3. Ensure the RELEASES file and all .nupkg files are accessible
4. Update the URL in `UpdateManager.cs`:

```csharp
this.updateUrl = updateUrl ?? "https://your-domain.com/updates";
```

The server should serve files with these MIME types:
- `.nupkg` → `application/octet-stream`
- `RELEASES` → `text/plain`

### Method 3: Azure Blob Storage / AWS S3

1. Create a public blob container or S3 bucket
2. Upload all files from `Releases` folder
3. Update the URL to your blob/bucket address
4. Ensure files are publicly readable

## How Auto-Updates Work

### Update Detection

When TypeBeat starts:
1. It connects to the update URL
2. Reads the `RELEASES` file which contains version info
3. Compares with current installed version
4. Shows update notification if newer version exists

### Update Process

1. User clicks "Update Now"
2. New version is downloaded in background
3. Files are validated and staged
4. Application restarts to apply update
5. Old version is backed up automatically

### Delta Updates

Squirrel creates delta packages between versions:
- **Full package** (~50-100MB): Complete installation
- **Delta package** (~1-10MB): Only changed files

Users get delta updates when available, making updates faster.

## First-Time Installation

Users install TypeBeat using `TypeBeatSetup.exe`:

1. Download `TypeBeatSetup.exe` from your releases
2. Run the installer
3. Application installs to `%LocalAppData%\TypeBeat`
4. Desktop shortcut created automatically
5. Updates check automatically on each launch

## Version Strategy

Follow semantic versioning:
- **Major** (X.0.0): Breaking changes
- **Minor** (1.X.0): New features
- **Patch** (1.0.X): Bug fixes

Example release flow:
```
1.0.0 → Initial release
1.0.1 → Bug fix
1.1.0 → New features
2.0.0 → Major redesign
```

## Troubleshooting

### Update Check Fails

- Verify update URL is accessible
- Check firewall settings
- Review logs in `%LocalAppData%\TypeBeat\logs`

### Installer Won't Run

- Check Windows SmartScreen settings
- Run as administrator
- Verify .NET 8.0 Runtime is installed

### Updates Don't Apply

- Ensure RELEASES file is in update directory
- Verify .nupkg files match RELEASES entries
- Check file permissions on server

## Testing Updates Locally

1. Build version 1.0.0 and install
2. Build version 1.0.1
3. Host `Releases` folder locally:
   ```powershell
   python -m http.server 8000 --directory Releases
   ```
4. Update URL in code to `http://localhost:8000`
5. Launch installed app - should detect update

## Security Considerations

- **Code Signing**: Sign your releases with a code signing certificate
- **HTTPS**: Always use HTTPS for update URLs
- **Checksums**: Squirrel validates checksums automatically
- **Permissions**: Ensure update server has proper access controls

## Advanced Configuration

### Custom Update Schedule

Modify `TypeBeatGame.cs` to check updates periodically:

```csharp
private void schedulePeriodicUpdateCheck()
{
    Scheduler.AddDelayed(() =>
    {
        checkForUpdates();
        schedulePeriodicUpdateCheck(); // Check again in 1 hour
    }, 3600000);
}
```

### Silent Updates

To update without user interaction:

```csharp
var updateInfo = await updateManager.CheckForUpdatesAsync();
if (updateInfo != null)
{
    await updateManager.DownloadAndApplyUpdateAsync(updateInfo);
    // Auto-restart on next launch
}
```

### Update Channels

Support beta/stable channels:

```csharp
var channel = ConfigManager.Get<string>("UpdateChannel"); // "stable" or "beta"
var url = channel == "beta" 
    ? "https://your-domain.com/updates-beta"
    : "https://your-domain.com/updates";
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Build Release
        run: .\build-release.ps1 -Version ${{ github.ref_name }}
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: Releases/*
```

## Support

For issues or questions:
- Check logs: `%LocalAppData%\TypeBeat\logs`
- GitHub Issues: https://github.com/YourUsername/TypeBeat/issues
- Documentation: https://github.com/YourUsername/TypeBeat/wiki
