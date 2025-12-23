# Volume Mixer Plugin for Macro Deck

A **Macro Deck 2** plugin that provides Windows audio control directly from your macro pad. Monitor and control audio devices, per-application volumes, and default device selection — a lightweight alternative to Voicemeeter Banana for basic audio routing needs.

---

## Features

### Current
- **Default Audio Device Info**: See which playback device is currently set as default (Multimedia & Communications roles)
- **Per-Application Volume Control**: View and adjust volume for individual apps (Spotify, Discord, games, etc.)
- **Mute/Unmute Apps**: Toggle mute state for specific applications
- **Switch Default Device**: Change the default playback device with a button press
- **Device Enumeration**: List all active audio output devices

### Planned
- **Volume Sliders**: Expose volume levels as Macro Deck variables for dynamic button labels
- **Audio Session Monitoring**: Real-time updates when apps start/stop playing audio
- **Input Device Support**: Microphone selection and monitoring

---

## Requirements

- **Macro Deck 2** (v2.9.0 or later recommended)
- **Windows 10/11** (uses Windows Core Audio API)
- **.NET 8.0 Runtime** (included with Macro Deck)

---

## Installation

### From Extension Store (Recommended)
> *Coming soon — once published to the Macro Deck Extension Store*

### Manual Installation

1. **Download** the latest release `.zip` from the [Releases](../../releases) page
2. **Extract** the folder contents
3. **Copy** the extracted folder to:
   ```
   %appdata%\Macro Deck\plugins\
   ```
4. **Restart** Macro Deck
5. The plugin should appear in **Settings > Extensions**

### Build from Source

#### Quick Build & Deploy (Recommended)

A PowerShell script is provided for building and automatically deploying to Macro Deck:

```powershell
# Build Debug configuration and deploy to Macro Deck
.\build-and-deploy.ps1

# Build Release configuration and deploy
.\build-and-deploy.ps1 -Release

# Deploy existing build without rebuilding
.\build-and-deploy.ps1 -SkipBuild
```

The script will:
1. Build the project in the specified configuration
2. Stop Macro Deck if it's running
3. Copy required files to `%appdata%\Macro Deck\plugins\VolumeMixerPlugin\`
4. Restart Macro Deck automatically

#### Manual Build

```bash
# Clone the repository
git clone <repo-url>
cd VolumeMixerPlugin

# Build in Release mode
dotnet build -c Release

# Output DLL will be in:
# bin/Release/net8.0-windows/VolumeMixerPlugin.dll
```

Copy the following files to `%appdata%\Macro Deck\plugins\VolumeMixerPlugin\`:
- `VolumeMixerPlugin.dll`
- `VolumeMixerPlugin.pdb`
- `NAudio.dll`
- `NAudio.Core.dll`
- `NAudio.Wasapi.dll`
- `ExtensionManifest.json`

---

## Usage

### Variables (Exposed to Macro Deck)

| Variable Name | Type | Description |
|---------------|------|-------------|
| `volumemixer_default_device` | String | Name of the current default multimedia device |
| `volumemixer_default_device_id` | String | Device ID (for internal use) |
| `volumemixer_comm_device` | String | Name of the current default communications device |
| `volumemixer_devices` | String | List of all active audio output devices |
| `volumemixer_app_<name>_volume` | Integer | Volume level (0-100) for a specific app |
| `volumemixer_app_<name>_muted` | Bool | Mute state for a specific app |

### Actions

| Action | Description | Configuration |
|--------|-------------|---------------|
| **Set App Volume** | Set volume for a specific application | App name, Volume (0-100) |
| **Volume Up/Down** | Increment/decrement app volume by 5% | App name |
| **Mute App** | Toggle mute state for a specific application | App name |
| **Set Default Device** | Change the default playback device | Device name or ID |
| **Refresh Devices** | Force refresh of audio device list | — |

### Example Button Configurations

**Show Current Default Device:**
- Label: `{volumemixer_default_device}`
- No action needed — variable updates automatically

**Spotify Volume Control:**
- Label: `Spotify: {volumemixer_app_spotify_volume}%`
- Action: Volume Up (App: Spotify)
- Long Press Action: Volume Down (App: Spotify)

---

## Technical Details

### Audio API
This plugin uses **NAudio** with the Windows **Core Audio API** (WASAPI) to:
- Enumerate audio endpoints (`MMDeviceEnumerator`)
- Access audio sessions per device (`AudioSessionManager`)
- Control per-application volume (`SimpleAudioVolume`)

### Memory Management
The plugin implements proper COM object disposal to prevent memory leaks:
- Single `MMDeviceEnumerator` instance, disposed on plugin disable
- Periodic refresh of audio sessions (configurable interval)
- Proper cleanup of `AudioSessionControl` references

### Limitations
- **Windows only** — Core Audio API is not available on Linux/macOS
- **Render devices only** (playback) — Input device support is planned
- **No audio routing** — Cannot route audio between devices like Voicemeeter
- **Process-based detection** — Apps are identified by process name, not window title

---

## Troubleshooting

### Plugin not loading
- Ensure Macro Deck 2.9.0+ is installed
- Check that the DLL is in the correct `plugins` folder
- Look for errors in Macro Deck logs: `%appdata%\Macro Deck\logs\`

### Apps not showing
- The app must be actively producing audio to appear
- Some system sounds may show as empty/unnamed sessions
- UWP apps may show under a different process name

### Volume changes not applying
- Ensure the app is not locked by another volume controller
- Some apps ignore external volume changes (rare)

---

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a Pull Request

For bugs or feature requests, open an issue.

---

## License

MIT License — See [LICENSE](LICENSE) for details.

---

## Credits

- **NAudio** — Audio library for .NET
- **Macro Deck** — The excellent macro pad software by SuchByte
- Inspired by the need for simple audio control without Voicemeeter overhead
