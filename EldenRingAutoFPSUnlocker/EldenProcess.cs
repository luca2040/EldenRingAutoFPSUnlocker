using EldenRingAutoFPSUnlocker.utils;
using EldenRingAutoFPSUnlocker.settings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;

namespace EldenRingAutoFPSUnlocker
{
  internal class EldenProcess
  {
    internal string GameProcessName;
    internal string GameFolderPath;
    internal string GameExePath;
    internal string SeamlessCoopPath;

    internal Process gameProcess;
    internal IntPtr gameHwnd = IntPtr.Zero;
    internal IntPtr gameAccessHwnd = IntPtr.Zero;
    internal static IntPtr gameAccessHwndStatic;

    internal long framelock_offset = 0x0;
    internal long hertzlock_offset = 0x0;

    internal EldenProcess(string proc_name)
    {
      GameProcessName = proc_name;

      int maxFramerate = MonitorInfo.GetMaxRefreshrate();
      if (maxFramerate > 0) FPSUnlockerSettings.DEFAULT_REFRESHRATE = maxFramerate;
    }

    // Based on SafeStartGame function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
    internal async Task StartGame()
    {
      // PathOperations.EnsureAdminRights();

      try
      {
        File.WriteAllText(Path.Combine(GameFolderPath, "steam_appid.txt"), "1245620");
      }
      catch
      {
        MessageBox.Show(ProgramData.DIALOG_CANT_WRITE_STEAM_ID, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        Environment.Exit(0);
      }

      if (FPSUnlockerSettings.autoOpenSteam)
      {
        LogFile.Log("Ensuring Steam is opened");

        Process[] procList = Process.GetProcessesByName("steam");
        if (procList.Length == 0)
        {
          ProcessStartInfo siSteam = new ProcessStartInfo
          {
            WindowStyle = ProcessWindowStyle.Minimized,
            Verb = "open",
            FileName = "steam://open/library",
          };
          Process procSteam = new Process
          {
            StartInfo = siSteam
          };
          procSteam.Start();

          await WaitForProgram("steam", 15000);
          await WaitForSteamReady();
          await Task.Delay(5000);
        }

        if (FPSUnlockerSettings.closeSteamWindows) { _ = MinimizeSteamWindows(true); }
        else { if (FPSUnlockerSettings.minimizeSteamWindows) _ = MinimizeSteamWindows(); }
      }

      if (FPSUnlockerSettings.useSeamlessCoopMod && (SeamlessCoopPath != null)) await StartGameSeamlessCoop();
      else await StartGameOriginal();
    }

    private async Task StartGameSeamlessCoop()
    {
      ProcessStartInfo siGame = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Hidden,
        // Verb = "runas",
        FileName = "cmd.exe",
        WorkingDirectory = GameFolderPath,
        Arguments = $"/C \"{EldenRingData.SEAMLESS_COOP_MOD_NAME}.exe\""
      };
      Process procGameStarter = new Process
      {
        StartInfo = siGame
      };
      procGameStarter.Start();
      await WaitForProgram(EldenRingData.DEFAULT_GAMENAME, 12000);
      await Task.Delay(4000);
      procGameStarter.Close();
    }

    private async Task StartGameOriginal()
    {
      ProcessStartInfo siGame = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Hidden,
        // Verb = "runas",
        FileName = "cmd.exe",
        WorkingDirectory = GameFolderPath,
        Arguments = $"/C \"{EldenRingData.DEFAULT_GAMENAME}.exe -noeac\""
      };
      Process procGameStarter = new Process
      {
        StartInfo = siGame
      };
      procGameStarter.Start();
      await WaitForProgram(EldenRingData.DEFAULT_GAMENAME, 10000);
      await Task.Delay(4000);
      procGameStarter.Close();
    }

