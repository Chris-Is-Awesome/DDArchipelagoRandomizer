using System;
using System.Linq;

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

	public static void LogError(string message, bool includeTrace = true)
	{
		Plugin.Logger.LogError(includeTrace ? message + "\n" + GetStackTrace() : message);
	}

	private static string GetStackTrace()
	{
		string stack = Environment.StackTrace;
		string[] lines = stack.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		return string.Join("\n", lines.Skip(3));
	}
}