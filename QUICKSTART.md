# TypeBeat - Quick Start Guide

## For End Users

### Installation

1. Download `TypeBeatSetup.exe` from the releases page
2. Run the installer
3. TypeBeat will be installed to `%LocalAppData%\TypeBeat`
4. A desktop shortcut will be created automatically
5. Launch and enjoy!

### Updates

TypeBeat checks for updates automatically when you start the game. When an update is available:

1. A notification will appear in the top-right corner
2. Click **"Update Now"** to download and install
3. The game will restart automatically
4. You're running the latest version!

### Uninstallation

1. Go to Windows Settings → Apps → Installed Apps
2. Find "TypeBeat"
3. Click Uninstall
4. Or use `Update.exe --uninstall` in the installation directory

---

## For Developers

### Building from Source

```bash
# Clone the repository
git clone https://github.com/YourUsername/TypeBeat.git
cd TypeBeat

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project TypeBeat.Desktop
```

### Creating a Release Build

**Option 1: Using PowerShell Script (Recommended)**
```powershell
.\build-release.ps1 -Version "1.0.0"
```

**Option 2: Using Batch File**
```batch
build-release.bat 1.0.0
```

**Option 3: Manual**
```bash
dotnet publish TypeBeat.Desktop\TypeBeat.Desktop.csproj -c Release -r win-x64
```

### Setting Up Auto-Updates

1. **Configure Update URL**
   
   Edit `TypeBeat.Game\Configuration\UpdateConfig.cs`:
   ```csharp
   return "https://github.com/YourUsername/TypeBeat/releases";
   ```

2. **Build Release Package**
   ```powershell
   .\build-release.ps1 -Version "1.0.0"
   ```

3. **Upload to GitHub Releases**
   - Create a new release on GitHub
   - Upload all files from `.\Releases` folder
   - Tag the release (e.g., `v1.0.0`)

4. **Test Updates**
   - Install the initial version
   - Create a new version (1.0.1)
   - Upload to releases
   - Launch installed app - should detect update!

### Project Structure

```
TypeBeat/
├── TypeBeat.Desktop/          # Desktop entry point
├── TypeBeat.Game/             # Core game logic
│   ├── Updates/               # Update system
│   ├── Overlays/              # UI overlays
│   ├── Configuration/         # Config files
│   └── ...
├── TypeBeat.Resources/        # Game assets
├── build-release.ps1          # Build script
└── DEPLOYMENT.md              # Full deployment guide
```

### Development Workflow

1. Make changes to the code
2. Test locally: `dotnet run --project TypeBeat.Desktop`
3. Increment version in `TypeBeat.Desktop.csproj`
4. Build release: `.\build-release.ps1 -Version "X.Y.Z"`
5. Upload to releases
6. Users get automatic updates!

### Update System Architecture

```
Game Startup
    ↓
Check UpdateConfig.UpdateUrl
    ↓
Connect to Update Server
    ↓
Read RELEASES file
    ↓
Compare versions
    ↓
[If newer version exists]
    ↓
Show UpdateNotificationOverlay
    ↓
[User clicks "Update Now"]
    ↓
Download delta/full package
    ↓
Apply update
    ↓
Restart game
```

### Configuration Options

Edit `TypeBeat.Game\Configuration\UpdateConfig.cs`:

- `UpdateUrl` - Where to check for updates
- `CheckOnStartup` - Check for updates on launch
- `AutoDownload` - Download updates automatically
- `ShowNotifications` - Show update notifications
- `CheckIntervalMs` - How often to check (milliseconds)

### Testing Updates Locally

1. Build version 1.0.0:
   ```powershell
   .\build-release.ps1 -Version "1.0.0"
   ```

2. Install it by running `TypeBeatSetup.exe` from `.\Releases`

3. Build version 1.0.1:
   ```powershell
   .\build-release.ps1 -Version "1.0.1"
   ```

4. Host the releases folder locally:
   ```bash
   cd Releases
   python -m http.server 8000
   ```

5. Update `UpdateConfig.cs` to use `http://localhost:8000`

6. Launch the installed 1.0.0 version - it should detect 1.0.1!

### Troubleshooting

**Build fails**
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` first
- Check for compilation errors

**Updates not detected**
- Verify UpdateUrl is accessible
- Check RELEASES file is present on server
- Review logs: `%LocalAppData%\TypeBeat\logs`

**Squirrel not found**
- Run: `dotnet tool install --global Clowd.Squirrel`
- Or let the build script install it automatically

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

### Resources

- Full Deployment Guide: `DEPLOYMENT.md`
- Squirrel Documentation: https://github.com/clowd/Clowd.Squirrel
- osu!framework: https://github.com/ppy/osu-framework

---

## Support

- **Issues**: https://github.com/YourUsername/TypeBeat/issues
- **Discussions**: https://github.com/YourUsername/TypeBeat/discussions
- **Wiki**: https://github.com/YourUsername/TypeBeat/wiki

## License

[Your License Here]
