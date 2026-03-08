# GhostCapture V1 Architecture

## Product direction

GhostCapture is being shaped as a Windows-first launcher for Android screen mirroring with two priorities:

1. Lowest practical end-to-end latency
2. Fast connection flow for USB and Wi-Fi debugging

V1 keeps the render/control engine delegated to `scrcpy` so the project can inherit its low-latency path instead of rebuilding the media stack from scratch.

## Runtime model

The application is split into four responsibilities:

- `AdbService`: device discovery, `mdns` lookup, pairing, and connect commands
- `WirelessPairingService`: QR payload generation and Wi-Fi pairing/connect orchestration
- `ScrcpyService`: low-latency process launch profile for mirroring
- `MainWindow`: Windows UI for connection state, launch, and in-window Wi-Fi pairing animation

## Wi-Fi debugging QR flow

The intended pairing flow follows the Android ADB Wi-Fi design:

- Generate a QR payload in the `WIFI:T:ADB;S:<service>;P:<secret>;;` format
- Wait for the phone to scan the QR and publish `_adb-tls-pairing._tcp` with the requested service name
- Resolve the endpoint via `adb mdns services`
- Run `adb pair <host:port> <secret>`
- Wait for auto-connect or trigger `adb connect` fallback
- Launch `scrcpy` immediately after a ready device is visible

The current implementation includes the QR payload, pairing orchestration, and a rendered QR bitmap inside the main window through a morphing Wi-Fi card transition.

The QR payload format and mDNS pairing/connect flow are aligned with the official ADB Wi-Fi design docs:

- <https://android.googlesource.com/platform/packages/modules/adb/+/HEAD/docs/dev/adb_wifi.md>
- <https://android.googlesource.com/platform/packages/modules/adb/+/refs/heads/main/docs/user/adb.1.md>

## Low-latency launch profile

The initial `scrcpy` profile is tuned for latency-first behavior:

- `--video-codec=h264`
- `--video-buffer=0`
- `--audio-buffer=20`
- `--audio-output-buffer=5`
- `--video-bit-rate=16M`
- `--max-size=1920`
- `--max-fps=120`

These are starter defaults only. Real tuning should be validated on target phones, routers, and capture workloads.

## ADB isolation

GhostCapture runs against its own ADB server port instead of the default shared port so it can avoid conflicts with Android Studio, OEM utilities, and other software that may already be running an ADB daemon on the machine.

The current implementation uses:

- a dedicated ADB server port
- `openscreen` mDNS support for Wi-Fi debugging discovery
- the same ADB environment for `scrcpy` launches

This keeps Wi-Fi debugging discovery and connection handling inside one controlled runtime path.

## UI direction

The current UI is a compact WPF window with three main states:

- connection overview and start action
- Wi-Fi launcher card
- in-window Wi-Fi pairing mode

When Wi-Fi pairing opens:

- the other cards fade and slide away
- the Wi-Fi card expands into the center of the window
- the QR code becomes the primary focus
- pairing progress stays visible beneath the QR without opening a second window

The target is a calm, minimal interface that stays understandable without long instructions.

## Workspace assumptions

Current workspace bundles:

- `C:\Project_winsurf\GhostCapture\scrcpy-win64-v3.3.4`
- `C:\Project_winsurf\GhostCapture\tools\scrcpy`

The canonical source bundle is:

- `tools\scrcpy\adb.exe`
- `tools\scrcpy\scrcpy.exe`

The publish output is flattened so the installed application can resolve its bundled binaries from the application directory itself, not from a visible `tools` folder in the installed program.

Installer assets:

- `installer\GhostCapture.iss`
- `scripts\Build-Installer.ps1`

## Build note

Current build output is produced as:

- published app files in `artifacts\publish\win-x64`
- installer in `artifacts\installer\GhostCapture-Setup.exe`

The installer flow packages the application as an installable Windows program and does not rely on a visible external `tools` folder next to the final executable.
