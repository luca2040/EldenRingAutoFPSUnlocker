using EldenRingAutoFPSUnlocker.utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using System.Text;

namespace EldenRingAutoFPSUnlocker
{
    internal class ProgramData
    {
        internal const string DIALOG_WINDOWS_TITLE = "Elden Ring Auto FPS Unlocker";
        internal const string DIALOG_EAC_RUNNING_WARNING = "EAC (Online mode) is running\n\nTHIS CAN POTENTIALLY CAUSE A PERMANENT BAN.\n\nTo start it safely close the game and start it by opening this mod.\nDo you still want to continue?";
        internal const string DIALOG_EAC_CONFIRM_RUNNING_WARNING = "Are you sure?";
        internal const string DIALOG_ONLINE_RUNNING_WARNING = "No steam appid file found.\nThis could mean the game has been started in online mode.\nPlease ensure the game is in offline mode (Or is using the Seamless Coop mod) before continuing to avoid the risk of getting banned.\nContinue?";
        internal const string DIALOG_CANT_RUN_AS_ADMIN = "Cannot restart the program as administrator.\nPlease re-open it manually as administrator.";
        internal const string DIALOG_CANT_FIND_PATH = "Cannot locate game path automatically.\nPlease select the game's executable file (eldenring.exe).";
        internal const string DIALOG_EXE_NOT_VALID = "Selected game file is not valid.";
        internal const string DIALOG_CANT_WRITE_STEAM_ID = "Cannot write steam appid file.";
        internal static string DIALOG_INT_NOT_VALID_CONFIG(string key) => $"Value {key} in configuration file is not a valid integer number.";
        internal static string DIALOG_CONFIG_VALUE_NOT_VALID_BOOLEAN(string key) => $"Value {key} in configuration file must be 1 or 0";
    }

    internal class EldenRingData
    {
        internal const string PROCESS_NAME = "eldenring";
        internal const string APPLICATION_NAME = "ELDEN RING";
        internal static string DEFAULT_GAMENAME = "eldenring";
        internal const string SEAMLESS_COOP_MOD_NAME = "ersc_launcher";
        internal const string PROCESS_DESCRIPTION = "elden";
        internal const string STEAM_APPID_FILENAME = "steam_appid.txt";
        internal const string CONFIG_FOLDER_NAME = "EldenRingAutoFPSUnlocker";
        internal const string CONFIG_FILE_NAME = "config.ini";
        internal const string DEFAULT_EMBEDDED_CONFIG_FILE_PATH = "settings.config.defaultConfig.ini";

        internal const int PROCESS_RUNNING_CHECK_RETRIES = 10; // Max number of retries if game is not responding
        internal const int PROCESS_RUNNING_CHECK_INTERVAL = 500; // Check interval in milliseconds

        internal const int GAME_RESTART_ATTEMPTS = 4;
        internal const int GAME_RESTART_DELAY = 6000;

        internal const int PROCESS_OPEN_RETRY_NUMBER = 8;
        internal const int PROCESS_FOUND_RETRY_DELAY = 500;

        /*
        Memory patterns from https://github.com/uberhalit/EldenRingFpsUnlockAndMore
        */

        /**
            <float>fFrameTick determines default frame rate limit in seconds.
            00007FF6AEA0EF5A | EB 4F                      | jmp eldenring.7FF6AEA0EFAB                                     |
            00007FF6AEA0EF5C | 8973 18                    | mov dword ptr ds:[rbx+18],esi                                  |
            00007FF6AEA0EF5F | C743 20 8988883C           | mov dword ptr ds:[rbx+20],3C888889                             | fFrameTick
            00007FF6AEA0EF66 | EB 43                      | jmp eldenring.7FF6AEA0EFAB                                     |
            00007FF6AEA0EF68 | 8973 18                    | mov dword ptr ds:[rbx+18],esi                                  |

            00007FF6AEA0EF5F (Version 1.2.0.0)
         */
        internal const string PATTERN_FRAMELOCK = "C7 43 1C 89 88 3C EB"; // first byte can can be 88/90 instead of 89 due to precision loss on floating point numbers
        internal const int PATTERN_FRAMELOCK_OFFSET = 3; // offset to byte array from found position
        internal const string PATTERN_FRAMELOCK_FUZZY = "89 73 ?? C7 ?? ?? ?? ?? ?? ?? EB ?? 89 73";
        internal const int PATTERN_FRAMELOCK_OFFSET_FUZZY = 6;

