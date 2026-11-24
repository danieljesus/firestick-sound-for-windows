# FireStick Sound for Windows

Turn a Windows PC into a Bluetooth speaker for an Amazon Fire TV Stick: the Fire TV streams its audio (A2DP) to the PC, which plays it through its default output device.

Tested on Windows 11 24H2 with a Fire TV Stick and a Broadcom BCM20702 Bluetooth adapter.

## Why this is needed

Windows can act as an A2DP sink, but only while an app holds an [`AudioPlaybackConnection`](https://learn.microsoft.com/windows/apps/develop/media-playback/enable-remote-audio-playback) open. There is no built-in setting for it.

The Fire TV also only lists Bluetooth devices whose Class of Device is audio (headphones, speakers). A PC advertises itself as *Computer / Desktop*, so it never shows up in the Fire TV pairing list. Windows lets you override the Class of Device through the [`COD Major` / `COD Type` registry values](https://learn.microsoft.com/windows-hardware/drivers/bluetooth/bluetooth-registry-entries), which this repo uses to advertise the PC as *Audio/Video / Loudspeaker*.

## Requirements

- Windows 10 2004 (build 19041) or later
- A working Bluetooth adapter (Windows uses only one Bluetooth radio at a time)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build

## Setup

1. Advertise the PC as a speaker (elevated PowerShell):

   ```powershell
   .\set-class-of-device.ps1 speaker
   ```

   If the adapter refuses to restart, reboot Windows so the new class takes effect.

2. Build and start the receiver:

   ```powershell
   dotnet build -c Release
   .\bin\Release\net10.0-windows10.0.19041.0\firestick-sound.exe
   ```

3. On the PC, open **Settings → Bluetooth & devices** and keep it open so the PC is discoverable.

4. On the Fire TV, go to **Settings → Controllers & Bluetooth Devices → Other Bluetooth Devices → Add Bluetooth Devices** and pick your PC. Accept the pairing prompt on Windows.

5. Play something on the Fire TV. The receiver logs `Opened` and the sound comes out of the PC.

The receiver must be running for audio to reach the PC. It reconnects automatically if the connection drops.

## Usage

```
firestick-sound.exe [name-filter]    Receive audio from paired sources (optionally only those whose name contains name-filter)
firestick-sound.exe --restart-radio  Turn Bluetooth off and on (no admin needed)
```

## Reverting

Once paired, the Fire TV should keep the pairing even if the PC goes back to advertising itself as a computer:

```powershell
.\set-class-of-device.ps1 computer
```

## License

[MIT](LICENSE)
