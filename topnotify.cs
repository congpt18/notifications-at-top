using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;

public enum NotificationPosition {
    TopLeft,
    TopRight,
    TopCenter
}

public class Program {

    #region Native Stuff
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    const uint MONITOR_DEFAULTTONEAREST = 2;

    const int VK_LBUTTON = 0x01;
    const int VK_RBUTTON = 0x02;
    const int VK_MENU = 0x12; // Alt key

    const short SWP_NOSIZE = 1;
    const short SWP_NOZORDER = 0x0004;
    const int SWP_SHOWWINDOW = 0x0040;
    #endregion

    public static int Cooldown = 0;

    #region Config Management
    public static string GetConfigPath() {
        try {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        } catch {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NotificationsAtTop");
            if (!Directory.Exists(appData)) {
                Directory.CreateDirectory(appData);
            }
            return Path.Combine(appData, "config.ini");
        }
    }

    public static bool LoadPosition(NotificationPosition position, out int customX, out int customY) {
        customX = 0;
        customY = 0;
        string path = GetConfigPath();
        if (!File.Exists(path)) return false;

        try {
            string[] lines = File.ReadAllLines(path);
            string currentSection = "";
            string targetSection = "[" + position.ToString() + "]";
            bool isCustom = false;
            int x = 0, y = 0;

            foreach (string rawLine in lines) {
                string line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) {
                    currentSection = line;
                    continue;
                }
                if (string.Equals(currentSection, targetSection, StringComparison.OrdinalIgnoreCase)) {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0) {
                        string key = line.Substring(0, eqIndex).Trim();
                        string val = line.Substring(eqIndex + 1).Trim();
                        if (string.Equals(key, "Custom", StringComparison.OrdinalIgnoreCase)) {
                            isCustom = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                        } else if (string.Equals(key, "X", StringComparison.OrdinalIgnoreCase)) {
                            int.TryParse(val, out x);
                        } else if (string.Equals(key, "Y", StringComparison.OrdinalIgnoreCase)) {
                            int.TryParse(val, out y);
                        }
                    }
                }
            }

