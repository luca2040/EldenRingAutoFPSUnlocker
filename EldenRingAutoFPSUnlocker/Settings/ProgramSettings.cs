using Microsoft.Win32;

namespace EldenRingAutoFPSUnlocker.settings
{
  internal static class FPSUnlockerSettings
  {
    internal const string REGISTRY_SETTINGS_PATH = @"Software\EldenRingAutoFPSUnlocker";
    internal const string REGISTRY_GAMEPATH_KEY = "gamepath"; // Store game path in registry to not ask user every time if path cant be automatically found

    static internal string DEFAULT_GAMEPATH = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game";
    static internal int DEFAULT_REFRESHRATE = 165; // Use this if cannot automatically read monitor's refreshrate and user doesnt set it
    internal const int OVERLAY_TIME = 4000; // Time in milliseconds the overlay stays there

    // Config
    // Commented out value is default
    static internal int targetFramerate = -1; // -1 -> automatically set max framerate based on monitor's refreshrate
    static internal bool showConfirmOverlay = true; // true
    static internal bool useSeamlessCoopMod = true; // true
    static internal bool autoOpenSteam = true; // true
    static internal bool minimizeSteamWindows = true; // true
    static internal bool closeSteamWindows = false; // false

    static internal string GetSavedGamepath()
    {
      RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_SETTINGS_PATH);

      if (key != null)
      {
        object value = key.GetValue(REGISTRY_GAMEPATH_KEY);
        key.Close();

        if (value != null)
          return value.ToString();
      }

      return null;
    }

    static internal void SaveGamepath(string newPath)
    {
      if (GetSavedGamepath() == newPath) return;

      RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(REGISTRY_SETTINGS_PATH);

      if (registryKey != null)
      {
        registryKey.SetValue(REGISTRY_GAMEPATH_KEY, newPath);
        registryKey.Close();
      }
    }
  }
}