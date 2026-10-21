using DDoor.ArchipelagoRandomizer.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

#nullable enable
/// <summary>
/// While the game is paused, Tab (keyboard) or Y (controller) opens a panel on top of the pause menu with where the
/// randomized doors you have been through lead (per area, starting with the one you are in; Page Up / Page Down or
/// the triggers to look at other areas) and, with Shop Upgrades on, the stat levels you have received. The game shows
/// neither: the shop bars show what was bought, not received. A small hint on the pause menu says how to open it.
/// </summary>
internal static class PauseTracker
{
	private const string Accent = "#FFD27A";
	private const string Dim = "#9A9A9A";
	private const float RefreshInterval = 0.5f;

	private static readonly TrackerPanel panel = new("TrackerPanel", false);
	private static readonly TrackerPanel hint = new("TrackerHint", true);
	private static bool wasPaused;
	private static bool open;
	private static bool loggedThisPause;
	private static float logStateAt;
	private static int areaIndex;
	private static float nextRefresh;

	// Groups of randomized exits, one per scene that has any, in the order of SceneTransitions.json
	private static List<DoorGroup>? groups;

	private sealed class DoorGroup(string scene, string title, List<string> exits)
	{
		public readonly string scene = scene;
		public readonly string title = title;
		public readonly List<string> exits = exits;
	}

	internal static void Init()
	{
		Plugin.Instance.OnUpdate += Update;
		Archipelago.OnConnected += () => groups = null;
	}

	private static void Update()
	{
		bool paused = IsPaused();
		if (!paused)
		{
			if (wasPaused)
			{
				panel.Hide();
				hint.Hide();
				wasPaused = false;
				open = false;
			}
			return;
		}

		if (!wasPaused)
		{
			wasPaused = true;
			open = false;
			loggedThisPause = false;
			logStateAt = Time.unscaledTime + 1f;
			areaIndex = CurrentAreaIndex();
			nextRefresh = 0;
		}

		InControl.InputDevice device = InControl.InputManager.ActiveDevice;
		if (Input.GetKeyDown(KeyCode.Tab) || (device != null && device.Action4.WasPressed))
		{
			open = !open;
			nextRefresh = 0;
			Logger.Log($"Tracker panel {(open ? "opened" : "closed")}");
			logStateAt = Time.unscaledTime + 1f;
		}

		List<DoorGroup> doorGroups = GetGroups();
		if (open && doorGroups.Count > 0)
		{
			if (Input.GetKeyDown(KeyCode.PageDown) || (device != null && device.RightTrigger.WasPressed))
			{
				areaIndex = (areaIndex + 1) % doorGroups.Count;
				nextRefresh = 0;
			}
			else if (Input.GetKeyDown(KeyCode.PageUp) || (device != null && device.LeftTrigger.WasPressed))
			{
				areaIndex = (areaIndex - 1 + doorGroups.Count) % doorGroups.Count;
				nextRefresh = 0;
			}
		}

		// Time is stopped while paused, so count in real time
		if (Time.unscaledTime < nextRefresh)
		{
			return;
		}
		nextRefresh = Time.unscaledTime + RefreshInterval;

		string text = BuildText(doorGroups);
		if (!loggedThisPause)
		{
			loggedThisPause = true;
			Logger.Log($"Paused: tracker text {text.Length} chars, {doorGroups.Count} door areas, shop {ShopAndPlanting.ShopActive}");
		}
		if (text.Length == 0)
		{
			panel.Hide();
			hint.Hide();
			return;
		}
		if (open)
		{
			hint.Hide();
			panel.Show(text + $"\n\n<color={Dim}>[Tab] / (Y) close</color>");
		}
		else
		{
			panel.Hide();
			hint.Show($"<color={Dim}>[Tab] / (Y)  Doors and stats</color>");
		}
		if (logStateAt >= 0 && Time.unscaledTime >= logStateAt)
		{
			logStateAt = -1;
			Logger.Log($"Tracker UI: {panel.Describe()} | {hint.Describe()}");
		}
	}

	private static bool IsPaused()
	{
		if (Archipelago.Instance == null || !Archipelago.Instance.IsConnected() || PlayerGlobal.instance == null)
		{
			return false;
		}
		UIMenuPauseController pause = UIMenuPauseController.instance;
		return pause != null && pause.IsPaused();
	}

	private static string BuildText(List<DoorGroup> doorGroups)
	{
		StringBuilder sb = new();
		if (doorGroups.Count > 0)
		{
			AppendDoors(sb, doorGroups);
		}
		if (ShopAndPlanting.ShopActive)
		{
			if (sb.Length > 0)
			{
				sb.Append("\n\n");
			}
			AppendStats(sb);
		}
		return sb.ToString();
	}

