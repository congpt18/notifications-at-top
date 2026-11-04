A forked from SamarthCat

# Notifications At The Top Of The Screen
Move windows 11 / 10 notifications to the top of the screen.  

# Usage

- Run ``` topleft.exe ``` for the top left

- Run ``` topcenter.exe ``` for the top center

- Run ``` topright.exe ``` for the top right

- Add a shortcut to it in ``` %appdata%\Microsoft\Windows\Start Menu\Programs\Startup ``` if you want it to run on boot


# Customization & Drag-and-Drop

- **Drag & Drop:** Hold `Alt` and **Left Click & Drag** any notification to place it anywhere on screen. The new position will be automatically saved to `config.ini` for future notifications.
- **Reset Position:** Hold `Alt` and **Right Click** the notification to reset it back to its original preset position (`topleft`, `topcenter`, or `topright`).


# Compiling
- Clone the repo

- Run ``` start build.cmd ``` in the command prompt to compile it.

Note: You don't need to install any version of .net to compile this, the compiler is built into windows.
