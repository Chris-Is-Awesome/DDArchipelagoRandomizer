using DDoor.AddUIToOptionsMenu;
using MagicUI.Core;
using MagicUI.Elements;
using System.Collections;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer.UI;

/// <summary>
/// The Frog King Title Trap: the Frog King's full title shakes onto the screen like his boss intro, then goes away on
/// its own after it has finished (it doesn't wait for a death, respawn or loading zone)
/// </summary>
internal class FrogKingTitle : CustomUI
{
	internal const string Header = "Giant Soul of the Frog King";
	internal const string Body =
		"Guardian of the Flooded Fortress. Lord of his domain.Ruler of all he surveys. Champion of the people. "
		+ "Keeper of the five oaths. First of his name. Second of his other name. He who is without middle initial or mercy. "
		+ "Destroyer of all those who would question his rightful rule or look at him askance. Eater of the vanquished. "
		+ "Devourer of the indolent. Guzzler of the non-committal. Bestower of wrathful opprobrium and fount of most "
		+ "wondrous beneficence. All glory to him and his mace! Holy basher of toads not frogs, but really what are toads "
		+ "anyway? Like worse frogs right? Uglier too. Smartest creature in all the land, really clever like that Einstein "
		+ "guy but more handsome and- okay hit the word count";

	/// <summary>Letters per second while the title is written out</summary>
	private const float lettersPerSecond = 90f;
	/// <summary>How long the finished title stays before fading</summary>
	private const float holdTime = 3f;
	private const float shakeWhileWriting = 6f;
	private const float shakeWhenDone = 2f;

	private TextObject headerObj;
	private TextObject bodyObj;
	private Coroutine running;
	private Shaker shaker;

	protected override string Name { get; set; } = "FrogKingTitle";
	protected override BackgroundSpriteType BackgroundType { get; set; } = BackgroundSpriteType.Blur;
	protected override float FadeDuration { get; set; } = 0.75f;
	protected override bool Persist { get; set; } = true;

	// ---------------------------------------------------------------- the game's own boss intro
	private const string IntroScene = "boss_Frog";
	private const string IntroPath = "_SceneMover/R_Cathedral_DOWN/_CONTENTS/CUTSCENE_FrogBOSSIntro/TEXT";
	private const string CachedName = "AP_FrogKingTitleText";
	/// <summary>The sound the boss intro text plays (BossText's Wwise event)</summary>
	private const string IntroSound = "BossText";
	private static GameObject playingIntro;

	/// <summary>
	/// The Frog King's intro title (its canvas, animated lines and splats) only exists in his boss scene, so it is
	/// preloaded once on connect, unless the seed can't have this trap
	/// </summary>
	internal static void RegisterPreload()
	{
		// Skipped only when the seed says the trap can't be in it and TrapLink is off. Seeds without the flag
		// (earlier builds) and traps sent by the server (/send) still get the real intro.
		bool wanted = Archipelago.Instance.apConfig.TrapLinkEnabled
			|| !Archipelago.Instance.TryGetSlotData("frog_king_title_trap", out object value)
			|| System.Convert.ToBoolean(value);
		if (!wanted)
		{
			return;
		}
		Preloader.Instance.AddObjectToCacheList(IntroScene, () =>
		{
			GameObject text = PathUtil.GetByPath(IntroScene, IntroPath);
			if (text == null)
			{
				Logger.LogError("Frog King Title Trap: couldn't find the Frog King's intro text to preload");
				return [];
			}
			text.name = CachedName;
			return [text];
		});
	}

	/// <summary>Shows the Frog King's real boss intro title with its sound and screen shake. False if it wasn't preloaded.</summary>
	internal static bool PlayGameIntro()
	{
		GameObject cached;
		try
		{
			cached = Preloader.GetCachedObject<GameObject>(CachedName);
		}
		catch
		{
			cached = null;
		}
		if (cached == null)
		{
			return false;
		}
		if (playingIntro != null)
		{
			Object.Destroy(playingIntro);
		}
		GameObject intro = Object.Instantiate(cached);
		intro.name = "AP_FrogKingTitle";
		Object.DontDestroyOnLoad(intro);
		playingIntro = intro;
		Plugin.Instance.StartCoroutine(RunGameIntro(intro));
		return true;
	}