        /**
            HARDCODED limit to 60 Hz monitor refresh rate on every resolution change. FromSoft doesn't even bother with reading the user defined Hz from driver.
            This is not just lazy, but anti-consumer as they did aknowledge user-defined Hz override in Sekiro, but not anymore in ER.
            00007FF7A30CAB25 | EB 0E                      | jmp eldenring.7FF7A30CAB35                                     |
            00007FF7A30CAB27 | C745 EF 3C000000           | mov dword ptr ss:[rbp-11],3C                                   | forces monitor to 60 (0x3C) Hz
            00007FF7A30CAB2E | C745 F3 01000000           | mov dword ptr ss:[rbp-D],1                                     | 1 indicates a hertz change
            00007FF7A30CAB35 | 8B87 940E0000              | mov eax,dword ptr ds:[rdi+E94]                                 |
            00007FF7A30CAB3B | 44:8BB3 54010000           | mov r14d,dword ptr ds:[rbx+154]                                |
            
            00007FF7A30CAB27 (Version 1.2.0.0)
         */
        internal const string PATTERN_HERTZLOCK = "EB ?? C7 ?? ?? 3C 00 00 00 C7 ?? ?? 01 00 00 00";
        internal const int PATTERN_HERTZLOCK_OFFSET = 5;  // 2
        internal const int PATTERN_HERTZLOCK_OFFSET_INTEGER1 = 0; //3
        internal const int PATTERN_HERTZLOCK_OFFSET_INTEGER2 = 0; // 10
        internal const int PATCH_HERTZLOCK_INSTRUCTION_LENGTH = 14;
    }

    internal class DllAPI
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
            IntPtr hProcess,
            long lpBaseAddress,
            [In, Out] byte[] lpBuffer,
            ulong dwSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(
            IntPtr hProcess,
            long lpBaseAddress,
            [Out] byte[] lpBuffer,
            ulong dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(
            string deviceName, int modeNum, ref MonitorInfo.DEVMODE devMode);


        // Overlay window DLLs

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst,
            ref Size psize, IntPtr hdcSrc, ref Point pptSrc, int crKey, ref OverlayWindow.BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        internal static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // Function to load .ini files

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetPrivateProfileString(
            string section, string key, string defaultValue,
            StringBuilder retVal, int size, string filePath);

    }

    internal class MemoryOperations
    {
        internal static bool IsValidAddress(long address)
        {
            return address >= 0x10000 && address < 0x000F000000000000;
        }

        static internal bool WriteBytes(long lpBaseAddress, byte[] bytes)
        {
            return DllAPI.WriteProcessMemory(EldenProcess.gameAccessHwndStatic, lpBaseAddress, bytes, (ulong)bytes.Length, out _);
        }

        static internal IntPtr OpenPrcessAllAccess(bool bInheritHandle, uint dwProcessId)
        {
            return DllAPI.OpenProcess(0x001F0FFF, bInheritHandle, dwProcessId);
        }
    }

    internal class PathOperations
    {
        // Based on GetApplicationPath function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
        static internal string GetApplicationPath(string p_name)
        {
            string displayName;
            string installDir;
            RegistryKey key;

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey?.GetValue("DisplayName") as string;

                if (displayName != null)
                {
                    if (InstallDirCheck(displayName, p_name, subkey, out installDir))
                        return installDir;
                }
            }

