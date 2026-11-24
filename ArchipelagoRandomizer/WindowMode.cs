using System;
using System.IO;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

/// <summary>
/// Stay Visible When Unfocused (mod option, on by default). The game is built with Unity's "visible in background"
/// setting off, so Unity hides the window whenever another window gets focus. That setting lives in
/// DeathsDoor_Data/globalgamemanagers and is only read when the game starts, so the option switches that one byte and
/// the change applies from the next start. A Steam "verify files" or game update puts the original file back; the
/// mod simply applies it again. Builds where the bytes around the setting don't match are left alone.
/// </summary>
internal static class WindowMode
{
	// Where PlayerSettings.visibleInBackground sits in the Steam build's globalgamemanagers (208188 bytes), with the
	// bytes around it to make sure it's the same build
	private const long FileSize = 208188;
	private const int FlagOffset = 4494;
	private static readonly byte[] Before = HexBytes("792e67616d6573000000010000000000");
	private static readonly byte[] After = HexBytes("01010000000000000000000000000000");

	private static bool pendingWrite;

	private static bool Wanted => Archipelago.Instance?.apConfig?.StayVisibleUnfocused ?? false;
	private static string FilePath => Path.Combine(Application.dataPath, "globalgamemanagers");

	private static byte[] HexBytes(string hex)
	{
		byte[] bytes = new byte[hex.Length / 2];
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
		}
		return bytes;
	}

	/// <summary>At startup (Plugin.Awake), when the option is toggled, and again when the game closes if it couldn't write before</summary>
	internal static void Apply(bool announce = false)
	{
		try
		{
			string path = FilePath;
			if (!File.Exists(path) || new FileInfo(path).Length != FileSize)
			{
				Logger.Log("Stay Visible When Unfocused: unknown game build, not changing anything");
				return;
			}
			byte want = Wanted ? (byte)1 : (byte)0;
			using FileStream file = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			byte[] window = new byte[Before.Length + 1 + After.Length];
			file.Seek(FlagOffset - Before.Length, SeekOrigin.Begin);
			if (file.Read(window, 0, window.Length) != window.Length)
			{
				return;
			}
			for (int i = 0; i < Before.Length; i++)
			{
				if (window[i] != Before[i]) { Logger.Log("Stay Visible When Unfocused: unknown game build, not changing anything"); return; }
			}
			for (int i = 0; i < After.Length; i++)
			{
				if (window[Before.Length + 1 + i] != After[i]) { Logger.Log("Stay Visible When Unfocused: unknown game build, not changing anything"); return; }
			}
			byte current = window[Before.Length];
			if (current != 0 && current != 1)
			{
				return;
			}
			pendingWrite = false;
			if (current == want)
			{
				return;
			}
			file.Seek(FlagOffset, SeekOrigin.Begin);
			file.WriteByte(want);
			Logger.Log($"Stay Visible When Unfocused: visible in background set to {want == 1}, applies from the next start");
			if (announce)
			{
				UIManager.Instance.ShowNotification("Restart the game for this to take effect");
			}
		}
		catch (IOException e)
		{
			// The game may still have the file open: try again when it closes
			pendingWrite = true;
			Logger.Log($"Stay Visible When Unfocused: will try again when the game closes ({e.Message})");
			if (announce)
			{
				UIManager.Instance.ShowNotification("This takes effect after restarting the game");
			}
		}
		catch (Exception e)
		{
			Logger.LogError($"Stay Visible When Unfocused failed: {e.Message}");
		}
	}

	/// <summary>Plugin.OnApplicationQuit</summary>
	internal static void OnQuit()
	{
		if (pendingWrite)
		{
			Apply();
		}
	}
}
