using System;
using System.Collections.Generic;
using System.Linq;

namespace DDoor.ArchipelagoRandomizer;

public static class Logger
{
	public static void Log(object message)
	{
		Plugin.Logger.LogMessage(message);
	}

	public static void LogWarning(object message)
	{
		Plugin.Logger.LogWarning(message);
	}

	public static void LogError(object message, bool includeTrace = true)
	{
		Plugin.Logger.LogError(includeTrace ? message + "\n" + GetStackTrace() : message);
	}

	private static string GetStackTrace()
	{
		string stack = Environment.StackTrace;
		string[] lines = stack.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		return string.Join("\n", lines.Skip(3));
	}
	public static void LogList<T>(List<T> list)
	{
		foreach (T item in list)
		{
			Plugin.Logger.LogDebug(item.ToString());
		}
	}
}