            EnsureAdminRights();

            // search in: LocalMachine_32
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey?.GetValue("DisplayName") as string;

                if (displayName != null)
                {
                    if (InstallDirCheck(displayName, p_name, subkey, out installDir))
                        return installDir;
                }
            }

            // search in: LocalMachine_64
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey?.GetValue("DisplayName") as string;

                if (displayName != null)
                {
                    if (InstallDirCheck(displayName, p_name, subkey, out installDir))
                        return installDir;
                }
            }

            // NOT FOUND
            return null;
        }

        // InstallDirCheck function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
        private static bool InstallDirCheck(string displayName, string p_name, RegistryKey subkey, out string installDir)
        {
            const string RegexNotASCII = @"[^\x00-\x80]+";
            installDir = string.Empty;

            // Check for non-English characters in displayName (CN, KR, ...) 
            if (Regex.IsMatch(displayName, RegexNotASCII))
            {
                // check if InstallLocation path contains ELDEN RING sind displayName contains non-standard characters 
                installDir = subkey.GetValue("InstallLocation") as string;
                if (installDir != null && installDir.Contains(p_name))
                {
                    // Not needed but just an additional check to see if eldenring.exe is in the InstallLocation path
                    if (File.Exists(installDir + @"\Game\eldenring.exe"))
                        return true;
                }
            }
            else if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase))
            {
                installDir = subkey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installDir))
                    return true;
            }

            return false;
        }

        public static void EnsureAdminRights()
        {
            if (!IsRunningAsAdmin())
                RelaunchAsAdmin();
        }

        private static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RelaunchAsAdmin()
        {
            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            ProcessStartInfo processInfo = new ProcessStartInfo()
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                MessageBox.Show(ProgramData.DIALOG_CANT_RUN_AS_ADMIN, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Based on PromptForGamePath function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
        internal static string PromptForGamePath()
        {
            MessageBox.Show(ProgramData.DIALOG_CANT_FIND_PATH, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Exclamation);

            string gameExePath = null;

            Thread thread = new Thread(() =>
            {
                gameExePath = OpenFile("Select eldenring.exe", "C:\\", new[] { "*.exe" }, new[] { "Elden Ring Executable" }, true);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (string.IsNullOrEmpty(gameExePath) || !File.Exists(gameExePath))
                Environment.Exit(0);

            var fileInfo = FileVersionInfo.GetVersionInfo(gameExePath);

            if (!fileInfo.FileDescription.ToLower().Contains(EldenRingData.PROCESS_DESCRIPTION))
            {
                MessageBox.Show(ProgramData.DIALOG_EXE_NOT_VALID, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Environment.Exit(0);
            }
            EldenRingData.DEFAULT_GAMENAME = Path.GetFileNameWithoutExtension(gameExePath);
            return gameExePath;
        }

        // OpenFile function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
        private static string OpenFile(string title, string defaultDir, string[] defaultExt, string[] filter, bool explicitFilter = false)
        {
            if (defaultExt.Length != filter.Length)
                throw new ArgumentOutOfRangeException("defaultExt must be the same length as filter!");
            string fullFilter = "";
            if (explicitFilter)
            {
                fullFilter = filter[0] + "|" + defaultExt[0];
            }
            else
            {
                for (int i = 0; i < defaultExt.Length; i++)
                {
                    if (i > 0)
                        fullFilter += "|";
                    fullFilter += filter[i] + " (*" + defaultExt[i] + ")|*" + defaultExt[i];
                }
            }

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = title,
                InitialDirectory = defaultDir,
                // DefaultExt = defaultExt,
                Filter = fullFilter,
                FilterIndex = 0,
            };
            bool? result = dlg.ShowDialog();
            if (result != true)
                return null;
            return File.Exists(dlg.FileName) ? dlg.FileName : null;
        }
    }
}