    internal async Task<bool> IsGameRunning(bool safeStarted = false)
    {
      Process[] eldenRingProcesses = GetEldenRingProcesses();

      if (!eldenRingProcesses.Any() || eldenRingProcesses[0].HasExited)
      {
        LogFile.Log("No elden ring processes found");
        return false;
      }

      for (int i = 0; i < EldenRingData.PROCESS_RUNNING_CHECK_RETRIES; i++)
      {
        if (eldenRingProcesses[0].HasExited)
        {
          LogFile.Log("Game exited");
          return false;
        }

        if (eldenRingProcesses[0].Responding)
          break;

        await Task.Delay(EldenRingData.PROCESS_RUNNING_CHECK_INTERVAL);
      }

      if (safeStarted) return true;

      bool isEACRunning = ServiceController.GetServices()
            .Any(service => service.ServiceName.Contains("EasyAntiCheat") &&
                            (service.Status == ServiceControllerStatus.Running ||
                             service.Status == ServiceControllerStatus.ContinuePending ||
                             service.Status == ServiceControllerStatus.StartPending));

      if (isEACRunning)
      {
        MessageBoxResult result = MessageBox.Show(ProgramData.DIALOG_EAC_RUNNING_WARNING, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        switch (result)
        {
          case MessageBoxResult.Yes:
            MessageBoxResult result_confirm = MessageBox.Show(ProgramData.DIALOG_EAC_CONFIRM_RUNNING_WARNING, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (result_confirm == MessageBoxResult.Yes)
            {
              LogFile.Log("Running mod with EAC - (Oh no... dont say i didnt warn you if u get banned)");
              return true; // Return early because eac doesnt let you check steam appid file
            }
            else
            {
              LogFile.Log("Not started in online mode");
              Environment.Exit(0);
            }

            break;
          case MessageBoxResult.No:
          default:
            LogFile.Log("Not started in online mode");
            Environment.Exit(0);
            break;
        }
      }

      LogFile.Log("Checking steam appid file");

      if (!File.Exists(Path.Combine(Path.GetDirectoryName(eldenRingProcesses[0].MainModule.FileName), EldenRingData.STEAM_APPID_FILENAME)))
      {
        MessageBoxResult result = MessageBox.Show(ProgramData.DIALOG_ONLINE_RUNNING_WARNING, ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        switch (result)
        {
          case MessageBoxResult.Yes:
            LogFile.Log("Running mod in online mode");
            break;
          case MessageBoxResult.No:
          default:
            LogFile.Log("Not started in online mode");
            Environment.Exit(0);
            break;
        }
      }

      return true;
    }

    // Based on SafeStartGame function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
    internal void LoadGamePath(string path = null, bool retrying = false)
    {
      GameExePath = FPSUnlockerSettings.DEFAULT_GAMEPATH;

      if (!File.Exists(GameExePath))
      {
        GameFolderPath = path ?? PathOperations.GetApplicationPath(EldenRingData.APPLICATION_NAME);

        if (GameFolderPath == null || (!File.Exists(Path.Combine(GameFolderPath, $"{EldenRingData.DEFAULT_GAMENAME}.exe")) && !File.Exists(Path.Combine(GameFolderPath, "GAME", $"{EldenRingData.DEFAULT_GAMENAME}.exe"))))
        {
          if (path != null && !retrying) LoadGamePath(PathOperations.GetApplicationPath(EldenRingData.APPLICATION_NAME), true);
          else GameExePath = PathOperations.PromptForGamePath();
        }
        else
        {
          if (File.Exists(Path.Combine(GameFolderPath, "GAME", $"{EldenRingData.DEFAULT_GAMENAME}.exe")))
            GameExePath = Path.Combine(GameFolderPath, "GAME", $"{EldenRingData.DEFAULT_GAMENAME}.exe");
          else if (File.Exists(Path.Combine(GameFolderPath, $"{EldenRingData.DEFAULT_GAMENAME}.exe")))
            GameExePath = Path.Combine(GameFolderPath, $"{EldenRingData.DEFAULT_GAMENAME}.exe");
          else
          {
            if (path != null && !retrying) LoadGamePath(PathOperations.GetApplicationPath(EldenRingData.APPLICATION_NAME), true);
            else GameExePath = PathOperations.PromptForGamePath();
          }
        }
      }
      else
      {
        var fileInfo = FileVersionInfo.GetVersionInfo(GameExePath);
        if (!fileInfo.FileDescription.ToLower().Contains(EldenRingData.PROCESS_DESCRIPTION))
          GameExePath = PathOperations.PromptForGamePath();
      }

      GameFolderPath = Path.GetDirectoryName(GameExePath);
      SeamlessCoopPath = Path.Combine(GameFolderPath, $"{EldenRingData.SEAMLESS_COOP_MOD_NAME}.exe");
      if (!File.Exists(SeamlessCoopPath)) SeamlessCoopPath = null;
    }


    internal async Task<bool> OpenGameProcess(int retries = 0)
    {
      Process[] eldenRingProcesses = GetEldenRingProcesses();

      if (eldenRingProcesses.Length != 1)
      {
        LogFile.Log($"No Elden Ring process or more than one found. Retrying in {EldenRingData.PROCESS_FOUND_RETRY_DELAY} ms...");
        if (retries > EldenRingData.PROCESS_OPEN_RETRY_NUMBER) return false;
        await Task.Delay(EldenRingData.PROCESS_FOUND_RETRY_DELAY);
        return await OpenGameProcess(retries + 1);
      }

      gameProcess = eldenRingProcesses[0];

      gameHwnd = gameProcess.MainWindowHandle;
      gameAccessHwnd = MemoryOperations.OpenPrcessAllAccess(false, (uint)gameProcess.Id);
      gameAccessHwndStatic = gameAccessHwnd;

      bool isGameOpened = CheckGameOpened();

      if (!isGameOpened)
      {
        if (retries > EldenRingData.PROCESS_OPEN_RETRY_NUMBER) return false;
        LogFile.Log("Cannot open process. Retrying in 5 seconds...");
        await Task.Delay(5000);
        return await OpenGameProcess(retries + 1);
      }

      return true;
    }

    // Based on CanPatchGame and PatchFramelock functions from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
    public (bool framelock_patched, bool hertzlock_patched, int fps) PatchGame()
    {
      gameProcess.Refresh();
      if (gameProcess.HasExited || !gameProcess.Responding)
        return (false, false, -1);

      bool framelock_p = false;
      bool hertzlock_p = false;

      int fps = -1;

      if (framelock_offset != 0x0)
      {
        fps = FPSUnlockerSettings.DEFAULT_REFRESHRATE;
        if (FPSUnlockerSettings.targetFramerate > 0) fps = FPSUnlockerSettings.targetFramerate;

        float dt = 1.0f / fps;

        MemoryOperations.WriteBytes(framelock_offset, BitConverter.GetBytes(dt));

        framelock_p = true;
      }

      if (hertzlock_offset != 0x0)
      {
        int noHertzLock = 0x00000000;

        MemoryOperations.WriteBytes(hertzlock_offset + EldenRingData.PATTERN_HERTZLOCK_OFFSET_INTEGER1, BitConverter.GetBytes(noHertzLock));
        MemoryOperations.WriteBytes(hertzlock_offset + EldenRingData.PATTERN_HERTZLOCK_OFFSET_INTEGER2, BitConverter.GetBytes(noHertzLock));

        hertzlock_p = true;
      }

      return (framelock_p, hertzlock_p, fps);
    }

    // Based on ReadGame function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
    public void GetMemoryOffsets()
    {
      if (gameProcess?.MainModule != null)
      {
        PatternScan patternScan = new PatternScan(gameAccessHwnd, gameProcess.MainModule);

        framelock_offset = patternScan.FindPattern(EldenRingData.PATTERN_FRAMELOCK) + EldenRingData.PATTERN_FRAMELOCK_OFFSET;
        if (!MemoryOperations.IsValidAddress(framelock_offset))
        {
          framelock_offset = patternScan.FindPattern(EldenRingData.PATTERN_FRAMELOCK_FUZZY) + EldenRingData.PATTERN_FRAMELOCK_OFFSET_FUZZY;
          if (!MemoryOperations.IsValidAddress(framelock_offset))
            framelock_offset = 0x0;
        }

        hertzlock_offset = patternScan.FindPattern(EldenRingData.PATTERN_HERTZLOCK) + EldenRingData.PATTERN_HERTZLOCK_OFFSET;
        if (!MemoryOperations.IsValidAddress(hertzlock_offset))
          hertzlock_offset = 0x0;
        else
        {
          byte[] _patch_hertzlock_disable = new byte[EldenRingData.PATCH_HERTZLOCK_INSTRUCTION_LENGTH];
          if (!DllAPI.ReadProcessMemory(gameAccessHwndStatic, hertzlock_offset, _patch_hertzlock_disable, EldenRingData.PATCH_HERTZLOCK_INSTRUCTION_LENGTH, out _))
            hertzlock_offset = 0x0;
        }
      }
      else
        LogFile.Log("Error opening process MainModule");
    }

    // WaitForProgram function from MainWindow.xaml.cs in https://github.com/uberhalit/EldenRingFpsUnlockAndMore
    private async Task<bool> WaitForProgram(string appName, int timeout = 5000)
    {
      int timePassed = 0;
      while (true)
      {
        Process[] procList = Process.GetProcessesByName(appName);
        foreach (Process proc in procList)
        {
          if (proc.ProcessName == appName && proc.Responding)
            return true;
        }

        await Task.Delay(500);
        timePassed += 500;
        if (timePassed > timeout)
          return false;
      }
    }

    private async Task<bool> WaitForSteamReady(long PIDtimeout = 10000, long ActiveUserTimeout = 60000)
    {
      long timePassed = 0;
      long PIDfoundTime = 0;
      bool PIDfound = false;

      while (true)
      {
        int steamPID = SteamChecker.GetRegistrySavedPID();
        int currentUser = SteamChecker.GetRegistryActiveUser();

        if (steamPID != 0 && !PIDfound)
        {
          PIDfound = true;
          PIDfoundTime = timePassed;
        }
        if (steamPID != 0 && currentUser != 0) return true;

        await Task.Delay(500);
        timePassed += 500;

        if (PIDfound)
        {
          if (timePassed > (ActiveUserTimeout + PIDfoundTime))
            return false;
        }
        else
        {
          if (timePassed > PIDtimeout)
            return false;
        }
      }
    }

    private static async Task MinimizeSteamWindows(bool closeWindows = false, int timeout = 15000)
    {
      int timePassed = 0;
      while (true)
      {
        IntPtr[] hWnds = FindWindow("steam");

        foreach (IntPtr hWnd in hWnds)
        {
          if (!hWnd.Equals(IntPtr.Zero))
          {
            if (closeWindows) DllAPI.SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero); // 0x0010 = close
            else DllAPI.ShowWindowAsync(hWnd, 2); // 2 = minimize
          }
        }

        await Task.Delay(800);
        timePassed += 800;
        if (timePassed > timeout)
        {
          LogFile.Log("Finished checking for steam windows");
          break;
        }
      }
    }

    private static IntPtr[] FindWindow(string titleName)
    {
      var foundWindows = new List<IntPtr>();

      Process[] pros = Process.GetProcesses(".");
      foreach (Process p in pros)
        if (p.MainWindowTitle.ToUpper().Contains(titleName.ToUpper()))
          foundWindows.Add(p.MainWindowHandle);

      return foundWindows.ToArray();
    }

    private static Process[] GetEldenRingProcesses()
    {
      return Process.GetProcessesByName(EldenRingData.PROCESS_NAME);
    }

    private bool CheckGameOpened()
    {
      return !(gameHwnd == IntPtr.Zero || gameAccessHwnd == IntPtr.Zero || gameProcess?.MainModule?.BaseAddress == IntPtr.Zero || gameProcess == null);
    }
  }

  static internal class SteamChecker
  {
    private const string REGISTRY_STEAM_PROCESS_PATH = @"Software\Valve\Steam\ActiveProcess";
    private const string REGISTRY_STEAM_PID_KEY = "pid";
    private const string REGISTRY_STEAM_ACTIVEUSER_KEY = "ActiveUser";

    internal static int GetRegistrySavedPID()
    {
      return ReadRegistryDword(REGISTRY_STEAM_PROCESS_PATH, REGISTRY_STEAM_PID_KEY);
    }

    internal static int GetRegistryActiveUser()
    {
      return ReadRegistryDword(REGISTRY_STEAM_PROCESS_PATH, REGISTRY_STEAM_ACTIVEUSER_KEY);
    }

    private static int ReadRegistryDword(string keyPath, string keyName)
    {
      RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath);

      if (key != null)
      {
        object value = key.GetValue(keyName);
        key.Close();

        if (value != null)
          return (int)value;
      }

      return 0;
    }
  }
}