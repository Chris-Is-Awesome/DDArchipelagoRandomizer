using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer.UI;

/// <summary>
/// Text shown on top of the pause menu (see PauseTracker): the doors and stats panel, and the small hint that says
/// how to open it
/// </summary>
internal class TrackerPanel(string name, bool isHint) : CustomUI
{
	private TextObject textObj;

	protected override string Name { get; set; } = name;
	protected override BackgroundSpriteType BackgroundType { get; set; } = isHint ? BackgroundSpriteType.None : BackgroundSpriteType.Border;
	protected override HorizontalAlignment HorizontalAlignment { get; set; } = isHint ? HorizontalAlignment.Left : HorizontalAlignment.Center;
	protected override VerticalAlignment VerticalAlignment { get; set; } = isHint ? VerticalAlignment.Bottom : VerticalAlignment.Center;
	protected override Padding Padding { get; set; } = isHint ? new Padding(40, 0, 0, 30) : new Padding(0);
	protected override bool Persist { get; set; } = true;
	protected override bool DoFade { get; set; } = false;

	public void Show(string text)
	{
		if (!IsShowing)
		{
			base.Show();
			BringToFront();
		}
		if (textObj != null && textObj.Text != text)
		{
			textObj.Text = text;
		}
	}

	/// <summary>
	/// MagicUI canvases draw at sorting order 0, under the pause menu's canvas (order 5) and its blur. The game's
	/// highest canvas order is 10, so 100 draws above all of them.
	/// </summary>
	private void BringToFront()
	{
		Canvas canvas = layoutRoot?.Canvas != null ? layoutRoot.Canvas.GetComponent<Canvas>() : null;
		if (canvas != null)
		{
			canvas.sortingOrder = 100;
		}
	}

	/// <summary>For the log: what the panel's canvas is doing</summary>
	public string Describe()
	{
		if (layoutRoot == null || layoutRoot.Canvas == null)
		{
			return $"{Name}: not created";
		}
		Canvas canvas = layoutRoot.Canvas.GetComponent<Canvas>();
		return $"{Name}: active {layoutRoot.Canvas.activeInHierarchy}, showing {IsShowing}, order {canvas?.sortingOrder}, "
			+ $"mode {canvas?.renderMode}, opacity {layoutRoot.Opacity}, text '{textObj?.Text?.Length}' chars";
	}

	protected override void Create()
	{
		base.Create();
		textObj = new TextObject(layoutRoot, $"{Name}_Text")
		{
			Text = "",
			FontSize = isHint ? 24 : 28,
			MaxWidth = 1100,
			TextAlignment = HorizontalAlignment.Left,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Padding = isHint ? new Padding(0) : new Padding(40, 30),
		};
		rootPanel.Child = textObj;
	}
}