	private static void AppendDoors(StringBuilder sb, List<DoorGroup> doorGroups)
	{
		Dictionary<string, string>? pairings = EntranceRandomizer.Instance?.GetEntrancePairings();
		if (pairings == null)
		{
			return;
		}
		HashSet<string> found = KnownExits(pairings);

		areaIndex = Mathf.Clamp(areaIndex, 0, doorGroups.Count - 1);
		DoorGroup group = doorGroups[areaIndex];
		sb.Append($"<b><size=32>{group.title}</size></b>  <color={Dim}>({areaIndex + 1}/{doorGroups.Count}, PgUp/PgDn or LT/RT)</color>");
		foreach (string exit in group.exits)
		{
			(string _, string label) = Describe(exit);
			sb.Append($"\n{label}  <color={Dim}>-></color>  ");
			if (found.Contains(exit) && pairings.TryGetValue(exit, out string target))
			{
				(string targetArea, string targetLabel) = Describe(target);
				sb.Append($"<color={Accent}>{targetArea}</color>: {targetLabel}");
			}
			else
			{
				sb.Append($"<color={Dim}>?</color>");
			}
		}
	}

	/// <summary>
	/// Exits whose destination you know: the ones you went through, plus (with coupled doors) the way back
	/// </summary>
	private static HashSet<string> KnownExits(Dictionary<string, string> pairings)
	{
		HashSet<string> found = Archipelago.Instance.GetStoredList(Archipelago.FoundEntrancesKey);
		bool coupled = Archipelago.Instance.TryGetSlotData("entrance_randomization", out object er) && System.Convert.ToInt64(er) == 1;
		if (!coupled)
		{
			return found;
		}
		HashSet<string> known = new(found);
		foreach (string exit in found)
		{
			// Coupled: A leads to B's arrival point, so B's exit leads back to A
			if (pairings.TryGetValue(exit, out string target) && pairings.TryGetValue(target, out string back) && back == exit)
			{
				known.Add(target);
			}
		}
		return known;
	}

	private static void AppendStats(StringBuilder sb)
	{
		sb.Append("<b><size=32>Stats received</size></b>");
		foreach (KeyValuePair<string, string> stat in ShopAndPlanting.StatNames)
		{
			sb.Append($"\n{stat.Value}: <color={Accent}>level {ShopAndPlanting.StatLevel(stat.Key)}</color>"
				+ $"  <color={Dim}>({ShopAndPlanting.PurchasedCount(stat.Key)}/5 bought in the shop)</color>");
		}
	}

	private static List<DoorGroup> GetGroups()
	{
		if (groups != null)
		{
			return groups;
		}
		Dictionary<string, string>? pairings = EntranceRandomizer.Instance?.GetEntrancePairings();
		bool randomized = Archipelago.Instance.TryGetSlotData("entrance_randomization", out object er) && System.Convert.ToInt64(er) > 0;
		if (pairings == null || !randomized)
		{
			// Not cached: the entrance randomizer may not be set up yet
			return new List<DoorGroup>();
		}

		List<DoorGroup> result = new();
		foreach (IGrouping<string, SceneTransitions.SceneTransition> scene in SceneTransitions.All
			.Where(t => t.originSceneName != "" && pairings.ContainsKey(t.startingRegion))
			.GroupBy(t => t.originSceneName.ToLowerInvariant()))
		{
			List<string> exits = scene.Select(t => t.startingRegion).Distinct().ToList();
			string title = string.Join(" / ", exits.Select(e => Describe(e).area).Distinct());
			result.Add(new DoorGroup(scene.Key, title, exits));
		}
		groups = result;
		return groups;
	}

	private static int CurrentAreaIndex()
	{
		List<DoorGroup> doorGroups = GetGroups();
		string current = SceneManager.GetActiveScene().name.ToLowerInvariant();
		int index = doorGroups.FindIndex(g => g.scene == current);
		return index >= 0 ? index : 0;
	}

	/// <summary>
	/// Splits an entrance name into its area and a short label: "Lost Cemetery Exit to Crypt" gives (Lost Cemetery,
	/// Exit to Crypt), "Door to Lost Cemetery" gives (Hall of Doors, Door to Lost Cemetery), "Lost Cemetery Door" gives
	/// (Lost Cemetery, Door to the Hall of Doors)
	/// </summary>
	private static (string area, string label) Describe(string name)
	{
		if (name.StartsWith("Door to "))
		{
			return ("Hall of Doors", name);
		}
		int exitAt = name.IndexOf(" Exit to ");
		if (exitAt > 0)
		{
			return (name.Substring(0, exitAt), name.Substring(exitAt + 1));
		}
		if (name.EndsWith(" Ancient Door"))
		{
			return (name.Substring(0, name.Length - " Ancient Door".Length), "Ancient Door");
		}
		if (name.EndsWith(" Door"))
		{
			return (name.Substring(0, name.Length - " Door".Length), "Door to the Hall of Doors");
		}
		return (name, name);
	}
}
