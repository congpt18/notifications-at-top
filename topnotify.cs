using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;

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

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    const short SWP_NOSIZE = 1;
    const short SWP_NOZORDER = 0x0004;
    const int SWP_SHOWWINDOW = 0x0040;
    #endregion

    public static int Cooldown = 0;

    /// <summary>
    /// Repositions a target notification window to the top left, top center, or top right of its containing screen.
    /// Supports multi-monitor environments and validates bounding rectangle.
    /// </summary>
    private static bool MoveNotification(IntPtr hwnd, NotificationPosition position, int yOffset, int xPadding) {
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

        // Obtain the specific monitor containing this notification window (Multi-monitor support)
        Screen screen = Screen.FromHandle(hwnd);
        int targetX;

        switch (position) {
            case NotificationPosition.TopLeft:
                targetX = screen.Bounds.Left + xPadding;
                break;
            case NotificationPosition.TopCenter:
                targetX = screen.Bounds.Left + (screen.Bounds.Width - width) / 2;
                break;
            case NotificationPosition.TopRight:
            default:
                targetX = screen.Bounds.Right - width - xPadding;
                break;
        }

        int targetY = screen.Bounds.Top + yOffset;

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

            while (true) {
                bool hasActiveNotification = false;

                // 1. MS Teams Notifications
                try {
                    IntPtr teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
                    if (teamsHwnd != IntPtr.Zero && IsWindow(teamsHwnd)) {
                        IntPtr chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");

                        if (chromeHwnd != IntPtr.Zero) {
                            if (MoveNotification(teamsHwnd, position, 15, 15)) {
                                hasActiveNotification = true;
                                Cooldown = 1;
                            }
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

                // 2. Windows System Notifications (CoreWindow)
                try {
                    IntPtr hwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");
                    if (hwnd != IntPtr.Zero && IsWindow(hwnd)) {
                        // 37PX Y offset to make the spacing even (MyDockFinder friendly)
                        if (MoveNotification(hwnd, position, -37, 0)) {
                            hasActiveNotification = true;
                        }
                    }
                } catch {
                    // Suppress external exceptions
                }

                // Adaptive polling: 15ms during active display/animation, 100ms when idle
                Thread.Sleep(hasActiveNotification ? 15 : 100);
            }
        }
    }
}
