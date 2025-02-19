using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using EldenRingAutoFPSUnlocker.utils;

namespace EldenRingAutoFPSUnlocker.settings
{
  internal static class ConfigFile
  {
    internal static string configFileFolderPath = null;
    internal static string configFilePath = null;

    internal static void CheckConfigFile(EldenProcess eldenRing)
    {
      configFileFolderPath = Path.Combine(eldenRing.GameFolderPath, EldenRingData.CONFIG_FOLDER_NAME);

      if (!Directory.Exists(configFileFolderPath))
      {
        Directory.CreateDirectory(configFileFolderPath);
      }

      configFilePath = Path.Combine(configFileFolderPath, EldenRingData.CONFIG_FILE_NAME);

      if (!File.Exists(configFilePath))
      {
        ExtractDefaultConfigFile();
      }
    }

    private static void ExtractDefaultConfigFile()
    {
      var assembly = Assembly.GetExecutingAssembly();
      string resourceFullPath = $"{assembly.GetName().Name}.{EldenRingData.DEFAULT_EMBEDDED_CONFIG_FILE_PATH}";

      using (Stream resourceStream = assembly.GetManifestResourceStream(resourceFullPath))
      {
        if (resourceStream == null)
        {
          throw new FileNotFoundException("Embedded config file not found", resourceFullPath);
        }

        using (FileStream fileStream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
        {
          resourceStream.CopyTo(fileStream);
        }
      }
    }

    internal static T ReadValue<T>(string section, string key, T defaultValue)
    {
      StringBuilder temp = new StringBuilder(255);
      DllAPI.GetPrivateProfileString(section, key, defaultValue.ToString(), temp, temp.Capacity, configFilePath);
      string result = temp.ToString();

      if (int.TryParse(result, out int intValue))
      {
        if (typeof(T) == typeof(bool))
        {
          if (intValue != 0 && intValue != 1)
          {
            LogFile.Log($"Value {key} must be 0 or 1");
            MessageBox.Show(ProgramData.DIALOG_CONFIG_VALUE_NOT_VALID_BOOLEAN(key), ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
          }

          return (T)(object)(intValue != 0);
        }

        return (T)(object)intValue;
      }
      else
      {
        LogFile.Log($"Cant read value {key}");
        MessageBox.Show(ProgramData.DIALOG_INT_NOT_VALID_CONFIG(key), ProgramData.DIALOG_WINDOWS_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
        Environment.Exit(1);
      }

      return default;
    }

    internal static void LoadConfigs()
    {
      FPSUnlockerSettings.targetFramerate = ReadValue("FRAMERATE", "target_framerate", FPSUnlockerSettings.targetFramerate);
      FPSUnlockerSettings.showConfirmOverlay = ReadValue("FRAMERATE", "confirm_overlay", FPSUnlockerSettings.showConfirmOverlay);
      FPSUnlockerSettings.useSeamlessCoopMod = ReadValue("INTEGRATIONS", "use_seamless_coop_mod", FPSUnlockerSettings.useSeamlessCoopMod);
      FPSUnlockerSettings.autoOpenSteam = ReadValue("STEAM", "ensure_steam_opened", FPSUnlockerSettings.autoOpenSteam);
      FPSUnlockerSettings.minimizeSteamWindows = ReadValue("STEAM", "minimize_steam_windows", FPSUnlockerSettings.minimizeSteamWindows);
      FPSUnlockerSettings.closeSteamWindows = ReadValue("STEAM", "close_steam_windows", FPSUnlockerSettings.closeSteamWindows);

      // Commented out value is default
      LogFile.Log($"targetFramerate value: {FPSUnlockerSettings.targetFramerate}"); // -1 -> automatically set max framerate based on monitor's refreshrate
      LogFile.Log($"showConfirmOverlay value: {FPSUnlockerSettings.showConfirmOverlay}"); // true
      LogFile.Log($"useSeamlessCoopMod value: {FPSUnlockerSettings.useSeamlessCoopMod}"); // true
      LogFile.Log($"autoOpenSteam value: {FPSUnlockerSettings.autoOpenSteam}"); // true
      LogFile.Log($"minimizeSteamWindows value: {FPSUnlockerSettings.minimizeSteamWindows}"); // true
      LogFile.Log($"closeSteamWindows value: {FPSUnlockerSettings.closeSteamWindows}"); // false
    }
  }
}