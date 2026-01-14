# Volume Mixer Plugin for Macro Deck

A **Macro Deck 2** plugin that provides Windows audio control directly from your macro pad. Monitor and control audio devices, per-application volumes, and default device selection — a lightweight alternative to Voicemeeter Banana for basic audio routing needs.

---

## Features

### Current
- **Default Audio Device Info**: See which playback device is currently set as default (Multimedia & Communications roles)
- **Default Microphone Info**: See which input device is currently set as default
- **Per-Application Volume Control**: View and adjust volume for individual apps (Spotify, Discord, games, etc.)
- **Mute/Unmute Apps**: Toggle mute state for specific applications
- **Switch Default Output**: Change the default playback device with a button press
- **Switch Default Microphone**: Change the default input device with a button press
- **Switch Output + Microphone**: Change both output and input devices together (audio profile switching)
- **Device Switching**: Switch output and/or microphone devices via actions
- **Toggle Mode**: Toggle between 2 devices with a single button press (output, microphone, or both)

### Planned
- **Volume Sliders**: Expose volume levels as Macro Deck variables for dynamic button labels
- **Audio Session Monitoring**: Real-time updates when apps start/stop playing audio

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
| `volumemixer_default_device` | String | Name of the current default multimedia output device |
| `volumemixer_comm_device` | String | Name of the current default communications output device |
| `volumemixer_default_mic` | String | Name of the current default multimedia input device |
| `volumemixer_comm_mic` | String | Name of the current default communications input device |
| `volumemixer_app_<name>_volume` | Integer | Volume level (0-100) for a tracked app (only apps used in configured actions) |
| `volumemixer_app_<name>_muted` | Bool | Mute state for a tracked app (only apps used in configured actions) |

### Actions

| Action | Description | Configuration |
|--------|-------------|---------------|
| **Set App Volume** | Set volume for a specific application | App name, Volume (0-100) |
| **Volume Up/Down** | Increment/decrement app volume by 5% | App name |
| **Mute App** | Toggle mute state for a specific application | App name |
| **Set Default Output** | Change the default playback device | Device name or ID. Toggle mode: select 2 devices to switch between them |
| **Set Default Microphone** | Change the default input device | Device name or ID. Toggle mode: select 2 microphones to switch between them |
| **Set Default Output + Microphone** | Change both output and input devices at once | Output device, Input device. Each can optionally use toggle mode |
| **Refresh Devices** | Force refresh of audio device list | — |

### Toggle Mode

All device switching actions support a **Toggle Mode** that lets you switch between 2 devices with a single button press:

1. Enable the "Toggle" checkbox in the action configuration
2. Select Device 1 and Device 2 from the dropdown lists
3. Each button press will switch to the other device
4. The plugin automatically detects the current device and switches to the alternate one

Toggle mode is available for:
- **Set Default Output**: Toggle between 2 playback devices
- **Set Default Microphone**: Toggle between 2 input devices  
- **Set Default Output + Microphone**: Toggle output and/or microphone independently

**Note:** The two devices must be different. If a device becomes unavailable (disconnected), the toggle will not execute and a warning will be logged.

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
