using Pipliz;
using Pipliz.JSON;
using Jobs;
using NPC;
using System.Collections.Generic;
using TerrainGeneration;
using BlockTypes;

namespace Gardener {

	public class GardenerJobInstance: AbstractAreaJob, IAreaJobSubArguments
	{
		private Vector3Int pos;
		private ItemTypes.ItemType GrassType;
		private bool defaultType;
		private bool autoRemove;
		private int stepx;
		private int height;
		private ItemTypes.ItemType[] yBlocks;

		// constructor
		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, int npcID = 0) : base(definition, owner, min, max, npcID)
		{
			this.pos = Vector3Int.invalidPos;
			this.stepx = 1;
			this.height = max.y - min.y;
			this.yBlocks = new ItemTypes.ItemType[height + 3];
		}

		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, int npcID, Vector3Int subPos, ushort type, bool isDefault = true, bool autoRemove = true, int stepx = 0): base(definition, owner, min, max, npcID)
		{
			this.pos = subPos;
			this.GrassType = ItemTypes.GetType(type);
			this.defaultType = isDefault;
			this.autoRemove = autoRemove;
			this.stepx = stepx;
			this.height = max.y - min.y;
			this.yBlocks = new ItemTypes.ItemType[height + 3];
		}

		// iterate over all reachable blocks of the area
		public override void CalculateSubPosition()
		{
			// this happens only once at creation of the job
			if (pos == Vector3Int.invalidPos) {
				pos = positionMin;
				positionSub = pos;
				return;
			}

			// after save/load it is undefined if the npc already worked on the current tile
			// re-work the same position to ensure it
			if (stepx == 0) {
				positionSub = pos;
				// direction of the steps can be calculated by even | odd
				int z = pos.z - positionMin.z;
				if (z % 2 != 0 || z == 1) {
					stepx = -1;
				} else {
					stepx = 1;
				}
			}

			bool validPos = false;
			while (!validPos) {
				if ( (stepx == 1 && pos.x < positionMax.x) || (stepx == -1 && pos.x > positionMin.x)) {
					pos.x += stepx;
				} else {
					if (pos.z < positionMax.z) {
						pos.z++;
						if (stepx == 1) {
							pos.x = positionMax.x;
							stepx = -1;
						} else {
							pos.x = positionMin.x;
							stepx = 1;
						}
					} else {
						pos = positionMin;
						if (autoRemove) {
							AreaJobTracker.RemoveJob(this);
						}
					}
				}
				if (!World.TryGetColumn(new Vector3Int(pos.x, positionMin.y - 1, pos.z), height + 3, yBlocks, 0)) {
					break;
				}
				int y = 0;
				while (yBlocks[y].IsSolid && y < height + 2) {
					y++;
				}
				if (y >= height + 2) {
					continue;
				}
				if (yBlocks[y - 1].IsFertile && !yBlocks[y].BlocksPathing && !yBlocks[y + 1].BlocksPathing) {
					validPos = true;
					pos.y = positionMin.y + y - 1;
				}
			}
			positionSub = pos;
		}
		
		// set the custom options (grass type | auto remove)
		public void SetArgument(Pipliz.JSON.JSONNode args)
		{
			int i;
			args.TryGetAsOrDefault("grassType", out i, 0);
			// type 0 is for 'biome default', means using the top block
			if (i > 0) {
				GrassType = Gardener.grassTypes[i - 1];
			} else {
				defaultType = true;
			}
			args.TryGetAsOrDefault("autoRemove", out autoRemove, true);
		}

		public override void OnNPCAtJob(ref NPCBase.NPCState state)
		{

			Vector3Int blockPos = positionSub;
			blockPos.y--;

			ItemTypes.ItemType block;
			if (!World.TryGetTypeAt(blockPos, out block)) {
				state.SetCooldown((double)Random.NextFloat(3f, 6f));
				return;
			}

			// for 'biome default' grass calculate it for every sub position
			// since the area can span multiple biomes
			if (defaultType) {
				TerrainGenerator gen = (TerrainGenerator)ServerManager.TerrainGenerator;
				GrassType = ItemTypes.GetType(gen.QueryData(blockPos.x, blockPos.z).Biome.TopBlockType);
			}

			// block is already grass: farm it
			if (block.ItemIndex == GrassType.ItemIndex && GrassType.IsFertile) {
				List<ItemTypes.ItemTypeDrops> GatherResults = new List<ItemTypes.ItemTypeDrops>();
				GatherResults.Add(new ItemTypes.ItemTypeDrops(GrassType.ItemIndex));
				ModLoader.Callbacks.OnNPCGathered.Invoke(this, blockPos, GatherResults);
				NPC.Inventory.Add(GatherResults);
				GatheredItemsCount++;
				if (GatheredItemsCount >= definition.MaxGathersPerRun) {
					shouldDumpInventory = true;
					GatheredItemsCount = 0;
				}
			// otherwise try to convert to grass but prevent converting to sand or snow
			} else {
				if (block.IsFertile && GrassType.IsFertile) {
					ServerManager.TryChangeBlock(blockPos, GrassType, Owner, ESetBlockFlags.DefaultAudio);
				}
			}
			positionSub = Vector3Int.invalidPos;
			state.JobIsDone = true;
			state.SetCooldown(1.0);
		}

		public override void SaveAreaJob(JSONNode colonyRootNode)
		{
			if (!colonyRootNode.TryGetChild(definition.Identifier, out JSONNode node)) {
				node = new JSONNode(NodeType.Array);
				colonyRootNode[definition.Identifier] = node;
			}
			node.AddToArray(new JSONNode(NodeType.Object)
				.SetAs("npc", (NPC != null) ? NPC.ID : 0)
				.SetAs("x-", positionMin.x)
				.SetAs("y-", positionMin.y)
				.SetAs("z-", positionMin.z)
				.SetAs("xd", positionMax.x - positionMin.x)
				.SetAs("yd", positionMax.y - positionMin.y)
				.SetAs("zd", positionMax.z - positionMin.z)
				.SetAs("xi", pos.x)
				.SetAs("yi", pos.y)
				.SetAs("zi", pos.z)
				.SetAs("t", GrassType.ItemIndex)
				.SetAs("d", defaultType)
				.SetAs("a", autoRemove)
			);
		}

	}

}

