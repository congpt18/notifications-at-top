A forked from SamarthCat
# Custom Notification Position (customNotifyPos)

Move and resize Windows 11 / 10 notifications and Microsoft Teams notifications to any custom position on the screen.

# Supported Notifications
- **Windows System Notifications:** Windows 10 & 11 Toast notifications (Action Center / `Windows.UI.Core.CoreWindow`).
- **Microsoft Teams (New):** Teams 2.1+ built on Edge WebView2 (`ms-teams.exe`).
- **Microsoft Teams (Classic):** Legacy Electron-based Teams (`Teams.exe`).
- **Microsoft Teams (New):** Teams 2.1+ built on Edge WebView2 (`ms-teams.exe`, window class `TeamsWebView`).
- **Microsoft Teams (Classic):** Legacy Electron-based Teams (`Teams.exe`, window class `Chrome_WidgetWin_1`).

# Usage

- Run ``` customNotifyPos.exe ```
- **Startup Test Notification:** On launch, a **TestNotify** banner automatically appears for 7 seconds so you can immediately preview, move (`Alt + Left Click & Drag`), and resize (drag borders) your preferred notification position.
- **Startup Preview (TestNotify):** On launch, a dark **Notification Preview** banner appears at the saved position (or Top Center by default). Use it to set your preferred position/size right away:
  - **Move:** plain **Left Click & Drag** anywhere on the banner (no `Alt` needed). It can be dragged across the whole screen.
  - **Resize:** drag any border or corner.
  - **Close:** click the **X** at the top-right corner.
  - **Auto-dismiss:** closes after 20 seconds; the timer resets while the cursor hovers over it.
- Add a shortcut to it in ``` %appdata%\Microsoft\Windows\Start Menu\Programs\Startup ``` if you want it to run on boot.

# Controls & Customization

- **Move Anywhere:** Hold `Alt` and **Left Click & Drag** any notification to place it anywhere on screen. The new position will be automatically saved to `config.ini`.
- **Resize Notification:** Move your mouse cursor to any border or corner of the notification. The cursor changes to resize arrows (`↔`, `↕`, `↖`, `↗`). **Left Click & Drag** the border to expand or shrink the notification. Content automatically adapts/reflows, and dimensions are saved to `config.ini`.
- **Reset Position & Size:** Hold `Alt` and **Right Click** the notification to reset it back to the default Top Center position and default dimensions.
The window under the mouse cursor is the one being moved/resized, so the preview and real notifications never conflict.

- **Move Anywhere:** Hold `Alt` and **Left Click & Drag** any real notification (Windows / Teams) to place it anywhere on screen. The new position is saved to `config.ini` on mouse release.
- **Resize Notification:** Move the cursor to any border or corner (cursor changes to resize arrows). **Left Click & Drag** to expand or shrink (min 250x80 px). Child render hosts of Teams are resized as well, and dimensions are saved to `config.ini`. No `Alt` needed.
- **Reset Position & Size:** Hold `Alt` and **Right Click** the notification to reset it to the default Top Center position and default dimensions (removes the saved section from `config.ini`).

# Configuration

Settings are stored in `config.ini` next to the executable:

```ini
[CustomNotifyPos]
Custom=true
X=1169        ; offset from the left edge of the monitor (px)
Y=31          ; offset from the top edge of the monitor (px)
CustomSize=true
Width=380
Height=110
```

- Coordinates are physical pixels (Per-Monitor DPI Aware V2), relative to the monitor containing the notification.
- Legacy `[TopCenter]`, `[TopRight]`, `[TopLeft]` sections are removed on reset.

# Notes

- Single instance only (guarded by a mutex); launching a second copy exits immediately.
- Lightweight: pure Win32 P/Invoke, no WinForms; working set is trimmed when idle.

# Compiling
- Clone the repository.
- Run ``` build.cmd ``` to compile `customNotifyPos.exe`.
- Note: No need to install any external .NET SDK; uses the built-in Windows .NET Framework 4.0 compiler.
 