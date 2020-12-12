using Pipliz;
using Pipliz.JSON;
using Jobs;
using NPC;
using System.Collections.Generic;
using TerrainGeneration;
using BlockTypes;

namespace Gardener {

	public enum E_STEP: byte
	{
		Xfirst,
		Zfirst
	}

	public class GardenerJobInstance: AbstractAreaJob, IAreaJobSubArguments
	{
		private Vector3Int pos;
		private ItemTypes.ItemType GrassType;
		private bool defaultType;
		private bool autoRemove;
		private bool loopedAround;
		private int stepx, stepz, height;
		private E_STEP innerStep;
		private ItemTypes.ItemType[] yBlocks;

		// constructor (from CommandTool/Menu, setting will be applied with callback)
		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, int npcID = 0) : base(definition, owner, min, max, npcID)
		{
			this.pos = Vector3Int.invalidPos;
			this.loopedAround = false;
			this.height = max.y - min.y + 1;
			this.yBlocks = new ItemTypes.ItemType[height + 3];

			// have NPC walk along the longer axis
			if (positionMax.x - positionMin.x > positionMax.z - positionMin.z) {
				innerStep = E_STEP.Xfirst;
			} else {
				innerStep = E_STEP.Zfirst;
			}
		}

		// constructor (from savegame)
		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, int npcID, Vector3Int npcPos, int sx, int sz, ushort type, bool isDefault = true, bool autoRemove = true): base(definition, owner, min, max, npcID)
		{
			this.pos = npcPos;
			this.GrassType = ItemTypes.GetType(type);
			this.defaultType = isDefault;
			this.autoRemove = autoRemove;
			this.height = max.y - min.y + 1;
			this.yBlocks = new ItemTypes.ItemType[height + 3];
			this.stepx = sx;
			this.stepz = sz;
		}
		
		// set the custom options (grass type | auto remove)
		public void SetArgument(Pipliz.JSON.JSONNode args)
		{
			int i;
			args.TryGetAsOrDefault("grassType", out i, 0);
			// type 0 is for 'biome default', using the top block
			if (i > 0) {
				GrassType = Gardener.grassTypes[i - 1];
			} else {
				defaultType = true;
			}
			args.TryGetAsOrDefault("autoRemove", out autoRemove, true);
		}

		// Define a starting position and calculate stepping
		public void CalculateStart()
		{

			// this should normally never happen
			if (NPC == null) {
				Log.Write("Gardener Job: CalculateStart() called without NPC");
				pos = positionMin;
				stepx = 1;
				stepz = 1;
				return;
			}
			// base step direction on current NPC position
			if (System.Math.Abs(NPC.Position.x - positionMin.x) < System.Math.Abs(NPC.Position.x - positionMax.x)) {
				stepx = 1;
				pos.x = positionMin.x;
			} else {
				stepx = -1;
				pos.x = positionMax.x;
			}
			if (System.Math.Abs(NPC.Position.z - positionMin.z) < System.Math.Abs(NPC.Position.z - positionMax.z)) {
				stepz = 1;
				pos.z = positionMin.z;
			} else {
				stepx = -1;
				pos.z = positionMax.z;
			}
			return;
		}

		// iterate over the x z area (endless loop, sets flag on wrap around)
		public void IterateToNextPos()
		{
			// step X first
			if (innerStep == E_STEP.Xfirst) {
				if ((stepx > 0 && pos.x < positionMax.x) || (stepx < 0 && pos.x > positionMin.x)) {
					pos.x += stepx;
				} else {
					if (stepx > 0) {
						//pos.x = positionMin.x;
						stepx = -1;
					} else {
						//pos.x = positionMax.x;
						stepx = 1;
					}
					// step Z second
					if ((stepz > 0 && pos.z < positionMax.z) || (stepz < 0 && pos.z > positionMin.z)) {
						pos.z += stepz;
					} else {
						if (stepz > 0) {
							pos.z = positionMin.z;
						} else {
							pos.z = positionMax.z;
						}
						loopedAround = true;
					}
				}
			// step Z first
			} else {
				if ((stepz > 0 && pos.z < positionMax.z) || (stepz < 0 && pos.z > positionMin.z)) {
					pos.z += stepz;
				} else {
					if (stepz > 0) {
						//pos.z = positionMin.z;
						stepz = -1;
					} else {
						//pos.z = positionMax.z;
						stepz = 1;
					}
					// step X second
					if ((stepx > 0 && pos.x < positionMax.x) || (stepx < 0 && pos.x > positionMin.x)) {
						pos.x += stepx;
					} else {
						if (stepx > 0) {
							pos.x = positionMin.x;
						} else {
							pos.x = positionMax.x;
						}
						loopedAround = true;
					}
				}
			}
			return;
		}

		// calculate next spot to work on
		public override void CalculateSubPosition()
		{
			// this happens at creation of the job or after loading
			if (stepx == 0 && stepz == 0) {
				CalculateStart();
			} else {
				IterateToNextPos();
			}

			bool workablePos = IsWorkableBlock();
			while (!workablePos && !loopedAround) {
				IterateToNextPos();
			}

			if (loopedAround) {
				if (autoRemove) {
					AreaJobTracker.RemoveJob(this);
					return;
				}
				loopedAround = false;
			}

			if (workablePos) {
				positionSub = pos;
			}
		}

		// check for a solid fertile block
		private bool IsWorkableBlock()
		{
			if (!World.TryGetColumn(new Vector3Int(pos.x, positionMin.y - 1, pos.z), height + 3, yBlocks, 0)) {
				return false;
			}

			int y = 0;
			while (yBlocks[y].IsSolid && y < height + 2) {
				y++;
			}
			if (y >= height + 2) {
				return false;
			}

			if (yBlocks[y - 1].IsFertile && !yBlocks[y].BlocksPathing && !yBlocks[y + 1].BlocksPathing) {
				pos.y = positionMin.y + y - 1;
				return true;
			}
			return false;
		}

		// have the NPC work one tile
		public override void OnNPCAtJob(ref NPCBase.NPCState state)
		{
			Vector3Int blockPos = positionSub;
			blockPos.y--;

			ItemTypes.ItemType block;
			ItemTypes.ItemType blockAbove;
			if (!World.TryGetTypeAt(blockPos, out block) || !World.TryGetTypeAt(positionSub, out blockAbove)) {
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
				if (GatheredItemsCount >= Definition.MaxGathersPerRun) {
					shouldDumpInventory = true;
					GatheredItemsCount = 0;
				}
			// otherwise try to convert to grass but prevent converting to sand or snow
			} else {
				if (block.IsFertile && GrassType.IsFertile) {
					ServerManager.TryChangeBlock(blockPos, GrassType, Owner, ESetBlockFlags.DefaultAudio);

					// remove old crops from on top the block
					if (blockAbove.NeedsBase) {
						ServerManager.TryChangeBlock(positionSub, BlockTypes.BuiltinBlocks.Types.air, Owner, ESetBlockFlags.DefaultAudio);
					}
				}
			}
			positionSub = Vector3Int.invalidPos;
			state.JobIsDone = true;
			state.SetCooldown(1.0);
		}

		public override void SaveAreaJob(JSONNode areasRootNode)
		{
			if (!IsValid) {
				return;
			}

			if (!areasRootNode.TryGetChild(Definition.Identifier, out JSONNode node)) {
				node = new JSONNode(NodeType.Array);
				areasRootNode[Definition.Identifier] = node;
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
				.SetAs("sx", stepx)
				.SetAs("sz", stepz)
				.SetAs("t", GrassType.ItemIndex)
				.SetAs("d", defaultType)
				.SetAs("a", autoRemove)
			);
		}

	}

}

