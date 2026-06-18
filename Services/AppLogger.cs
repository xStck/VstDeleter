using System;

namespace VstDeleter.Services;

public static class AppLogger
{
    public static event Action<string>? OnLog;

    public static void Log(string message)
    {
        OnLog?.Invoke(message);
        System.Diagnostics.Debug.WriteLine(message);
    }
}
