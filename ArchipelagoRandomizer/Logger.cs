using System.Collections.Generic;

namespace DDoor.ArchipelagoRandomizer;

static class Logger
{
	public static void Log(string message)
	{
		Plugin.Logger.LogMessage(message);
	}

	public static void LogWarning(string message)
	{
		Plugin.Logger.LogWarning(message);
	}

	public static void LogError(string message)
	{
		Plugin.Logger.LogError(message);
	}
	public static void LogList<T>(List<T> list)
	{
		foreach (T item in list)
		{
			Plugin.Logger.LogDebug(item.ToString());
		}
	}
}