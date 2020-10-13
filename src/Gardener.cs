using Pipliz;
using Shared;
using Jobs;
using Chatting;
using NetworkUI;
using NetworkUI.AreaJobs;
using NetworkUI.Items;
using Pipliz.JSON;
using System.Collections.Generic;

namespace Gardener {

	[ModLoader.ModManager]
	public static class Gardener
	{
		public struct GardenerSettings {
			public int grassType, autoRemove;

			public GardenerSettings(int g, int a)
			{
				this.grassType = g;
				this.autoRemove = a;
			}
		}

		public const string NAMESPACE = "Gardener";
		public static ItemTypes.ItemType GardenerItem;
		public static List<ItemTypes.ItemType> grassTypes;
		public static Dictionary<Players.Player, GardenerSettings> PlayerJobSettings;

		// Initialize the mod
		[ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, NAMESPACE + ".RegisterJob")]
		[ModLoader.ModCallbackProvidesFor("createareajobdefinitions")]
		public static void AfterItemTypesDefined()
		{
			GardenerJobSettings vGardenerJobSettings = new GardenerJobSettings();
			AreaJobTracker.RegisterAreaJobDefinition(vGardenerJobSettings);

			grassTypes = new List<ItemTypes.ItemType>();
			grassTypes.Add(ItemTypes.GetType("grasscolddry"));
			grassTypes.Add(ItemTypes.GetType("grasscoldmid"));
			grassTypes.Add(ItemTypes.GetType("grasscoldwet"));
			grassTypes.Add(ItemTypes.GetType("grasshotdriest"));
			grassTypes.Add(ItemTypes.GetType("grasshotdry"));
			grassTypes.Add(ItemTypes.GetType("grasshotmid"));
			grassTypes.Add(ItemTypes.GetType("grasshotwet"));
			grassTypes.Add(ItemTypes.GetType("grasstaigadry"));
			grassTypes.Add(ItemTypes.GetType("grasstaigawet"));
			grassTypes.Add(ItemTypes.GetType("grasstropicdriest"));
			grassTypes.Add(ItemTypes.GetType("grasstropicdry"));
			grassTypes.Add(ItemTypes.GetType("grasstropicmid"));
			grassTypes.Add(ItemTypes.GetType("grasstropicwet"));
			grassTypes.Add(ItemTypes.GetType("grasstundradry"));
			grassTypes.Add(ItemTypes.GetType("grasstundrawet"));

			PlayerJobSettings = new Dictionary<Players.Player, GardenerSettings>();
			GardenerItem = ItemTypes.GetType("gardener.gardenhoe");
			Log.Write($"Gardener job activated successfully");
		}

		// Check R and L clicks with the garden hoe
		[ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, NAMESPACE + ".OnPlayerClicked")]
		public static void OnPlayerClicked(Players.Player player, PlayerClickedData data)
		{
			if (data.TypeSelected != GardenerItem.ItemIndex || player.ActiveColony == null) {
				return;
			}

			if (data.ClickType == PlayerClickedData.EClickType.Left) {
				SendGardenerJobSelectionUI(player);
			} else {
				if (!PlayerJobSettings.ContainsKey(player)) {
					SendGardenerJobSelectionUI(player);
				} else {
					StartAreaSelectTool(player);
				}
			}
		}

		// Send job selection UI
		public static void SendGardenerJobSelectionUI(Players.Player player)
		{
			GardenerSettings playerSettings;
			if (PlayerJobSettings.ContainsKey(player)) {
				playerSettings = PlayerJobSettings[player];
			} else {
				playerSettings = new GardenerSettings(0, 1);
			}

			List<string> allGrassTypes = new List<string>();
			allGrassTypes.Add(Localization.GetSentence(player.LastKnownLocale, "gardener.defaultGrass"));
			foreach (ItemTypes.ItemType item in grassTypes) {
				allGrassTypes.Add(Localization.GetType(player.LastKnownLocale, item));
			}
			NetworkMenu menu = new NetworkMenu {
				Identifier = "gardener.jobmenu",
				TextColor = UnityEngine.Color.black,
				Height = -1,
				Width = -1,
				ForceClosePopups = true
			};
			menu.LocalStorage.SetAs("header", Localization.GetSentence(player.LastKnownLocale, "gardener.menu.mainheader"));
			menu.LocalStorage.SetAs("grassType", playerSettings.grassType);
			menu.LocalStorage.SetAs("autoRemove", playerSettings.autoRemove);

			Label typeLabel = new Label("gardener.menu.typelabel");
			DropDownNoLabel grassType = new DropDownNoLabel("grassType", allGrassTypes, 30, 300f, 0f, 0f);

			EmptySpace vertSpace = new EmptySpace(20);
			Label autoLabel = new Label("gardener.menu.autoLabel");
			Toggle autoRemove = new Toggle("gardener.menu.autoremove", "autoRemove");
			ButtonCallback okButton = new ButtonCallback("gardener.menu.okbutton", new LabelData("gardener.menu.okbutton"), 40, 25, ButtonCallback.EOnClickActions.ClosePopup);
			menu.Items.Add(typeLabel);
			menu.Items.Add(grassType);
			menu.Items.Add(vertSpace);
			menu.Items.Add(autoLabel);
			menu.Items.Add(autoRemove);
			menu.Items.Add(vertSpace);
			menu.Items.Add(okButton);

			NetworkMenuManager.SendServerPopup(player, menu);
		}

		// Receive job selection UI settings
		[ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerPushedNetworkUIButton, NAMESPACE + ".OnPlayerPushedNetworkUIButton")]
		public static void OnPlayerPushedNetworkUIButton(NetworkUI.ButtonPressCallbackData data)
		{
			if (data.ButtonIdentifier != "gardener.menu.okbutton") {
				return;
			}
			GardenerSettings playerSettings;
			if (PlayerJobSettings.ContainsKey(data.Player)) {
				playerSettings = PlayerJobSettings[data.Player];
			} else {
				playerSettings = new GardenerSettings(0, 1);
			}
			data.Storage.TryGetAs("grassType", out playerSettings.grassType);
			data.Storage.TryGetAs("autoRemove", out playerSettings.autoRemove);
			PlayerJobSettings[data.Player] = playerSettings;
			StartAreaSelectTool(data.Player);
		}

		// start area select tool to create a job
		public static void StartAreaSelectTool(Players.Player player)
		{
			GardenerSettings playerSettings = PlayerJobSettings[player];
			JSONNode data = new JSONNode();
			data.SetAs("grassType", playerSettings.grassType);
			data.SetAs("autoRemove", playerSettings.autoRemove);

			// use regular farm limits as default, autoRemove (=convert dirt) allows larger area
			int count = 100, height = 4;
			if (playerSettings.autoRemove == 1) {
				count = 1000;
				height = 100;
			}

			GenericCommandToolSettings jobData = new GenericCommandToolSettings();
			jobData.TranslationKey = "gardener.job.areatype";
			jobData.Key = "gardener.gardener";
			jobData.NPCTypeKey = "gardener";
			jobData.Minimum3DBlockCount = 1;
			jobData.Maximum3DBlockCount = count * 4;
			jobData.Minimum2DBlockCount = 1;
			jobData.Maximum2DBlockCount = count;
			jobData.MinimumHeight = 1;
			jobData.MaximumHeight = height;
			jobData.OneAreaOnly = true;
			jobData.JSONData = data;

			AreaJobTracker.StartCommandToolSelection(player, jobData);
		}

	}
}

