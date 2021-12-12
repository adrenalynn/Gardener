using Pipliz;
using Jobs;
using NPC;

namespace Gardener {

	public class GardenerJobSettings: AbstractAreaJobDefinition
	{
		public override IAreaJob CreateAreaJob(Colony owner, Vector3Int min, Vector3Int max, bool isLoaded, int npcID = 0)
		{
			return new GardenerJobInstance(this, owner, min, max, npcID);
		}

		public GardenerJobSettings()
		{
			Identifier = "gardener.gardener";
			UsedNPCType = NPCType.GetByKeyNameOrDefault("gardener");
			MaxGathersPerRun = 10;
		}

		public override IAreaJob CreateAreaJob(Colony owner, JSONNode node)
		{
			int npcid = node.GetAsOrDefault("npc", 0);
			Vector3Int min = new Vector3Int {
				x = node.GetAsOrDefault("x-", int.MinValue),
				y = node.GetAsOrDefault("y-", int.MinValue),
				z = node.GetAsOrDefault("z-", int.MinValue)
			};
			Vector3Int max = new Vector3Int {
				x = node.GetAsOrDefault("xd", 0),
				y = node.GetAsOrDefault("yd", 0),
				z = node.GetAsOrDefault("zd", 0)
			};
			max += min;
			Vector3Int pos = new Vector3Int {
				x = node.GetAsOrDefault("xi", int.MinValue),
				y = node.GetAsOrDefault("yi", int.MinValue),
				z = node.GetAsOrDefault("zi", int.MinValue)
			};
			int type = node.GetAsOrDefault("t", 0);
			int sx = node.GetAsOrDefault("sx", 1);
			int sz = node.GetAsOrDefault("sz", 1);
			bool isDefault = node.GetAsOrDefault("d", true);
			bool autoRemove = node.GetAsOrDefault("a", true);
			return new GardenerJobInstance(this, owner, min, max, npcid, pos, sx, sz, (ushort)type, isDefault, autoRemove);
		}

	}

}

