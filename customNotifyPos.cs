​using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;

public enum ResizeEdge {
    None,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class Program {

    #region Native Win32 Structures
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

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX {
        public uint cbSize;
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }
    #endregion

    #region Win32 API Imports
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("user32.dll")]
    static extern bool SetSystemCursor(IntPtr hcur, uint id);

    [DllImport("user32.dll")]
    static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

    [DllImport("user32.dll")]
    static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

    [DllImport("user32.dll")]
    static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern int SetBkMode(IntPtr hdc, int iBkMode);

    [DllImport("gdi32.dll")]
    static extern uint SetTextColor(IntPtr hdc, uint crColor);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint format);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFont(int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight, uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet, uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);

    [DllImport("gdi32.dll")]
    static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    const uint MONITOR_DEFAULTTONEAREST = 2;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    const int VK_LBUTTON = 0x01;
    const int VK_RBUTTON = 0x02;
    const int VK_MENU = 0x12; // Alt key

    const short SWP_NOSIZE = 1;
    const short SWP_NOZORDER = 0x0004;
    const int SWP_SHOWWINDOW = 0x0040;

    const int GWL_STYLE = -16;
    const int WS_MAXIMIZEBOX = 0x00010000;

    const int IDC_ARROW = 32512;
    const int IDC_SIZENWSE = 32642;
    const int IDC_SIZENESW = 32643;
    const int IDC_SIZEWE = 32644;
    const int IDC_SIZENS = 32645;

    const uint OCR_NORMAL = 32512;
    const uint SPI_SETCURSORS = 0x0057;

    const int RESIZE_BORDER = 8;
    const string CONFIG_SECTION = "CustomNotifyPos";
    #endregion

    public static int Cooldown = 0;
    private static IntPtr testNotifyHwnd = IntPtr.Zero;
    private static WndProcDelegate testWndProcHolder; // Prevents GC collection of delegate
    private static IntPtr cachedTeamsHwnd = IntPtr.Zero;

    #region Pure Win32 Startup Test Notification (Zero WinForms Overhead)
    private static IntPtr TestWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        const uint WM_PAINT = 0x000F;
        const uint WM_TIMER = 0x0113;
        const uint WM_DESTROY = 0x0002;
        const uint WM_LBUTTONDOWN = 0x0201;
        const int TRANSPARENT = 1;
        const uint DT_LEFT = 0x0000;
        const uint DT_TOP = 0x0000;
        const uint DT_WORDBREAK = 0x0010;
        const uint DT_NOPREFIX = 0x0800;
        const uint DT_CENTER = 0x0001;
        const uint DT_VCENTER = 0x0004;
        const uint DT_SINGLELINE = 0x0020;

        if (msg == WM_PAINT) {
            PAINTSTRUCT ps;
            IntPtr hdc = BeginPaint(hWnd, out ps);

            RECT clientRect = new RECT();
            GetClientRect(hWnd, ref clientRect);

            // Dark Windows 11 Fluent style background and border
            IntPtr hBrush = CreateSolidBrush(0x00202020); // RGB(32, 32, 32)
            IntPtr hPen = CreatePen(0, 1, 0x00464646);     // RGB(70, 70, 70)
            IntPtr oldBrush = SelectObject(hdc, hBrush);
            IntPtr oldPen = SelectObject(hdc, hPen);

            Rectangle(hdc, 0, 0, clientRect.Width, clientRect.Height);

            SetBkMode(hdc, TRANSPARENT);

            // Title: Bold Segoe UI
            IntPtr hFontTitle = CreateFont(-15, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            IntPtr oldFont = SelectObject(hdc, hFontTitle);
            SetTextColor(hdc, 0x00FFFFFF);

            RECT titleRect = new RECT { Left = 16, Top = 12, Right = clientRect.Width - 36, Bottom = 34 };
            DrawText(hdc, "Notification Preview", -1, ref titleRect, DT_LEFT | DT_TOP | DT_NOPREFIX);

            // Close button [X] at top-right
            IntPtr hFontClose = CreateFont(-14, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            SelectObject(hdc, hFontClose);
            SetTextColor(hdc, 0x00A0A0A0);
            RECT closeRect = new RECT { Left = clientRect.Width - 32, Top = 8, Right = clientRect.Width - 8, Bottom = 32 };
            DrawText(hdc, "X", -1, ref closeRect, DT_CENTER | DT_VCENTER | DT_SINGLELINE);

            // Instruction lines: Regular Segoe UI
            IntPtr hFontMsg = CreateFont(-12, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            SelectObject(hdc, hFontMsg);
            SetTextColor(hdc, 0x00D0D0D0);

            RECT msgRect = new RECT { Left = 16, Top = 38, Right = clientRect.Width - 16, Bottom = clientRect.Height - 8 };
            DrawText(hdc, "Left Click & Drag anywhere to move\nDrag borders to resize | Alt+Right Click to reset\nClick [X] when finished", -1, ref msgRect, DT_LEFT | DT_TOP | DT_WORDBREAK | DT_NOPREFIX);

            // Clean up GDI allocations
            SelectObject(hdc, oldFont);
            SelectObject(hdc, oldPen);
            SelectObject(hdc, oldBrush);
            DeleteObject(hFontMsg);
            DeleteObject(hFontClose);
            DeleteObject(hFontTitle);
            DeleteObject(hPen);
            DeleteObject(hBrush);

            EndPaint(hWnd, ref ps);
            return IntPtr.Zero;
        } else if (msg == WM_LBUTTONDOWN) {
            RECT clientRect = new RECT();
            GetClientRect(hWnd, ref clientRect);
            int mouseX = (int)(lParam.ToInt64() & 0xFFFF);
            int mouseY = (int)((lParam.ToInt64() >> 16) & 0xFFFF);
            if (mouseX >= clientRect.Width - 36 && mouseY <= 36) {
                KillTimer(hWnd, (IntPtr)1);
                DestroyWindow(hWnd);
                return IntPtr.Zero;
            }
        } else if (msg == WM_TIMER) {
            KillTimer(hWnd, (IntPtr)1);
            DestroyWindow(hWnd);
            return IntPtr.Zero;
        } else if (msg == WM_DESTROY) {
            testNotifyHwnd = IntPtr.Zero;
            PostQuitMessage(0);
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static void LaunchStartupTestNotification(int customW, int customH, bool hasCustomSize, bool hasCustomPos, int customX, int customY) {
        Thread t = new Thread(delegate() {
            try {
                testWndProcHolder = new WndProcDelegate(TestWndProc);

                WNDCLASSEX wc = new WNDCLASSEX();
                wc.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX));
                wc.style = 3; // CS_HREDRAW | CS_VREDRAW
                wc.lpfnWndProc = testWndProcHolder;
                wc.hInstance = GetModuleHandle(null);
                wc.hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW);
                wc.lpszClassName = "CustomNotifyPos_Toast";

                RegisterClassEx(ref wc);

                int w = (hasCustomSize && customW > 50) ? customW : 380;
                int h = (hasCustomSize && customH > 30) ? customH : 110;

                RECT screen = GetMonitorRect(IntPtr.Zero);
                int initX = hasCustomPos ? (screen.Left + customX) : (screen.Left + (screen.Width - w) / 2);
                int initY = hasCustomPos ? (screen.Top + customY) : (screen.Top + 15);
                if (initY < screen.Top) initY = screen.Top;

                const int WS_EX_TOPMOST = 0x00000008;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const uint WS_POPUP = 0x80000000;

                IntPtr hwnd = CreateWindowEx(
                    WS_EX_TOPMOST | WS_EX_TOOLWINDOW,
                    "CustomNotifyPos_Toast",
                    "TestNotify",
                    WS_POPUP,
                    initX, initY, w, h,
                    IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

                if (hwnd != IntPtr.Zero) {
                    testNotifyHwnd = hwnd;
                    SetTimer(hwnd, (IntPtr)1, 20000, IntPtr.Zero); // 20s auto-dismiss
                    ShowWindow(hwnd, 5); // SW_SHOW
                    UpdateWindow(hwnd);

                    MSG msg;
                    while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0) {
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
            } catch {
                testNotifyHwnd = IntPtr.Zero;
            }
        });
        t.IsBackground = true;
        t.Start();
    }
    #endregion

    #region Cursor Management
    private static ResizeEdge lastAppliedCursorEdge = ResizeEdge.None;

    private static void UpdateCursorForEdge(ResizeEdge edge) {
        if (edge == lastAppliedCursorEdge) return;
        lastAppliedCursorEdge = edge;

        int idc = 0;
        switch (edge) {
            case ResizeEdge.Left:
            case ResizeEdge.Right:
                idc = IDC_SIZEWE;
                break;
            case ResizeEdge.Top:
            case ResizeEdge.Bottom:
                idc = IDC_SIZENS;
                break;
            case ResizeEdge.TopLeft:
            case ResizeEdge.BottomRight:
                idc = IDC_SIZENWSE;
                break;
            case ResizeEdge.TopRight:
            case ResizeEdge.BottomLeft:
                idc = IDC_SIZENESW;
                break;
            default:
                idc = 0;
                break;
        }

        if (idc != 0) {
            try {
                IntPtr hCur = LoadCursor(IntPtr.Zero, idc);
                if (hCur != IntPtr.Zero) {
                    IntPtr hCopy = CopyIcon(hCur);
                    if (hCopy != IntPtr.Zero) {
                        SetSystemCursor(hCopy, OCR_NORMAL);
                    }
                }
            } catch {
                // Suppress cursor exceptions
            }
        } else {
            RestoreSystemCursor();
        }
    }

    private static void RestoreSystemCursor() {
        if (lastAppliedCursorEdge != ResizeEdge.None) {
            lastAppliedCursorEdge = ResizeEdge.None;
            try {
                SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, 0);
            } catch {
                // Suppress cursor reset exceptions
            }
        }
    }
    #endregion

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

    public static bool LoadConfig(out int customX, out int customY, out bool hasCustomSize, out int customW, out int customH) {
        customX = 0;
        customY = 0;
        hasCustomSize = false;
        customW = 0;
        customH = 0;
        string path = GetConfigPath();
        if (!File.Exists(path)) return false;

        try {
            string[] lines = File.ReadAllLines(path);
            string currentSection = "";
            bool isCustom = false;
            int x = 0, y = 0, w = 0, h = 0;
            bool isCustomSize = false;

            // Priority loading from [CustomNotifyPos], with legacy fallback to [TopCenter]/[TopRight]/[TopLeft]
            string targetSection = "[" + CONFIG_SECTION + "]";
            bool foundSection = false;

            foreach (string rawLine in lines) {
                string line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) {
                    currentSection = line;
                    continue;
                }
                if (string.Equals(currentSection, targetSection, StringComparison.OrdinalIgnoreCase)) {
                    foundSection = true;
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
                        } else if (string.Equals(key, "CustomSize", StringComparison.OrdinalIgnoreCase)) {
                            isCustomSize = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                        } else if (string.Equals(key, "Width", StringComparison.OrdinalIgnoreCase)) {
                            int.TryParse(val, out w);
                        } else if (string.Equals(key, "Height", StringComparison.OrdinalIgnoreCase)) {
                            int.TryParse(val, out h);
                        }
                    }
                }
            }

            // Fallback to legacy sections if not yet migrated
            if (!foundSection) {
                currentSection = "";
                foreach (string rawLine in lines) {
                    string line = rawLine.Trim();
                    if (line.StartsWith("[") && line.EndsWith("]")) {
                        currentSection = line;
                        continue;
                    }
                    if (string.Equals(currentSection, "[TopCenter]", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentSection, "[TopRight]", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentSection, "[TopLeft]", StringComparison.OrdinalIgnoreCase)) {
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
                            } else if (string.Equals(key, "CustomSize", StringComparison.OrdinalIgnoreCase)) {
                                isCustomSize = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                            } else if (string.Equals(key, "Width", StringComparison.OrdinalIgnoreCase)) {
                                int.TryParse(val, out w);
                            } else if (string.Equals(key, "Height", StringComparison.OrdinalIgnoreCase)) {
                                int.TryParse(val, out h);
                            }
                        }
                    }
                }
            }

            if (isCustomSize && w > 50 && h > 30) {
                hasCustomSize = true;
                customW = w;
                customH = h;
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

    public static void SaveConfig(bool hasPos, int customX, int customY, bool hasSize, int customW, int customH) {
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

            if (!ini.ContainsKey(CONFIG_SECTION)) {
                ini[CONFIG_SECTION] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            if (hasPos) {
                ini[CONFIG_SECTION]["Custom"] = "true";
                ini[CONFIG_SECTION]["X"] = customX.ToString();
                ini[CONFIG_SECTION]["Y"] = customY.ToString();
            }
            if (hasSize) {
                ini[CONFIG_SECTION]["CustomSize"] = "true";
                ini[CONFIG_SECTION]["Width"] = customW.ToString();
                ini[CONFIG_SECTION]["Height"] = customH.ToString();
            }

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

    public static void ResetConfig() {
        try {
            string path = GetConfigPath();
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            string currentSection = "";

            foreach (string rawLine in lines) {
                string line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) {
                    currentSection = line;
                    if (string.Equals(currentSection, "[" + CONFIG_SECTION + "]", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentSection, "[TopCenter]", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentSection, "[TopRight]", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentSection, "[TopLeft]", StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }
                if (!string.Equals(currentSection, "[" + CONFIG_SECTION + "]", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(currentSection, "[TopCenter]", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(currentSection, "[TopRight]", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(currentSection, "[TopLeft]", StringComparison.OrdinalIgnoreCase)) {
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
    /// Actively invokes garbage collection and empties physical working set back to Windows.
    /// Keeps memory footprint at ~1.5 - 3 MB.
    /// </summary>
    public static void TrimWorkingSet() {
        try {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
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

    private static ResizeEdge GetResizeEdge(POINT pt, RECT rect, int borderSize) {
        bool inHorizontal = pt.X >= rect.Left - 2 && pt.X <= rect.Right + 2;
        bool inVertical = pt.Y >= rect.Top - 2 && pt.Y <= rect.Bottom + 2;
        if (!inHorizontal || !inVertical) return ResizeEdge.None;

        bool onLeft = pt.X >= rect.Left - 2 && pt.X <= rect.Left + borderSize;
        bool onRight = pt.X >= rect.Right - borderSize && pt.X <= rect.Right + 2;
        bool onTop = pt.Y >= rect.Top - 2 && pt.Y <= rect.Top + borderSize;
        bool onBottom = pt.Y >= rect.Bottom - borderSize && pt.Y <= rect.Bottom + 2;

        if (onTop && onLeft) return ResizeEdge.TopLeft;
        if (onTop && onRight) return ResizeEdge.TopRight;
        if (onBottom && onLeft) return ResizeEdge.BottomLeft;
        if (onBottom && onRight) return ResizeEdge.BottomRight;

        if (onLeft) return ResizeEdge.Left;
        if (onRight) return ResizeEdge.Right;
        if (onTop) return ResizeEdge.Top;
        if (onBottom) return ResizeEdge.Bottom;

        return ResizeEdge.None;
    }

    /// <summary>
    /// Synchronously resizes child render hosts (Chromium render widget for Teams Classic and Teams WebView2).
    /// </summary>
    private static void ResizeChildContent(IntPtr hwnd, int width, int height) {
        try {
            // Synchronously resize child render hosts across all nested levels (Teams Classic and Teams WebView2)
            EnumChildWindows(hwnd, delegate(IntPtr childHwnd, IntPtr lParam) {
                StringBuilder sb = new StringBuilder(32);
                GetClassName(childHwnd, sb, 32);
                string cls = sb.ToString();
                if (cls.StartsWith("Chrome_") || cls.StartsWith("Intermediate D3D")) {
                    SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
                }
                return true;
            }, IntPtr.Zero);
        } catch {
            // Suppress child resize exceptions
        }
    }

    /// <summary>
    /// Checks process executable name via unmanaged Win32 API without creating managed Process objects.
    /// </summary>
    private static bool IsTeamsProcess(uint pid) {
        if (pid == 0) return false;
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero) return false;

        try {
            StringBuilder sb = new StringBuilder(260);
            uint size = (uint)sb.Capacity;
            if (QueryFullProcessImageName(hProcess, 0, sb, ref size)) {
                string exePath = sb.ToString();
                return exePath.EndsWith("ms-teams.exe", StringComparison.OrdinalIgnoreCase) ||
                       exePath.EndsWith("teams.exe", StringComparison.OrdinalIgnoreCase);
            }
        } catch { }
        finally {
            CloseHandle(hProcess);
        }
        return false;
    }

    /// <summary>
    /// Discovers MS Teams notification or call windows for both Classic (Electron) and New Teams (WebView2 on Windows 11).
    /// </summary>
    private static IntPtr FindTeamsNotificationWindow() {
        try {
            // 0. Cache hit: If previously discovered Teams notification window is still open & visible, reuse it
            if (cachedTeamsHwnd != IntPtr.Zero && IsWindow(cachedTeamsHwnd) && IsWindowVisible(cachedTeamsHwnd)) {
                return cachedTeamsHwnd;
            }
            cachedTeamsHwnd = IntPtr.Zero;

            // 1. Fast Path: Classic Teams notification with static title
            IntPtr classicHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
            if (classicHwnd != IntPtr.Zero && IsWindow(classicHwnd) && IsWindowVisible(classicHwnd)) {
                cachedTeamsHwnd = classicHwnd;
                return classicHwnd;
            }

            // 2. Dynamic Path: New Teams (WebView2) floating toast/call banners
            IntPtr targetHwnd = IntPtr.Zero;
            StringBuilder sbClass = new StringBuilder(32);

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam) {
                if (!IsWindowVisible(hWnd)) return true;

                sbClass.Length = 0;
                GetClassName(hWnd, sbClass, 32);
                string cls = sbClass.ToString();
                if (!string.Equals(cls, "Chrome_WidgetWin_1", StringComparison.Ordinal) &&
                    !string.Equals(cls, "TeamsWebView", StringComparison.Ordinal)) {
                    return true;
                }

                RECT r = new RECT();
                if (!GetWindowRect(hWnd, ref r)) return true;
                int w = r.Width;
                int h = r.Height;

                // Typical floating toast / call banner dimensions
                if (w < 150 || w > 650 || h < 50 || h > 450) return true;

                // Ensure it's not a regular window with a maximize button
                int style = GetWindowLong(hWnd, GWL_STYLE);
                if ((style & WS_MAXIMIZEBOX) != 0) return true;

                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (IsTeamsProcess(pid)) {
                    targetHwnd = hWnd;
                    return false; // Found, stop enumeration
                }

                return true;
            }, IntPtr.Zero);

            cachedTeamsHwnd = targetHwnd;
            return targetHwnd;
        } catch {
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Repositions and resizes a target notification window. Uses saved custom position/size if available,
    /// or centers at the top of the monitor by default.
    /// </summary>
    private static bool MoveNotification(IntPtr hwnd, bool hasCustom, int customX, int customY, bool hasCustomSize, int customW, int customH, int yOffset) {
        try {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) {
                return false;
            }

            RECT rect = new RECT();
            if (!GetWindowRect(hwnd, ref rect)) {
                return false;
            }

            int width = (hasCustomSize && customW > 50) ? customW : rect.Width;
            int height = (hasCustomSize && customH > 30) ? customH : rect.Height;

            // Reject invalid, hidden or offscreen sizes
            if (rect.Width < 30 || rect.Width > 4000 || rect.Height < 20) {
                return false;
            }

            RECT screen = GetMonitorRect(hwnd);
            int targetX;
            int targetY;

            if (hasCustom) {
                targetX = screen.Left + customX;
                targetY = screen.Top + customY;
            } else {
                // Default position: Centered horizontally at the top of the screen
                targetX = screen.Left + (screen.Width - width) / 2;
                targetY = screen.Top + yOffset;
            }

            if (targetY < screen.Top) targetY = screen.Top;

            // Avoid redundant SetWindowPos calls if already at the target position/size
            if (rect.Left == targetX && rect.Top == targetY && (!hasCustomSize || (rect.Width == width && rect.Height == height))) {
                return true;
            }

            if (hasCustomSize) {
                SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
                ResizeChildContent(hwnd, width, height);
            } else {
                SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
            }
            return true;
        } catch {
            return false;
        }
    }

    public static void Main(string[] args) {
        // Enable Per-Monitor V2 DPI awareness for pixel-perfect coordinates across all displays
        try {
            if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)) {
                SetProcessDPIAware();
            }
        } catch {
            try { SetProcessDPIAware(); } catch { }
        }

        // Prevent multiple instances from conflicting with each other
        string mutexName = "customNotifyPos_Instance";
        bool createdNew;

        using (Mutex mutex = new Mutex(true, mutexName, out createdNew)) {
            if (!createdNew) {
                return; // Another instance is already active
            }

            int customOffsetX = 0;
            int customOffsetY = 0;
            bool hasCustomSize = false;
            int customWidth = 0;
            int customHeight = 0;
            bool hasCustomPos = LoadConfig(out customOffsetX, out customOffsetY, out hasCustomSize, out customWidth, out customHeight);

            // Reclaim initialization memory footprint to OS
            TrimWorkingSet();

            // Launch startup test notification so user can position/resize it immediately
            LaunchStartupTestNotification(customWidth, customHeight, hasCustomSize, hasCustomPos, customOffsetX, customOffsetY);

            // Drag state variables
            bool isDragging = false;
            IntPtr draggingHwnd = IntPtr.Zero;
            POINT dragStartCursor = new POINT();
            POINT dragStartWindow = new POINT();

            // Resize state variables
            bool isResizing = false;
            IntPtr resizingHwnd = IntPtr.Zero;
            ResizeEdge activeResizeEdge = ResizeEdge.None;
            RECT resizeStartRect = new RECT();

            bool wasLButtonDown = false;
            bool wasActiveNotification = false;

            while (true) {
                // Check modifier key and mouse states for Drag & Drop / Resize / Reset
                bool isAltDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                bool isLButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
                bool isRButtonDown = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

                // 1. Locate candidate notification windows
                bool hasTest = (testNotifyHwnd != IntPtr.Zero && IsWindow(testNotifyHwnd));

                IntPtr teamsHwnd = IntPtr.Zero;
                try {
                    teamsHwnd = FindTeamsNotificationWindow();
                    if (teamsHwnd != IntPtr.Zero && IsWindow(teamsHwnd)) {
                        Cooldown = 1;
                    } else if (Cooldown > 0) {
                        Cooldown = 0;
                    }
                } catch {
                    // Suppress external exceptions
                }

                IntPtr sysHwnd = IntPtr.Zero;
                try {
                    sysHwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");
                } catch {
                    // Suppress external exceptions
                }

                bool hasTeams = (teamsHwnd != IntPtr.Zero && IsWindow(teamsHwnd));
                bool hasSys = (sysHwnd != IntPtr.Zero && IsWindow(sysHwnd));
                bool hasActiveNotification = hasTest || hasTeams || hasSys;

                POINT curPt;
                GetCursorPos(out curPt);

                // 2. Identify target window directly under cursor (Hover-based selection)
                IntPtr hoverHwnd = IntPtr.Zero;
                RECT hoverRect = new RECT();

                if (hasTest) {
                    RECT r = new RECT();
                    if (GetWindowRect(testNotifyHwnd, ref r) && IsPointInRect(curPt, r)) {
                        hoverHwnd = testNotifyHwnd;
                        hoverRect = r;
                    }
                }
                if (hoverHwnd == IntPtr.Zero && hasTeams) {
                    RECT r = new RECT();
                    if (GetWindowRect(teamsHwnd, ref r) && IsPointInRect(curPt, r)) {
                        hoverHwnd = teamsHwnd;
                        hoverRect = r;
                    }
                }
                if (hoverHwnd == IntPtr.Zero && hasSys) {
                    RECT r = new RECT();
                    if (GetWindowRect(sysHwnd, ref r) && IsPointInRect(curPt, r)) {
                        hoverHwnd = sysHwnd;
                        hoverRect = r;
                    }
                }

                // 3. Reset auto-dismiss timer on TestNotify if cursor is hovering or interacting with it
                if (hoverHwnd == testNotifyHwnd && hasTest) {
                    SetTimer(testNotifyHwnd, (IntPtr)1, 20000, IntPtr.Zero);
                }

                // 4. Handle initial user interactions (Drag / Resize / Reset)
                if (!isDragging && !isResizing) {
                    if (hoverHwnd != IntPtr.Zero) {
                        // Check Alt + Right Click: Reset custom position and size to default Top Center
                        if (isAltDown && isRButtonDown) {
                            ResetConfig();
                            hasCustomPos = false;
                            hasCustomSize = false;
                            customWidth = 0;
                            customHeight = 0;
                            isDragging = false;
                            isResizing = false;
                            RestoreSystemCursor();
                            Thread.Sleep(200); // Debounce
                        } else {
                            ResizeEdge edge = GetResizeEdge(curPt, hoverRect, RESIZE_BORDER);
                            if (edge != ResizeEdge.None) {
                                UpdateCursorForEdge(edge);
                                if (isLButtonDown) {
                                    isResizing = true;
                                    resizingHwnd = hoverHwnd;
                                    activeResizeEdge = edge;
                                    dragStartCursor = curPt;
                                    resizeStartRect = hoverRect;
                                }
                            } else {
                                RestoreSystemCursor();
                                // TestNotify can be dragged with plain Left Click or Alt+Left Click.
                                // Real notifications (Teams, Windows) require Alt+Left Click.
                                bool allowDrag = (hoverHwnd == testNotifyHwnd) || isAltDown;
                                if (allowDrag && isLButtonDown) {
                                    // Check if clicked the [X] button on TestNotify (top-right 36x36 area)
                                    bool isCloseClick = (hoverHwnd == testNotifyHwnd) && 
                                                        (curPt.X >= hoverRect.Right - 36 && curPt.Y <= hoverRect.Top + 36);
                                    if (isCloseClick) {
                                        KillTimer(testNotifyHwnd, (IntPtr)1);
                                        DestroyWindow(testNotifyHwnd);
                                        testNotifyHwnd = IntPtr.Zero;
                                    } else {
                                        isDragging = true;
                                        draggingHwnd = hoverHwnd;
                                        dragStartCursor = curPt;
                                        dragStartWindow = new POINT { X = hoverRect.Left, Y = hoverRect.Top };
                                    }
                                }
                            }
                        }
                    } else {
                        RestoreSystemCursor();
                    }
                }

                // 5. Execute Resize tracking if in resizing state
                if (isResizing) {
                    if (isLButtonDown && IsWindow(resizingHwnd)) {
                        int deltaX = curPt.X - dragStartCursor.X;
                        int deltaY = curPt.Y - dragStartCursor.Y;

                        int newX = resizeStartRect.Left;
                        int newY = resizeStartRect.Top;
                        int newWidth = resizeStartRect.Width;
                        int newHeight = resizeStartRect.Height;

                        const int MIN_WIDTH = 250;
                        const int MIN_HEIGHT = 80;

                        switch (activeResizeEdge) {
                            case ResizeEdge.Right:
                                newWidth = Math.Max(MIN_WIDTH, resizeStartRect.Width + deltaX);
                                break;
                            case ResizeEdge.Bottom:
                                newHeight = Math.Max(MIN_HEIGHT, resizeStartRect.Height + deltaY);
                                break;
                            case ResizeEdge.BottomRight:
                                newWidth = Math.Max(MIN_WIDTH, resizeStartRect.Width + deltaX);
                                newHeight = Math.Max(MIN_HEIGHT, resizeStartRect.Height + deltaY);
                                break;
                            case ResizeEdge.Left:
                                newWidth = resizeStartRect.Width - deltaX;
                                if (newWidth < MIN_WIDTH) {
                                    deltaX = resizeStartRect.Width - MIN_WIDTH;
                                    newWidth = MIN_WIDTH;
                                }
                                newX = resizeStartRect.Left + deltaX;
                                break;
                            case ResizeEdge.Top:
                                newHeight = resizeStartRect.Height - deltaY;
                                if (newHeight < MIN_HEIGHT) {
                                    deltaY = resizeStartRect.Height - MIN_HEIGHT;
                                    newHeight = MIN_HEIGHT;
                                }
                                newY = resizeStartRect.Top + deltaY;
                                break;
                            case ResizeEdge.TopLeft:
                                newWidth = resizeStartRect.Width - deltaX;
                                if (newWidth < MIN_WIDTH) {
                                    deltaX = resizeStartRect.Width - MIN_WIDTH;
                                    newWidth = MIN_WIDTH;
                                }
                                newX = resizeStartRect.Left + deltaX;

                                newHeight = resizeStartRect.Height - deltaY;
                                if (newHeight < MIN_HEIGHT) {
                                    deltaY = resizeStartRect.Height - MIN_HEIGHT;
                                    newHeight = MIN_HEIGHT;
                                }
                                newY = resizeStartRect.Top + deltaY;
                                break;
                            case ResizeEdge.TopRight:
                                newWidth = Math.Max(MIN_WIDTH, resizeStartRect.Width + deltaX);
                                newHeight = resizeStartRect.Height - deltaY;
                                if (newHeight < MIN_HEIGHT) {
                                    deltaY = resizeStartRect.Height - MIN_HEIGHT;
                                    newHeight = MIN_HEIGHT;
                                }
                                newY = resizeStartRect.Top + deltaY;
                                break;
                            case ResizeEdge.BottomLeft:
                                newWidth = resizeStartRect.Width - deltaX;
                                if (newWidth < MIN_WIDTH) {
                                    deltaX = resizeStartRect.Width - MIN_WIDTH;
                                    newWidth = MIN_WIDTH;
                                }
                                newX = resizeStartRect.Left + deltaX;
                                newHeight = Math.Max(MIN_HEIGHT, resizeStartRect.Height + deltaY);
                                break;
                        }

                        SetWindowPos(resizingHwnd, IntPtr.Zero, newX, newY, newWidth, newHeight, SWP_NOZORDER | SWP_SHOWWINDOW);
                        ResizeChildContent(resizingHwnd, newWidth, newHeight);
                    } else {
                        // User released mouse button: save new dimensions
                        isResizing = false;
                        RestoreSystemCursor();
                        if (IsWindow(resizingHwnd)) {
                            RECT finalRect = new RECT();
                            if (GetWindowRect(resizingHwnd, ref finalRect)) {
                                RECT screen = GetMonitorRect(resizingHwnd);
                                customOffsetX = finalRect.Left - screen.Left;
                                customOffsetY = finalRect.Top - screen.Top;
                                if (customOffsetY < 0) customOffsetY = 0;
                                customWidth = finalRect.Width;
                                customHeight = finalRect.Height;
                                hasCustomPos = true;
                                hasCustomSize = true;
                                SaveConfig(true, customOffsetX, customOffsetY, true, customWidth, customHeight);
                            }
                        }
                        resizingHwnd = IntPtr.Zero;
                    }
                }
                // 6. Execute Move tracking if in dragging state
                else if (isDragging) {
                    if (isLButtonDown && IsWindow(draggingHwnd)) {
                        int newX = dragStartWindow.X + (curPt.X - dragStartCursor.X);
                        int newY = dragStartWindow.Y + (curPt.Y - dragStartCursor.Y);
                        if (hasCustomSize && customWidth > 0 && customHeight > 0) {
                            SetWindowPos(draggingHwnd, IntPtr.Zero, newX, newY, customWidth, customHeight, SWP_NOZORDER | SWP_SHOWWINDOW);
                        } else {
                            SetWindowPos(draggingHwnd, IntPtr.Zero, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                        }
                    } else {
                        // User released mouse button: save new position
                        isDragging = false;
                        if (IsWindow(draggingHwnd)) {
                            RECT finalRect = new RECT();
                            if (GetWindowRect(draggingHwnd, ref finalRect)) {
                                RECT screen = GetMonitorRect(draggingHwnd);
                                customOffsetX = finalRect.Left - screen.Left;
                                customOffsetY = finalRect.Top - screen.Top;
                                if (customOffsetY < 0) customOffsetY = 0;
                                hasCustomPos = true;
                                SaveConfig(true, customOffsetX, customOffsetY, hasCustomSize, customWidth, customHeight);
                            }
                        }
                        draggingHwnd = IntPtr.Zero;
                    }
                } else {
                    // Normal positioning when not actively dragging or resizing
                    if (hasTest && testNotifyHwnd != draggingHwnd && testNotifyHwnd != resizingHwnd) {
                        MoveNotification(testNotifyHwnd, hasCustomPos, customOffsetX, customOffsetY, hasCustomSize, customWidth, customHeight, 15);
                    }
                    if (hasTeams && Cooldown == 1 && teamsHwnd != draggingHwnd && teamsHwnd != resizingHwnd) {
                        MoveNotification(teamsHwnd, hasCustomPos, customOffsetX, customOffsetY, hasCustomSize, customWidth, customHeight, 15);
                    }
                    if (hasSys && sysHwnd != draggingHwnd && sysHwnd != resizingHwnd) {
                        MoveNotification(sysHwnd, hasCustomPos, customOffsetX, customOffsetY, hasCustomSize, customWidth, customHeight, -37);
                    }
                }

                // Trim working set and reclaim RAM when notification dismisses
                if (wasActiveNotification && !hasActiveNotification && !isDragging && !isResizing) {
                    RestoreSystemCursor();
                    TrimWorkingSet();
                }
                wasActiveNotification = hasActiveNotification;
                wasLButtonDown = isLButtonDown;

                // Adaptive polling: 10ms during drag/resize, 15ms during active display, 250ms when idle
                Thread.Sleep(isDragging || isResizing ? 10 : (hasActiveNotification ? 15 : 250));
            }
        }
    }
}
