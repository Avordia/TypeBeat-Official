# TypeBeat

A typing rhythm game built with osu!framework featuring auto-update functionality.

## Installation

### For End Users

Download `TypeBeatSetup.exe` from the [Releases](https://github.com/YourUsername/TypeBeat/releases) page and run the installer. The game will automatically check for updates when launched.

### For Developers

```bash
# Clone and build
git clone https://github.com/YourUsername/TypeBeat.git
cd TypeBeat
dotnet restore
dotnet build
dotnet run --project TypeBeat.Desktop
```

See [QUICKSTART.md](QUICKSTART.md) for detailed build instructions.

## Creating Releases

Build an installable package with auto-update support:

```powershell
.\build-release.ps1 -Version "1.0.0"
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for complete deployment guide.

## Configuration

To set up backend authentication and online features:

1. Copy `TypeBeat.Game/Configuration/appsettings.template.json` to `TypeBeat.Game/Configuration/appsettings.json`
2. Fill in the `BackendUrl` and `BackendApiKey` values with actual credentials (available from project maintainers)
3. Never commit `appsettings.json` to the repository

Alternatively, you can use environment variables:
- `TYPEBEAT_BACKEND_URL` - Your backend URL
- `TYPEBEAT_BACKEND_KEY` - Your backend API key

If no configuration is provided, the game will run in offline mode only.