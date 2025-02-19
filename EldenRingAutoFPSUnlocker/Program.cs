using EldenRingAutoFPSUnlocker.utils;
using EldenRingAutoFPSUnlocker.settings;
using System;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;

namespace EldenRingAutoFPSUnlocker
{

  public partial class Program
  {
    internal static EldenProcess eldenRing;

    static void ProgramExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception ex)
      {
        if (ex is UnauthorizedAccessException || ex is SecurityException || ex is Win32Exception)
        {
          Console.WriteLine("Restarting as admin");
          PathOperations.EnsureAdminRights();
        }

        Console.WriteLine($"Error: {ex.Message}");
      }

      Environment.Exit(1);
    }

    static async Task Main()
    {
      // PathOperations.EnsureAdminRights();
      AppDomain.CurrentDomain.UnhandledException += ProgramExceptionHandler; // To restart as admin in case of crash

      LogFile.GetLogPath();
      LogFile.ResetLogFile();
      eldenRing = new EldenProcess(EldenRingData.PROCESS_NAME);

      string loadedGamepath = FPSUnlockerSettings.GetSavedGamepath();

      // Check game path

      eldenRing.LoadGamePath(loadedGamepath);
      LogFile.Log($"Gamepath: {eldenRing.GameFolderPath}\nGame exe path: {eldenRing.GameExePath}\nSeamlessCoop path: {eldenRing.SeamlessCoopPath}");

      FPSUnlockerSettings.SaveGamepath(eldenRing.GameFolderPath);

      // Load settings

      ConfigFile.CheckConfigFile(eldenRing);
      ConfigFile.LoadConfigs();

      // Check if game is already running

      bool IsGameAlreadyRunning = await eldenRing.IsGameRunning();
      LogFile.Log($"Game already running: {IsGameAlreadyRunning}");

      // Start game if not already running

      if (!IsGameAlreadyRunning)
      {
        LogFile.Log("Starting game");
        await eldenRing.StartGame();
      }

      // Sometimes that shit doesnt start so check it multiple times

      for (int i = 0; i < EldenRingData.GAME_RESTART_ATTEMPTS; i++)
      {
        LogFile.Log($"Checking if it really started, n: {i}");

        bool HasGameStarted = await eldenRing.IsGameRunning(true);

        if (HasGameStarted)
        {
          LogFile.Log("Game running");
          break;
        }
        else
        {
          LogFile.Log("Game didnt start. Retrying");
          await Task.Delay(EldenRingData.GAME_RESTART_DELAY);
          await eldenRing.StartGame();
        }
      }

      // Open game process

      bool gameOpened = await eldenRing.OpenGameProcess();

      if (!gameOpened)
      {
        LogFile.Log("Process not opened");
        Environment.Exit(1);
      }

      // Read offsets

      LogFile.Log("Reading memory offsets");

      eldenRing.GetMemoryOffsets();

      LogFile.Log($"FrameTick offset: 0x{eldenRing.framelock_offset:X}");
      LogFile.Log($"HertzLock offset: 0x{eldenRing.hertzlock_offset:X}");

      // Patch game

      LogFile.Log("Patching game");

      (bool framelock_patched, bool hertzlock_patched, int setFPS) = eldenRing.PatchGame();

      LogFile.Log($"Setting game max FPS to: {setFPS}");
      LogFile.Log($"Framelock patched: {framelock_patched}\nHertzlock patched: {hertzlock_patched}");

      if (FPSUnlockerSettings.showConfirmOverlay)
      {
        if (framelock_patched)
          OpenOverlayWindow.Open($"FPS unlocked to: {setFPS}", FPSUnlockerSettings.OVERLAY_TIME);
        else
          OpenOverlayWindow.Open("Cannot unlock FPS", FPSUnlockerSettings.OVERLAY_TIME);
      }

      LogFile.Log("Program done.");
      Environment.Exit(0);
    }
  }
}
