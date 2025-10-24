Typing Rhytmn Game

## Configuration

To set up backend authentication and online features:

1. Copy `TypeBeat.Game/Configuration/appsettings.template.json` to `TypeBeat.Game/Configuration/appsettings.json`
2. Fill in the `BackendUrl` and `BackendApiKey` values with actual credentials (available from project maintainers)
3. Never commit `appsettings.json` to the repository

Alternatively, you can use environment variables:
- `TYPEBEAT_BACKEND_URL` - Your backend URL
- `TYPEBEAT_BACKEND_KEY` - Your backend API key

If no configuration is provided, the game will run in offline mode only.