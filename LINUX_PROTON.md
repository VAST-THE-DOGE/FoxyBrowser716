# Running FoxyBrowser716 on Linux via Proton

FoxyBrowser716 is a WinUI 3 / Windows App SDK application. It runs on Linux through
[Valve Proton](https://github.com/ValveSoftware/Proton) (a compatibility layer built on Wine).

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Steam with Proton (≥ 8.0 recommended) | Or a standalone Proton build |
| 64-bit Linux | x86_64 only |
| WebView2 Evergreen Runtime | Installed into the Wine prefix (see below) |

---

## Running Unpackaged on Windows (Developer / Tester)

No special installer or MSIX is needed. Just build in Release|x64 and run the exe:

```
bin\x64\Release\net9.0-windows10.0.22621.0\win-x64\FoxyBrowser716.exe
```

The custom `Program.cs` entry point automatically calls `Bootstrap.Initialize()` before
`Application.Start()` when no MSIX package identity is detected, so the app works from
the plain output folder.

**Rider / Visual Studio:** Use the **"FoxyBrowser716-WinUI (Unpackaged)"** launch profile
that is already defined in `Properties/launchSettings.json`. In Visual Studio it appears in
the launch-profile dropdown next to the Run button. In Rider, select it in
*Run → Edit Configurations → Launch profile*.

---

## Build a Self-Contained Release

On Windows (or in a CI pipeline), publish a self-contained unpackaged build:

```powershell
dotnet publish FoxyBrowser716/FoxyBrowser716.csproj `
  -p:PublishProfile=UnpackagedWin-x64
```

The output lands in:
```
FoxyBrowser716/bin/Release/net9.0-windows10.0.22621.0/win-x64/publish/
```

Copy the entire `publish/` folder to your Linux machine.

---

## Set Up a Dedicated Wine Prefix

Using a dedicated prefix keeps FoxyBrowser's Windows dependencies isolated.

```bash
export WINEPREFIX="$HOME/.wine-foxybrowser"
export WINEARCH=win64
```

Create the prefix (Proton will initialise it on first use, or force it now):

```bash
# Using plain Wine to bootstrap:
wine wineboot --init
```

---

## Install WebView2

FoxyBrowser uses the system (Evergreen) WebView2 Runtime.
Install it into the Wine prefix once:

```bash
# Download the standalone WebView2 bootstrapper from Microsoft:
curl -L "https://go.microsoft.com/fwlink/partner/ms-webview2-runtime-installer" \
  -o MicrosoftEdgeWebview2Setup.exe

wine MicrosoftEdgeWebview2Setup.exe /install
```

> **Tip:** If the interactive installer doesn't work in Wine, use the fixed-version
> offline installer instead — search Microsoft's documentation for
> "WebView2 Runtime fixed version distribution".

---

## Install Visual C++ Redistributable (if needed)

The Windows App SDK may require the VC++ 2019/2022 x64 runtime. If the app fails to
start with a missing DLL error, install it:

```bash
# Download from https://aka.ms/vs/17/release/vc_redist.x64.exe
wine vc_redist.x64.exe /install /quiet /norestart
```

---

## Launch FoxyBrowser716

### Using Steam's Proton (recommended)

Add a non-Steam game in Steam, set the target to `FoxyBrowser716.exe`, then in
*Properties → Compatibility* enable "Force the use of a specific Steam Play
compatibility tool" and select Proton ≥ 8.0.

Set the launch options in Steam:
```
WINEPREFIX=$HOME/.wine-foxybrowser %command%
```

### Using a standalone Proton build

```bash
export WINEPREFIX="$HOME/.wine-foxybrowser"

# Replace with the path to your proton/dist/bin directory:
export PROTON_BIN="$HOME/.steam/steam/steamapps/common/Proton 9.0/dist/bin"

$PROTON_BIN/wine64 /path/to/publish/FoxyBrowser716.exe
```

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `STATUS_DLL_NOT_FOUND` / missing `Microsoft.ui.xaml.dll` | The publish folder is missing WinAppSDK native DLLs. Make sure you used the `UnpackagedWin-x64` publish profile (self-contained). |
| Blank / crashed WebView2 tab | WebView2 not installed in the prefix — follow the install step above. |
| GPU rendering issues | Try adding `PROTON_USE_WINED3D=1` or `DXVK_HUD=0` to the launch environment; on some hardware D3D12 via DXVK works better, on others WineD3D is more stable. |
| Window shows but is entirely transparent | Try `WINEDEBUG=-all DXVK_ASYNC=1`. |
| App shows the packaged-launch error | The bootstrapper detected it is unpackaged (expected). If it crashes, it is most likely a missing DLL from the bullet above. |

---

## Known Limitations under Proton

- **Windows notifications** (`ToastNotification`) are not supported; the app does not use them currently.
- **Startup-with-Windows task** (`StartupTask`) is disabled automatically when running unpackaged.
- **System file/folder pickers** work via WinRT's `FileOpenPicker`/`FolderPicker` with the `InitializeWithWindow` interop; behaviour inside Wine may vary.
- **GPU-accelerated WebView2** may need DXVK; software rendering fallback is always available.
