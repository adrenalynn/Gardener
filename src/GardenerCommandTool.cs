using NetworkUI;
using NetworkUI.Items;
using ModLoaderInterfaces;
using Jobs;
using System.Collections.Generic;

namespace Gardener {

	public class GardenerCommandTool : IOnConstructCommandTool, IOnPlayerPushedNetworkUIButton
	{

		// CommandTool (1) menu entry
		public void OnConstructCommandTool(Players.Player player, NetworkMenu menu, string identifier)
		{
			if (identifier != "popup.tooljob.flaxherbfarming") {
				return;
			}
			menu.Items.Add(new EmptySpace(20));
			CommandToolManager.GenerateTwoColumnCenteredRow(menu, CommandToolManager.GetButtonTool(player, "gardener.gardenerjob", "popup.tooljob.gardenerfarm", 200, false));
		}

		// Check for Gardener menu or job selection
		public void OnPlayerPushedNetworkUIButton(NetworkUI.ButtonPressCallbackData data)
		{
			if (data.ButtonIdentifier == "gardener.menu.okbutton") {
				ProcessGardenerMenuResults(data);
			} else if (data.ButtonIdentifier == "gardener.gardenerjob") {
				SendGardenerNetworkMenu(data.Player);
			}
		}

		// Send job selection UI
		public void SendGardenerNetworkMenu(Players.Player player)
		{
			Gardener.GardenerSettings playerSettings;
			if (Gardener.PlayerJobSettings.ContainsKey(player)) {
				playerSettings = Gardener.PlayerJobSettings[player];
			} else {
				playerSettings = new Gardener.GardenerSettings(0, 1);
			}

			List<string> allGrassTypes = new List<string>();
			allGrassTypes.Add(Localization.GetSentence(player.LastKnownLocale, "gardener.defaultGrass"));
			foreach (ItemTypes.ItemType item in Gardener.grassTypes) {
				allGrassTypes.Add(Localization.GetType(player.LastKnownLocale, item));
			}
			NetworkMenu menu = new NetworkMenu {
				Identifier = "gardener.jobmenu",
				TextColor = UnityEngine.Color.black,
				Height = -1,
				Width = -1,
				ForceClosePopups = true
			};
			menu.LocalStorage.Add("header", Localization.GetSentence(player.LastKnownLocale, "gardener.menu.mainheader"));
			menu.LocalStorage.Add("grassType", playerSettings.grassType);
			menu.LocalStorage.Add("autoRemove", playerSettings.autoRemove);

			menu.Items.Add(new Label("gardener.menu.typelabel"));
			menu.Items.Add(new DropDownNoLabel("grassType", allGrassTypes, 30, 300f, 0f, 0f));
			menu.Items.Add(new EmptySpace(20));
			menu.Items.Add(new Label("gardener.menu.autoLabel"));
			menu.Items.Add(new Toggle("gardener.menu.autoremove", "autoRemove"));
			menu.Items.Add(new EmptySpace(20));
			menu.Items.Add(new ButtonCallback("gardener.menu.okbutton", new LabelData("gardener.menu.okbutton"), 60, 35, ButtonCallback.EOnClickActions.ClosePopup));

			NetworkMenuManager.SendServerPopup(player, menu);
		}

		// Extract parameters from menu result to start the selection tool
		public static void ProcessGardenerMenuResults(NetworkUI.ButtonPressCallbackData data)
		{
			Gardener.GardenerSettings playerSettings;
			if (Gardener.PlayerJobSettings.ContainsKey(data.Player)) {
				playerSettings = Gardener.PlayerJobSettings[data.Player];
			} else {
				playerSettings = new Gardener.GardenerSettings(0, 1);
			}
			playerSettings.grassType = (int)data.Storage.GetValue("grassType");
			playerSettings.autoRemove = (int)data.Storage.GetValue("autoRemove");
			Gardener.PlayerJobSettings[data.Player] = playerSettings;
			Gardener.StartAreaSelectTool(data.Player);
		}

	}
}

