using HarmonyLib;
using MagicUI.Core;
using MagicUI.Elements;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UEUI = UnityEngine.UI;
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
		currentInputFieldIndex = 0;
		base.Show();
		Prefill();
		Plugin.Instance.StartCoroutine(SelectFirstTextInput());
		Plugin.Instance.OnUpdate += CheckForInputs;
	}

	public override void Hide()
	{
		base.Hide();
		Plugin.Instance.OnUpdate -= CheckForInputs;
	}

	protected override void Create()
	{
		base.Create();

		saveMenu = TitleScreen.instance.saveMenu;
		StackLayout mainStack = new StackLayout(layoutRoot, "MainStack")
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Padding(60),
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
		portInput = CreateNumericalInput("PortInput", 200, "Port (eg. 38281)");
		portInput.GameObject.GetComponent<UEUI.InputField>().characterLimit = 5; // Ports are max 5 numbers
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
		if (connectioninfo.Port == 0)
		{
			portInput.Text = "";
		}
		else
		{
			portInput.Text = connectioninfo.Port.ToString();
		}
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

	private TextInput CreateNumericalInput(string name, int minWidth, string placeholder)
	{
		return new TextInput(layoutRoot, name)
		{
			MinWidth = minWidth,
			FontSize = 50,
			Placeholder = placeholder,
			Padding = new Padding(25),
			ContentType = UEUI.InputField.ContentType.IntegerNumber
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

	public void CheckForInputs()
	{
		while (IsShowing)
		{
			GameObject selected = EventSystem.current.currentSelectedGameObject;

			// If not in text input field
			if (selected == null || selected.GetComponent<UnityEngine.UI.InputField>() == null)
			{
				return;
			}

			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				//If enter/return is pressed, connect
				ClickedConnect(null);
				return;
			}
			else if (Input.inputString.Length > 0 || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.CapsLock) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Q))
			{
				// Filter out ASCII symbols, including backspace, from proc'ing the controller input support
				// Filter out shift keys and caps lock to allow capital letters
				// Filter out control keys and specific letters, because CRTL + those letters does not produce an inputString because they are keyboard shortcuts
				// (While shift/caps/control would not trigger the Buttons code below under default keybinding, assume that the player may have rebound their keyboard controls)
				// If someone has remapped both their copy/paste/etc. shortcuts AND their Death's Door controls so that they clash but aren't those listed letters, they will run into issues
				Input.ResetInputAxes(); //required to avoid below controller handling from triggering
				Buttons.inputPaused = true;
				return;
			}
			Buttons.inputPaused = false;

			// Should only reach this section from Controller inputs or unfiltered keyboard inputs
			if (Buttons.Tapped("MenuOk")) // Enter and Return are now handled above
			{
				ClickedConnect(null);
			}
			else if (Input.GetKeyDown(KeyCode.Tab) || Buttons.Tapped("MenuRight"))
			{
				currentInputFieldIndex = (currentInputFieldIndex + 1) % tabbableInputs.Length;
				tabbableInputs[currentInputFieldIndex].SelectAndActivate();
			}
			else if (Buttons.Tapped("MenuLeft"))
			{
				currentInputFieldIndex = (currentInputFieldIndex - 1) % tabbableInputs.Length;
				if (currentInputFieldIndex < 0)
				{
					currentInputFieldIndex += tabbableInputs.Length;
				}
				tabbableInputs[currentInputFieldIndex].SelectAndActivate();
			}
			else if (Input.GetKeyDown(KeyCode.Escape) || Buttons.Tapped("MenuBack"))
			{
				ClickedBack(null);
			}

			return;
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
		urlInput.SelectAndActivate();
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
			GameSave.currentSave = __instance.saveSlots[__instance.index].saveFile;
			if (AGM.AlternativeGameModes.SelectedModeName == "ARCHIPELAGO" || (AGM.AlternativeGameModes.SelectedModeName == "START" && GameSave.currentSave.IsKeyUnlocked("ArchipelagoRandomizer")))
			{
				UIManager.Instance.ShowConnectionMenu();
				return false;
			}
			return true;
		}
	}
}