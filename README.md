# Custom Notification Position (customNotifyPos)

Move and resize Windows 11 / 10 notifications and Microsoft Teams notifications to any custom position on the screen.

## Supported Notifications
- **Windows System Notifications:** Windows 10 & 11 Toast notifications (Action Center / `Windows.UI.Core.CoreWindow`).
- **Microsoft Teams (New):** Teams 2.1+ built on Edge WebView2 (`ms-teams.exe`, window class `TeamsWebView`).
- **Microsoft Teams (Classic):** Legacy Electron-based Teams (`Teams.exe`, window class `Chrome_WidgetWin_1`).

## Usage
- Run `customNotifyPos.exe`.
- **Startup Preview (TestNotify):** On launch, a Fluent dark-themed preview banner appears at the saved position (or Top Center by default). Use it to customize position and size immediately:
  - **Move:** Plain **Left Click & Drag** anywhere on the banner (no `Alt` needed).
  - **Resize:** Drag any border or corner.
  - **Close:** Click the **X** button at the top-right corner.
  - **Auto-dismiss:** Closes automatically after 20 seconds of inactivity (timer resets while cursor hovers over it).
- Add a shortcut to `%appdata%\Microsoft\Windows\Start Menu\Programs\Startup` if you want it to run on Windows startup.

## Controls & Customization
- **Move Notifications:**
  - **Test Preview Banner:** Plain **Left Click & Drag** anywhere on the banner.
  - **Real Notifications (Windows / Teams):** Hold `Alt` and **Left Click & Drag** anywhere on the notification.
  - Position is saved to `config.ini` upon releasing the mouse button.
- **Resize Notifications:** Move cursor to any border or corner (cursor changes to resize arrows `↔`, `↕`, `↖`, `↗`). **Left Click & Drag** to expand or shrink (minimum 250×80 px). Child render widgets for Teams reflow automatically, and size is saved to `config.ini`. No `Alt` key needed.
- **Reset Position & Size:** Hold `Alt` and **Right Click** any notification to reset it immediately back to default Top Center position and default dimensions (removes saved settings from `config.ini`).

## Configuration
Settings are stored in `config.ini` next to the executable:

```ini
[CustomNotifyPos]
Custom=true
X=1169        ; offset from the left edge of the monitor (px)
Y=31          ; offset from the top edge of the monitor (px)
CustomSize=true
Width=380     ; notification width in px
Height=110    ; notification height in px
```

- Coordinates are physical pixels (Per-Monitor DPI Aware V2), relative to the monitor containing the notification.
- Legacy `[TopCenter]`, `[TopRight]`, `[TopLeft]` sections are automatically migrated and cleaned up on reset.

## Notes
- **Single Instance Only:** Guarded by a named Win32 Mutex; launching a duplicate instance exits immediately.
- **Ultra-low Resource Consumption:** Pure Win32 P/Invoke with zero WinForms dependencies. Working set is automatically trimmed via Win32 API to ~1.5 – 3 MB RAM when idle.
- **Multi-DPI & Resolution Adaptive:** Fully Per-Monitor V2 DPI aware. Automatically adapts and keeps notifications visible when switching monitor resolutions or remote desktop (RDP) sessions without overwriting config.

## Compiling
- Clone the repository.
- Run `build.cmd` or compile via Windows built-in .NET Framework compiler:
  ```cmd
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /platform:anycpu32bitpreferred /out:"customNotifyPos.exe" "customNotifyPos.cs"
  ```
- Note: No external .NET SDK or Visual Studio required.