            if (isCustom) {
                customX = x;
                customY = y;
                return true;
            }
        } catch {
            // Ignore file read exceptions
        }

        return false;
    }

    public static void SavePosition(NotificationPosition position, int customX, int customY) {
        try {
            string path = GetConfigPath();
            Dictionary<string, Dictionary<string, string>> ini = 
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(path)) {
                string[] lines = File.ReadAllLines(path);
                string currentSection = "";
                foreach (string rawLine in lines) {
                    string line = rawLine.Trim();
                    if (line.StartsWith("[") && line.EndsWith("]")) {
                        currentSection = line.Substring(1, line.Length - 2);
                        if (!ini.ContainsKey(currentSection)) {
                            ini[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (eq > 0 && !string.IsNullOrEmpty(currentSection)) {
                        string k = line.Substring(0, eq).Trim();
                        string v = line.Substring(eq + 1).Trim();
                        ini[currentSection][k] = v;
                    }
                }
            }

            string sec = position.ToString();
            if (!ini.ContainsKey(sec)) {
                ini[sec] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            ini[sec]["Custom"] = "true";
            ini[sec]["X"] = customX.ToString();
            ini[sec]["Y"] = customY.ToString();

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Dictionary<string, string>> secPair in ini) {
                sb.AppendLine("[" + secPair.Key + "]");
                foreach (KeyValuePair<string, string> kvp in secPair.Value) {
                    sb.AppendLine(kvp.Key + "=" + kvp.Value);
                }
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        } catch {
            // Suppress IO errors
        }
    }

    public static void ResetPosition(NotificationPosition position) {
        try {
            string path = GetConfigPath();
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            string currentSection = "";
            string targetSection = "[" + position.ToString() + "]";

            foreach (string rawLine in lines) {
                string line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) {
                    currentSection = line;
                    if (string.Equals(currentSection, targetSection, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }
                if (!string.Equals(currentSection, targetSection, StringComparison.OrdinalIgnoreCase)) {
                    sb.AppendLine(rawLine);
                }
            }
            File.WriteAllText(path, sb.ToString());
        } catch {
            // Suppress IO errors
        }
    }
    #endregion

    /// <summary>
    /// Reduces process working set to reclaim physical RAM to OS.
    /// </summary>
    public static void TrimWorkingSet() {
        try {
            SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
        } catch {
            // Suppress memory trimming errors
        }
    }

    /// <summary>
    /// Retrieves monitor bounding rectangle for a window handle without allocating WinForms objects.
    /// </summary>
    private static RECT GetMonitorRect(IntPtr hwnd) {
        IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero) {
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (GetMonitorInfo(hMonitor, ref mi)) {
                return mi.rcMonitor;
            }
        }
        return new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
    }

    private static bool IsPointInRect(POINT pt, RECT rect) {
        return pt.X >= rect.Left && pt.X <= rect.Right && pt.Y >= rect.Top && pt.Y <= rect.Bottom;
    }

    /// <summary>
    /// Repositions a target notification window. Uses saved custom position if available,
    /// or calculates target based on screen preset.
    /// </summary>
    private static bool MoveNotification(IntPtr hwnd, NotificationPosition position, bool hasCustom, int customX, int customY, int yOffset, int xPadding) {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) {
            return false;
        }

        RECT rect = new RECT();
        if (!GetWindowRect(hwnd, ref rect)) {
            return false;
        }

        int width = rect.Width;
        int height = rect.Height;

        // Reject invalid, hidden or offscreen sizes
        if (width < 30 || width > 4000 || height < 20) {
            return false;
        }

        RECT screen = GetMonitorRect(hwnd);
        int targetX;
        int targetY;

        if (hasCustom) {
            targetX = screen.Left + customX;
            targetY = screen.Top + customY;
        } else {
            switch (position) {
                case NotificationPosition.TopLeft:
                    targetX = screen.Left + xPadding;
                    break;
                case NotificationPosition.TopCenter:
                    targetX = screen.Left + (screen.Width - width) / 2;
                    break;
                case NotificationPosition.TopRight:
                default:
                    targetX = screen.Right - width - xPadding;
                    break;
            }
            targetY = screen.Top + yOffset;
        }

        SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
        return true;
    }

    public static void Main(string[] args) {
        // Enable DPI awareness to prevent incorrect scaling on high-res displays
        try {
            SetProcessDPIAware();
        } catch {
            // Suppress if unsupported on legacy environments
        }

        string appName = System.AppDomain.CurrentDomain.FriendlyName;
        NotificationPosition position = NotificationPosition.TopRight; // Default fallback

        if (appName.IndexOf("topleft", StringComparison.OrdinalIgnoreCase) >= 0 ||
            (args.Length > 0 && string.Equals(args[0], "left", StringComparison.OrdinalIgnoreCase))) {
            position = NotificationPosition.TopLeft;
        } else if (appName.IndexOf("topcenter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (args.Length > 0 && (string.Equals(args[0], "center", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(args[0], "topcenter", StringComparison.OrdinalIgnoreCase)))) {
            position = NotificationPosition.TopCenter;
        } else if (appName.IndexOf("topright", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (args.Length > 0 && string.Equals(args[0], "right", StringComparison.OrdinalIgnoreCase))) {
            position = NotificationPosition.TopRight;
        }

        // Prevent multiple instances from conflicting with each other
        string mutexName = "NotificationsAtTop_" + position.ToString();
        bool createdNew;

        using (Mutex mutex = new Mutex(true, mutexName, out createdNew)) {
            if (!createdNew) {
                return; // Another instance is already active
            }

            int customOffsetX = 0;
            int customOffsetY = 0;
            bool hasCustomPos = LoadPosition(position, out customOffsetX, out customOffsetY);

            // Reclaim initialization memory footprint to OS
            TrimWorkingSet();

            // Drag state variables
            bool isDragging = false;
            IntPtr draggingHwnd = IntPtr.Zero;
            POINT dragStartCursor = new POINT();
            POINT dragStartWindow = new POINT();
            bool wasActiveNotification = false;

            while (true) {
                bool hasActiveNotification = false;

                // Check modifier key and mouse states for Drag & Drop / Reset
                bool isAltDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                bool isLButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
                bool isRButtonDown = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

                IntPtr activeHwnd = IntPtr.Zero;

                // 1. Locate MS Teams Notifications
                IntPtr teamsHwnd = IntPtr.Zero;
                try {
                    teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
                    if (teamsHwnd != IntPtr.Zero && IsWindow(teamsHwnd)) {
                        IntPtr chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");

                        if (chromeHwnd != IntPtr.Zero) {
                            activeHwnd = teamsHwnd;
                            hasActiveNotification = true;
                            Cooldown = 1;
                        } else if (Cooldown > 0) {
                            hasActiveNotification = true;
                            if (Cooldown >= 30) {
                                SetWindowPos(teamsHwnd, IntPtr.Zero, 0, -9999, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                                Cooldown = 0;
                            } else {
                                Cooldown++;
                            }
                        }
                    } else {
                        Cooldown = 0;
                    }
                } catch {
                    // Suppress external exceptions
                }

                // 2. Locate Windows System Notifications (CoreWindow)
                IntPtr sysHwnd = IntPtr.Zero;
                try {
                    sysHwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");
                    if (sysHwnd != IntPtr.Zero && IsWindow(sysHwnd)) {
                        activeHwnd = sysHwnd;
                        hasActiveNotification = true;
                    }
                } catch {
                    // Suppress external exceptions
                }

                // 3. Handle Drag & Drop / Reset Interactions
                if (hasActiveNotification && activeHwnd != IntPtr.Zero) {
                    POINT curPt;
                    GetCursorPos(out curPt);

                    RECT winRect = new RECT();
                    GetWindowRect(activeHwnd, ref winRect);

                    // Check Alt + Right Click: Reset custom position to default preset
                    if (isAltDown && isRButtonDown && IsPointInRect(curPt, winRect)) {
                        ResetPosition(position);
                        hasCustomPos = false;
                        isDragging = false;
                        Thread.Sleep(200); // Debounce
                    }
                    // Check Alt + Left Click: Initiate Drag
                    else if (!isDragging && isAltDown && isLButtonDown && IsPointInRect(curPt, winRect)) {
                        isDragging = true;
                        draggingHwnd = activeHwnd;
                        dragStartCursor = curPt;
                        dragStartWindow = new POINT { X = winRect.Left, Y = winRect.Top };
                    }
                }

                // Execute Drag tracking if in dragging state
                if (isDragging) {
                    if (isLButtonDown && IsWindow(draggingHwnd)) {
                        POINT curPt;
                        GetCursorPos(out curPt);
                        int newX = dragStartWindow.X + (curPt.X - dragStartCursor.X);
                        int newY = dragStartWindow.Y + (curPt.Y - dragStartCursor.Y);
                        SetWindowPos(draggingHwnd, IntPtr.Zero, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    } else {
                        // User released mouse button: save new position
                        isDragging = false;
                        if (IsWindow(draggingHwnd)) {
                            RECT finalRect = new RECT();
                            if (GetWindowRect(draggingHwnd, ref finalRect)) {
                                RECT screen = GetMonitorRect(draggingHwnd);
                                customOffsetX = finalRect.Left - screen.Left;
                                customOffsetY = finalRect.Top - screen.Top;
                                hasCustomPos = true;
                                SavePosition(position, customOffsetX, customOffsetY);
                            }
                        }
                    }
                } else {
                    // Normal positioning when not actively dragging
                    if (teamsHwnd != IntPtr.Zero && Cooldown == 1) {
                        MoveNotification(teamsHwnd, position, hasCustomPos, customOffsetX, customOffsetY, 15, 15);
                    }
                    if (sysHwnd != IntPtr.Zero) {
                        MoveNotification(sysHwnd, position, hasCustomPos, customOffsetX, customOffsetY, -37, 0);
                    }
                }

                // Trim working set when active notification dismisses to keep memory footprint minimal
                if (wasActiveNotification && !hasActiveNotification && !isDragging) {
                    TrimWorkingSet();
                }
                wasActiveNotification = hasActiveNotification;

                // Adaptive polling: 10ms during drag, 15ms during active display, 250ms when idle
                Thread.Sleep(isDragging ? 10 : (hasActiveNotification ? 15 : 250));
            }
        }
    }
}