	private static IEnumerator RunGameIntro(GameObject intro)
	{
		Canvas canvas = intro.GetComponent<Canvas>();
		if (canvas != null)
		{
			// It drew through the boss room's camera; on top of everything else here
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 90;
		}
		CanvasGroup group = intro.GetComponent<CanvasGroup>() ?? intro.AddComponent<CanvasGroup>();
		group.alpha = 1;
		group.interactable = false;
		group.blocksRaycasts = false;

		Transform solid = intro.transform.Find("BOLD_TEXT_SOLID");
		if (solid == null)
		{
			Object.Destroy(intro);
			yield break;
		}
		// Its BossText has no text object set (the cutscene's timeline does the work), so it would only throw
		foreach (BossText bossText in solid.GetComponents<BossText>())
		{
			Object.Destroy(bossText);
		}
		solid.gameObject.SetActive(true);
		GameObject splats = solid.Find("Splats")?.gameObject;
		GameObject firstLines = solid.Find("TEXT_1 (1)")?.gameObject;
		GameObject restLines = solid.Find("1")?.gameObject;
		splats?.SetActive(false);
		firstLines?.SetActive(false);
		restLines?.SetActive(false);

		// In the boss room the cutscene's timeline moves the lines into place. Without it, "LORD OF HIS DOMAIN" /
		// "CHAMPION OF THE PEOPLE" wait off screen and the lower titles sit too low, so they are slid up here to
		// where the cutscene ends (measured from the game: same units as the lines' anchored positions).
		RectTransform middleLines = firstLines != null ? firstLines.transform.Find("SolidText3") as RectTransform : null;
		RectTransform lowerLines = restLines != null ? restLines.transform as RectTransform : null;
		float middleTarget = 20f;
		// Tuned against a screenshot of the real intro (1 unit is about 3.5 px at 1080p)
		float lowerTarget = lowerLines != null ? lowerLines.anchoredPosition.y + 35f : 0f;
		if (lowerLines != null)
		{
			// The lower titles also sit a little closer together at the end of the cutscene
			Nudge(lowerLines.Find("SolidText_final (2)"), 3f);  // EATER OF THE VANQUISHED ...
			Transform last = lowerLines.Find("FInal");          // BESTOWER ... / HOLY BASHER ... / SMARTEST ...
			Nudge(last, 3f);
			Nudge(last?.Find("SolidText_final (5)"), -2f);
		}
		middleLines?.gameObject.SetActive(false);

		// KING OF THE SWAMP and GUARDIAN OF THE FLOODED FORTRESS slam in
		Slam(intro);
		firstLines?.SetActive(true);
		splats?.SetActive(true);
		yield return new WaitForSecondsRealtime(1.3f);
		if (intro == null) yield break;

		// Then the middle titles rise up under them
		if (middleLines != null)
		{
			SetY(middleLines, middleTarget - 150f);
			middleLines.gameObject.SetActive(true);
			yield return Slide(middleLines, middleTarget - 150f, middleTarget, 0.35f);
			Slam(intro);
		}
		yield return new WaitForSecondsRealtime(1.2f);
		if (intro == null) yield break;

		// And the rest fill the bottom of the screen
		if (lowerLines != null)
		{
			SetY(lowerLines, lowerTarget - 150f);
			restLines.SetActive(true);
			yield return Slide(lowerLines, lowerTarget - 150f, lowerTarget, 0.35f);
			Slam(intro);
		}
		yield return new WaitForSecondsRealtime(5f);

		// Then it goes away by itself
		for (float t = 0; t < 1f && intro != null; t += Time.unscaledDeltaTime)
		{
			group.alpha = 1 - t;
			yield return null;
		}
		if (intro != null)
		{
			Object.Destroy(intro);
		}
	}

