using Pipliz;
using Shared;
using Jobs;
using Chatting;
using NetworkUI;
using NetworkUI.AreaJobs;
using NetworkUI.Items;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ModLoaderInterfaces;

namespace Gardener {

	public class ModInterfaces: IAfterItemTypesDefined
	{
		public void AfterItemTypesDefined()
		{
			Gardener.AfterItemTypesDefined();
		}
	}


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
		public static void AfterItemTypesDefined()
		{
			GardenerJobSettings vGardenerJobSettings = new GardenerJobSettings();
			AreaJobTracker.RegisterAreaJobDefinition(vGardenerJobSettings);

			grassTypes = new List<ItemTypes.ItemType>();
			grassTypes.Add(ItemTypes.GetType("grassdry"));
			grassTypes.Add(ItemTypes.GetType("grassfen"));
			grassTypes.Add(ItemTypes.GetType("grassheath"));
			grassTypes.Add(ItemTypes.GetType("grassheathpurple"));
			grassTypes.Add(ItemTypes.GetType("grassmax"));
			grassTypes.Add(ItemTypes.GetType("grassmid"));
			grassTypes.Add(ItemTypes.GetType("grasswet"));

			PlayerJobSettings = new Dictionary<Players.Player, GardenerSettings>();
			GardenerItem = ItemTypes.GetType("gardener.gardenhoe");
			CommandTool = new GardenerCommandTool();
			Log.Write($"Gardener job activated successfully");
		}

		// start area select tool to create a job
		public static void StartAreaSelectTool(Players.Player player)
		{
			GardenerSettings playerSettings = PlayerJobSettings[player];
			JObject data = new JObject();
			data.Add("grassType", playerSettings.grassType);
			data.Add("autoRemove", playerSettings.autoRemove);

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

