using Newtonsoft.Json.Linq;
using Pipliz;
using Jobs;
using NPC;

namespace Gardener {

	public class GardenerJobSettings: AbstractAreaJobDefinition
	{
		public override IAreaJob CreateAreaJob(Colony owner, Vector3Int min, Vector3Int max)
		{
			return new GardenerJobInstance(this, owner, min, max, null);
		}

		public override IAreaJob LoadAreaJob(Colony owner, Vector3Int min, Vector3Int max, NPCID? npcID, JObject miscData)
		{
			return new GardenerJobInstance(this, owner, min, max, npcID, miscData);
		}

		public override int MaxGathersPerRun { get; set; }

		public GardenerJobSettings()
		{
			this.Identifier = "gardener.gardener";
			this.UsedNPCType = NPCType.GetByKeyNameOrDefault("gardener");
			this.MaxGathersPerRun = 10;
		}

	}

}