	private static void Nudge(Transform transform, float up)
	{
		if (transform is RectTransform rect)
		{
			SetY(rect, rect.anchoredPosition.y + up);
		}
	}

	private static void SetY(RectTransform rect, float y)
	{
		rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
	}

	private static IEnumerator Slide(RectTransform rect, float from, float to, float duration)
	{
		for (float t = 0; t < duration && rect != null; t += Time.unscaledDeltaTime)
		{
			float k = 1f - Mathf.Pow(1f - t / duration, 3f);  // ease out
			SetY(rect, Mathf.Lerp(from, to, k));
			yield return null;
		}
		if (rect != null)
		{
			SetY(rect, to);
		}
	}

	private static void Slam(GameObject source)
	{
		try
		{
			SoundEngine.Event(IntroSound, source);
			ScreenShake.Shake(0.4f, 16, 0.1f, null);
		}
		catch (System.Exception e)
		{
			Logger.LogError($"Frog King Title Trap sound/shake failed: {e.Message}");
		}
	}

	public void Play()
	{
		if (running != null)
		{
			Plugin.Instance.StopCoroutine(running);
		}
		base.Show();
		Canvas canvas = layoutRoot.Canvas.GetComponent<Canvas>();
		if (canvas != null)
		{
			canvas.sortingOrder = 90;
		}
		running = Plugin.Instance.StartCoroutine(Run());
	}

	protected override void Create()
	{
		base.Create();
		StackLayout stack = new(layoutRoot, $"{Name}_Stack")
		{
			Orientation = Orientation.Vertical,
			Spacing = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Padding(60, 40),
		};
		headerObj = new TextObject(layoutRoot, $"{Name}_Header")
		{
			Text = Header,
			FontSize = 64,
			MaxWidth = 1400,
			TextAlignment = HorizontalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		bodyObj = new TextObject(layoutRoot, $"{Name}_Body")
		{
			// Laid out at full length from the start (hidden letters are transparent) so the text doesn't reflow
			Text = Hidden(0),
			FontSize = 34,
			MaxWidth = 1400,
			TextAlignment = HorizontalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		stack.Children.Add(headerObj);
		stack.Children.Add(bodyObj);
		rootPanel.Child = stack;
		shaker = layoutRoot.Canvas.AddComponent<Shaker>();
		shaker.targets = [headerObj.GameObject.transform, bodyObj.GameObject.transform];
	}

	private static string Hidden(int shown)
	{
		shown = Mathf.Clamp(shown, 0, Body.Length);
		return shown >= Body.Length ? Body : Body.Substring(0, shown) + "<color=#FFFFFF00>" + Body.Substring(shown) + "</color>";
	}

	private IEnumerator Run()
	{
		float start = Time.unscaledTime;
		int shown = 0;
		shaker.intensity = shakeWhileWriting;
		while (shown < Body.Length)
		{
			int now = Mathf.FloorToInt((Time.unscaledTime - start) * lettersPerSecond);
			if (now != shown)
			{
				shown = now;
				bodyObj.Text = Hidden(shown);
			}
			yield return null;
		}
		shaker.intensity = shakeWhenDone;
		yield return new WaitForSecondsRealtime(holdTime);
		shaker.intensity = 0;
		Hide();
		running = null;
	}

	/// <summary>Shakes the text around where the layout put it</summary>
	private class Shaker : MonoBehaviour
	{
		internal Transform[] targets = [];
		internal float intensity;
		private Vector3[] offsets = [];

		private void LateUpdate()
		{
			if (offsets.Length != targets.Length)
			{
				offsets = new Vector3[targets.Length];
			}
			for (int i = 0; i < targets.Length; i++)
			{
				if (targets[i] == null)
				{
					continue;
				}
				// Take away last frame's shake first, so a re-layout in between isn't fought against
				Vector3 basePosition = targets[i].localPosition - offsets[i];
				offsets[i] = intensity > 0 ? (Vector3)(Random.insideUnitCircle * intensity) : Vector3.zero;
				targets[i].localPosition = basePosition + offsets[i];
			}
		}
	}
}
