# Gasvar Macro

Windows desktop macro tool built with .NET 8 and WinForms. Includes three modes:

- **Vortex Nexus** — automated click sequences with configurable timing and screen regions
- **AutoClicker** — repeated clicks at the cursor (finite or infinite, with optional jitter)
- **Recorder** — record and play back mouse/keyboard macros with relative coordinates

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and run

```bash
cd AutoClicker
dotnet build AutoClicker.sln
dotnet run --project AutoClicker/AutoClicker.csproj
```

## Publish (self-contained)

```bash
dotnet publish AutoClicker/AutoClicker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `AutoClicker/AutoClicker/bin/Release/net8.0-windows/win-x64/publish/GasvarMacro.exe`

## Settings location

Settings are stored in:

`%AppData%\GasvarMacro\app-settings.json`

Recorded macros saved to the library go to:

`%AppData%\GasvarMacro\macros\`

On first launch, legacy `app-settings.json` or `macro-settings.json` next to the executable are migrated automatically.

## Default hotkeys

| Action | Hotkey |
|--------|--------|
| Nav Vortex | F1 |
| Nav AutoClicker | F2 |
| Nav Recorder | F3 |
| Start | F9 |
| Stop | F10 |
| Play (recorder) | F7 |
| Save settings | Ctrl+Alt+S |
| Reset | Ctrl+Alt+R |
| Import macro | Ctrl+Alt+I |
| Export macro | Ctrl+Alt+E |
| Vortex pause | Up Arrow |

All hotkeys are configurable in the header panel. Conflicts are logged at startup.

## Features

- Global hotkeys work while the app is in the system tray
- Status overlay during macro execution (optional)
- Vortex profiles for different coordinate layouts
- Coordinate picker for Vortex X/Y fields
- Macro library with save/load/delete
- Loop playback for recorded macros
- Full settings import/export (JSON)
- Start with Windows (optional, in Settings)

## Tests

```bash
dotnet test AutoClicker/AutoClicker.Tests/AutoClicker.Tests.csproj
```
