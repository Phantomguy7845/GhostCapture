# GhostCapture

GhostCapture is a Windows desktop launcher for fast Android screen mirroring built on top of `adb` and `scrcpy`.

It is designed around a simple flow:

- open the app
- see whether a device is connected
- start mirroring immediately over USB
- pair once with Android Wi-Fi debugging by scanning a QR code
- reconnect wirelessly with the same low-friction UI

## What it does

- Detects Android devices through `adb`
- Distinguishes USB and wireless connections in the UI
- Uses a private ADB server port so GhostCapture does not fight with other ADB clients
- Generates Android-compatible Wi-Fi debugging QR payloads
- Pairs with wireless debugging through `adb mdns services`, `adb pair`, and connect fallback
- Launches `scrcpy` with a latency-first profile
- Starts `scrcpy` without a terminal window
- Packages everything into a Windows installer instead of a portable tools folder

## Tech stack

- `.NET 8`
- `WPF`
- `QRCoder`
- bundled `adb`
- bundled `scrcpy`
- `Inno Setup` for `setup.exe`

## Project layout

- `GhostCapture.sln`: solution entry point
- `src/GhostCapture.App`: WPF application
- `docs/ghostcapture-v1-architecture.md`: architecture notes
- `scripts/Build-Installer.ps1`: publish + installer build script
- `scripts/Sync-ScrcpyTools.ps1`: refresh bundled `adb` and `scrcpy` files
- `installer/GhostCapture.iss`: Inno Setup script
- `tools/scrcpy`: bundled runtime files used for publish/install

## Running locally

Requirements:

- Windows 10 or 11
- .NET 8 SDK
- Inno Setup 6 if you want to build the installer

Run the app in development:

```powershell
dotnet run --project .\src\GhostCapture.App\GhostCapture.App.csproj
```

Build the installer:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Installer.ps1
```

Installer output:

- `artifacts\installer\GhostCapture-Setup.exe`

## Usage

### USB

1. Enable Developer options and USB debugging on the Android device.
2. Connect the phone with a USB cable.
3. Open GhostCapture.
4. Press `Start Screen`.

### Wi-Fi debugging

1. Make sure the phone and PC are on the same Wi-Fi network.
2. On Android, open Developer options and enable Wireless debugging.
3. Choose the QR pairing option on the phone.
4. In GhostCapture, press `Connect via Wi-Fi`.
5. Scan the QR code shown in the app.
6. Wait for pairing and connection, then mirroring starts automatically.

## Latency direction

GhostCapture is tuned for responsiveness first.

- USB is the best mode for games and the lowest practical latency.
- Wi-Fi debugging is treated as a serious low-latency mode, but it still depends on the phone, router, signal quality, and local network conditions.
- The default `scrcpy` launch profile prefers low buffering and fast display over maximum visual quality.

## Notes

- The repository includes the runtime files needed to launch `adb` and `scrcpy` during development and packaging.
- Build artifacts and the original extracted upstream bundle are intentionally ignored from Git.
- The app currently targets one Android device at a time.

## Acknowledgements

GhostCapture builds on the work of:

- [`scrcpy`](https://github.com/Genymobile/scrcpy)
- Android `adb` Wi-Fi debugging
