using System;
using System.IO;

namespace EldenRingAutoFPSUnlocker.utils
{
  public static class LogFile
  {
    private const string LOG_FOLDER_NAME = "EldenRingAutoFPSUnlocker";
    private const string LOG_FILE_NAME = "logs.txt";

    internal static string logFilePath = null;

    internal static void GetLogPath()
    {
      string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      logFilePath = Path.Combine(local, LOG_FOLDER_NAME, LOG_FILE_NAME);
    }

    internal static void ResetLogFile()
    {
      if (logFilePath == null) return;
      try
      {
        string directoryPath = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
          Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(logFilePath, string.Empty);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error resetting log file: {ex.Message}");
      }
    }

    public static void Log(string message)
    {
      Console.WriteLine(message);

      if (logFilePath == null) return;

      string messageWithTime = $"[{DateTime.Now:HH:mm:ss}] {message}";
      try
      {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
          writer.WriteLine(messageWithTime);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Cant save to log file.\nException: {ex.Message}\nMessage to log: {message}");
      }
    }
  }
}