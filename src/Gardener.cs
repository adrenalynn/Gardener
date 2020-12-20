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
		public static GardenerCommandTool CommandTool;

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
			CommandTool = new GardenerCommandTool();
			Log.Write($"Gardener job activated successfully");
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

