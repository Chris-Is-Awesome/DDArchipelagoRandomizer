using MagicUI.Core;
using MagicUI.Elements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer.UI;

internal abstract class CustomUI
{
	protected enum BackgroundSpriteType
	{
		None,
		Border,
		Blur,
	}

	protected LayoutRoot layoutRoot;
	protected Panel rootPanel;
	private static Dictionary<BackgroundSpriteType, Sprite> backgroundSprites;

	protected abstract string Name { get; set; }
	protected virtual BackgroundSpriteType BackgroundType { get; set; } = BackgroundSpriteType.Border;
	protected virtual float BackgroundAlpha { get; set; } = 1f;
	protected virtual int PanelWidth { get; set; } = 100;
	protected virtual int PanelHeight { get; set; } = 100;
	protected virtual HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
	protected virtual VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;
	protected virtual Padding Padding { get; set; } = new Padding(0);
	protected virtual float FadeDuration { get; set; } = 0.5f;
	protected virtual bool Persist { get; set; } = false;
	protected virtual bool DoFade { get; set; } = true;

	protected bool IsShowing { get; private set; }
	protected bool IsFading { get; private set; }

	public static void CacheBackgroundSprite()
	{
		Sprite borderSprite = TitleScreen.instance.saveMenu.saveSlots[0].borders[0].sprite;
		Sprite blurSprite = TitleScreen.instance.keybindMenu.gamepadSubMenu.transform.Find("__Col_Action/KEYBIND_").GetComponent<UnityEngine.UI.Image>().sprite;

		backgroundSprites = new Dictionary<BackgroundSpriteType, Sprite>()
		{
			{ BackgroundSpriteType.Border, borderSprite },
			{ BackgroundSpriteType.Blur, blurSprite }
		};
	}

	public virtual void Show()
	{
		if (layoutRoot == null || layoutRoot.Canvas == null)
		{
			Create();
		}

		layoutRoot.Canvas.SetActive(true);

		if (DoFade)
		{
			Plugin.Instance.StartCoroutine(FadeIn());
		}

		IsShowing = true;
	}

	public virtual void Hide()
	{
		if (layoutRoot == null || layoutRoot.Canvas == null)
		{
			return;
		}

		if (DoFade)
		{
			Plugin.Instance.StartCoroutine(FadeOut());
		}
		else
		{
			layoutRoot.Canvas.SetActive(false);
		}

		IsShowing = false;
	}

	protected virtual void Create()
	{
		if (layoutRoot != null && layoutRoot.Canvas != null)
		{
			return;
		}

		layoutRoot = new(Persist, Name);
		rootPanel = new Panel(layoutRoot, GetBackgroundSprite(), $"{Name}_Panel")
		{
			HorizontalAlignment = HorizontalAlignment,
			VerticalAlignment = VerticalAlignment,
			Padding = Padding,
		};
	}

	private Sprite GetBackgroundSprite()
	{
		if (backgroundSprites == null)
		{
			CacheBackgroundSprite();
		}
		if (backgroundSprites.TryGetValue(BackgroundType, out Sprite sprite))
		{
			return sprite;
		}

		// Return empty sprite. Returning null would null ref when creating panel
		return Sprite.Create(new Texture2D(0, 0), new Rect(0, 0, 0, 0), new Vector2(0.5f, 0.5f));
	}

	private IEnumerator FadeIn()
	{
		layoutRoot.Opacity = 0;
		layoutRoot.BeginFade(1, FadeDuration);
		IsFading = true;

		yield return new WaitForSeconds(FadeDuration);

		// Disable object if not visible
		if (layoutRoot.Opacity <= 0)
		{
			layoutRoot.Canvas.SetActive(false);
		}

		IsFading = false;
	}

	private IEnumerator FadeOut()
	{
		layoutRoot.BeginFade(0, FadeDuration);
		IsFading = true;

		yield return new WaitForSeconds(FadeDuration);

		IsFading = false;
	}
}