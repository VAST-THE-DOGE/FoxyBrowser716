# Running FoxyBrowser716 on Linux via Proton

FoxyBrowser716 runs on Linux through [Valve Proton](https://github.com/ValveSoftware/Proton) (a Wine-based compatibility layer).

---

## Build

On Windows, publish a self-contained build:

```powershell
dotnet publish FoxyBrowser716/FoxyBrowser716.csproj `
  -c Release `
  -p:Platform=x64 `
  -r win-x64 `
  -p:PublishProfile=UnpackagedWin-x64
```

Output folder:
```
FoxyBrowser716/bin/Release/net9.0-windows10.0.22621.0/win-x64/publish/
```

Copy the entire `publish/` folder to your Linux machine.

---

## Set Up a Wine Prefix

```bash
export WINEPREFIX="$HOME/.wine-foxybrowser"
export WINEARCH=win64
wine wineboot --init
```

---

## Install WebView2

```bash
curl -L "https://go.microsoft.com/fwlink/partner/ms-webview2-runtime-installer" \
  -o MicrosoftEdgeWebview2Setup.exe
wine MicrosoftEdgeWebview2Setup.exe /install
```

---

## Launch

### Steam (recommended)

Add `FoxyBrowser716.exe` as a non-Steam game, enable Proton ≥ 8.0 in *Properties → Compatibility*, and set launch options:

```
WINEPREFIX=$HOME/.wine-foxybrowser %command%
```

### Standalone Proton

```bash
export WINEPREFIX="$HOME/.wine-foxybrowser"
export PROTON_BIN="$HOME/.steam/steam/steamapps/common/Proton 9.0/dist/bin"
$PROTON_BIN/wine64 /path/to/publish/FoxyBrowser716.exe
```

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `STATUS_DLL_NOT_FOUND` / missing `Microsoft.ui.xaml.dll` | The publish folder is missing WinAppSDK native DLLs. Use the `UnpackagedWin-x64` publish profile (self-contained). |
| Blank / crashed WebView2 | WebView2 not installed in the prefix — follow the install step above. |
| GPU / rendering issues | Try `PROTON_USE_WINED3D=1` in the launch environment. |
