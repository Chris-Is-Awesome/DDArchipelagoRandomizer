using HarmonyLib;
using MagicUI.Core;
using MagicUI.Elements;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using AGM = DDoor.AlternativeGameModes;

namespace DDoor.ArchipelagoRandomizer.UI;

internal class ConnectionMenu : CustomUI
{
	private SaveMenu saveMenu;
	private TextInput urlInput;
	private TextInput portInput;
	private TextInput slotNameInput;
	private TextInput passwordInput;
	private TextInput[] tabbableInputs;
	private int currentInputFieldIndex;

	protected override string Name { get; set; } = "ConnectionMenu";
	protected override Padding Padding { get; set; } = new Padding(15);
	protected override float FadeDuration { get; set; } = 0.35f;

	public override void Show()
	{
		currentInputFieldIndex = 1;
		base.Show();
		Prefill();
		Plugin.Instance.StartCoroutine(SelectFirstTextInput());
		Plugin.Instance.StartCoroutine(CheckForInputs());
	}

	public override void Hide()
	{
		base.Hide();
		Plugin.Instance.StopCoroutine(CheckForInputs());
	}

	protected override void Create()
	{
		base.Create();

		saveMenu = TitleScreen.instance.saveMenu;
		StackLayout mainStack = new StackLayout(layoutRoot, "MainStack")
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Padding(30),
		};
		TextObject headingText = new TextObject(layoutRoot, "Heading")
		{
			Text = "Archipelago Connection Settings",
			FontSize = 90,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Padding(15),
		};
		mainStack.Children.Add(headingText);
		urlInput = CreateTextInput("URLInput", 400, "URL (eg. archipelago.gg)");
		portInput = CreateTextInput("PortInput", 200, "Port (eg. 38281)");
		mainStack.Children.Add(CreateStackLayout("URLPortStack", urlInput, portInput));
		slotNameInput = CreateTextInput("SlotNameInput", 200, "Player name");
		passwordInput = CreateTextInput("PasswordInput", 400, "Password");
		mainStack.Children.Add(CreateStackLayout("SlotNamePasswordStack", slotNameInput, passwordInput));
		Button connectBtn = CreateButton("ConnectBtn", "Connect", ClickedConnect);
		Button backBtn = CreateButton("BackBtn", "Back", ClickedBack);
		mainStack.Children.Add(CreateStackLayout("ButtonsStack", connectBtn, backBtn));
		rootPanel.Child = mainStack;

		tabbableInputs = [
			urlInput,
			portInput,
			slotNameInput,
			passwordInput,
		];
	}

	private void Prefill()
	{
		Archipelago.APSaveData connectioninfo = Archipelago.Instance.GetAPSaveData();

		if (connectioninfo == null)
		{
			return;
		}

		urlInput.Text = connectioninfo.URL;
		portInput.Text = connectioninfo.Port.ToString();
		slotNameInput.Text = connectioninfo.SlotName;
		passwordInput.Text = connectioninfo.Password;
	}

	private void ClickedConnect(Button _)
	{
		string url = urlInput.Text;
		string slotName = slotNameInput.Text;
		string password = passwordInput.Text;

		if (!int.TryParse(portInput.Text, out int port) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(slotName))
		{
			UIManager.Instance.ShowNotification("You must specify a URL, player name, and a port number!");
			return;
		}

		Archipelago.APSaveData connectionInfo = Archipelago.Instance.GetAPSaveData();
		connectionInfo.URL = url;
		connectionInfo.Port = port;
		connectionInfo.SlotName = slotName;
		connectionInfo.Password = password;

		ArchipelagoRandomizerMod.Instance.EnableMod(connectionInfo);
	}

	private void ClickedBack(Button _)
	{
		Hide();
		Plugin.Instance.StartCoroutine(ReturnToSaveMenu());
	}

	private StackLayout CreateStackLayout(string name, params ArrangableElement[] children)
	{
		StackLayout newStackLayout = new StackLayout(layoutRoot, name)
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		for (int i = 0; i < children.Length; i++)
		{
			newStackLayout.Children.Add(children[i]);
		}

		return newStackLayout;
	}

	private TextInput CreateTextInput(string name, int minWidth, string placeholder)
	{
		return new TextInput(layoutRoot, name)
		{
			MinWidth = minWidth,
			FontSize = 50,
			Placeholder = placeholder,
			Padding = new Padding(25)
		};
	}

	private Button CreateButton(string name, string text, System.Action<Button> onClickedCallback)
	{
		Button newButton = new Button(layoutRoot, name)
		{
			Content = text,
			FontSize = 70,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Padding = new Padding(15, 45)
		};
		newButton.Click += onClickedCallback;

		return newButton;
	}

	private IEnumerator CheckForInputs()
	{
		while (IsShowing)
		{
			GameObject selected = EventSystem.current.currentSelectedGameObject;

			// If not in text input field
			if (selected == null || selected.GetComponent<UnityEngine.UI.InputField>() == null)
			{
				yield return null;
			}

			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				ClickedConnect(null);
			}
			else if (Input.GetKeyDown(KeyCode.Tab))
			{
				currentInputFieldIndex = (currentInputFieldIndex + 1) % tabbableInputs.Length;
				tabbableInputs[currentInputFieldIndex].SelectAndActivate();
			}

			yield return null;
		}
	}

	private IEnumerator ReturnToSaveMenu()
	{
		// Wait for fade to finish
		while (IsFading)
		{
			yield return null;
		}

		saveMenu.GainFocus(true);
		// Nullify transitionButton to ensure we return to title screen on back input
		saveMenu.transitionButton = null;
		saveMenu.openSubMenu();
	}

	private IEnumerator SelectFirstTextInput()
	{
		// Wait for end of frame so we can select it after the others have been created
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		portInput.SelectAndActivate();
	}

	[HarmonyPatch]
	private class Patches()
	{
		/// <summary>
		/// Shows connection menu when selecting Archipelago mode
		/// </summary>
		[HarmonyPrefix, HarmonyPatch(typeof(SaveMenu), nameof(SaveMenu.startGame))]
		private static bool StartGamePatch(SaveMenu __instance)
		{
			if (AGM.AlternativeGameModes.SelectedModeName == "ARCHIPELAGO")
			{
				UIManager.Instance.ShowConnectionMenu();
				return false;
			}

			return true;
		}
	}
}