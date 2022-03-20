using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Pipliz;
using Jobs;
using NPC;
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
		private bool firstCheckAfterSaveLoad;
		private int stepx, stepz, height;
		private E_STEP innerStep;
		private ItemTypes.ItemType[] yBlocks;
		private List<ItemTypes.ItemTypeDrops> GatherResults;

		// constructor (from CommandTool/Menu, settings will be applied with callback)
		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, NPCID? npcID) : base(definition, owner, min, max, npcID)
		{
			this.pos = Vector3Int.invalidPos;
			this.loopedAround = false;
			this.firstCheckAfterSaveLoad = false;
			this.height = max.y - min.y + 1;
			this.yBlocks = new ItemTypes.ItemType[height + 3];
			this.GatherResults = new List<ItemTypes.ItemTypeDrops>();
			SetStepDirection();
		}

		// constructor (from savegame)
		public GardenerJobInstance(IAreaJobDefinition definition, Colony owner, Vector3Int min, Vector3Int max, NPCID? npcID, JObject miscData): base(definition, owner, min, max, npcID)
		{
			this.pos = miscData.GetValue("workPos").ToObject<Vector3Int>();
			this.loopedAround = false;
			this.height = max.y - min.y + 1;
			this.yBlocks = new ItemTypes.ItemType[height + 3];
			this.GatherResults = new List<ItemTypes.ItemTypeDrops>();
			this.GrassType = ItemTypes.GetType((ushort)miscData.GetValue("type"));
			this.defaultType = (bool)miscData.GetValue("isDefault");
			this.autoRemove = (bool)miscData.GetValue("autoRemove");
			this.stepx = (int)miscData.GetValue("sx");
			this.stepz = (int)miscData.GetValue("sz");
			SetStepDirection();

			// flag to force work the current block
			firstCheckAfterSaveLoad = true;
		}

		public void SetStepDirection()
		{
			// have NPC walk along the longer axis
			if (System.Math.Abs(positionMax.x - positionMin.x) > System.Math.Abs(positionMax.z - positionMin.z)) {
				this.innerStep = E_STEP.Xfirst;
			} else {
				this.innerStep = E_STEP.Zfirst;
			}
		}

		// set the custom options (grass type | auto remove)
		public void SetArgument(JObject args)
		{
			int i = (int)args.GetValue("grassType");
			// type 0 is for 'biome default', using the top block
			if (i > 0) {
				GrassType = Gardener.grassTypes[i - 1];
			} else {
				defaultType = true;
			}
			autoRemove = (bool)args.GetValue("autoRemove");
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
				stepz = -1;
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
						stepx = -1;
					} else {
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
						stepz = -1;
					} else {
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
			// this happens at creation of the job
			if (stepx == 0 && stepz == 0) {
				CalculateStart();
			} else {
				if (!firstCheckAfterSaveLoad) {
					IterateToNextPos();
				} else {
					firstCheckAfterSaveLoad = false;
				}
			}

			bool workablePos = IsWorkableBlock();
			while (!workablePos && !loopedAround) {
				IterateToNextPos();
				workablePos = IsWorkableBlock();
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
				// in convert mode skip blocks that already are the target type
				if (defaultType) {
					TerrainGenerator gen = (TerrainGenerator)ServerManager.TerrainGenerator;
					GrassType = ItemTypes.GetType(gen.QueryData(pos.x, pos.z).Biome.TopBlockType);
				}
				if (autoRemove && yBlocks[y - 1].ItemIndex == GrassType.ItemIndex) {
					return false;
				}
				pos.y = positionMin.y + y - 1;
				return true;
			}
			return false;
		}

		// have the NPC work one tile
		public override void OnNPCAtJob(ref NPCBase.NPCState state)
		{
			GardenerJobSettings settings = (GardenerJobSettings)this.Definition;
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
				if (GatheredItemsCount >= settings.MaxGathersPerRun) {
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
						GatherResults.Clear();
						List<ItemTypes.ItemTypeDrops> onRemoveItems = blockAbove.OnRemoveItems;
						for (int i = 0; i < onRemoveItems.Count; i++) {
							GatherResults.Add(onRemoveItems[i]);
						}
						GatheredItemsCount++;
						if (GatheredItemsCount >= settings.MaxGathersPerRun) {
							shouldDumpInventory = true;
							GatheredItemsCount = 0;
						}
						ModLoader.Callbacks.OnNPCGathered.Invoke(this, positionSub, GatherResults);
						NPC.Inventory.Add(GatherResults);
					}
				}
			}
			positionSub = Vector3Int.invalidPos;
			state.JobIsDone = true;
			state.SetCooldown(1.0);
		}

		// custom data for saving
		public override JToken GetMiscSaveData()
		{
			JObject miscData = new JObject();
			miscData.Add("workPos", JToken.FromObject(this.pos));
			miscData.Add("sx", this.stepx);
			miscData.Add("sz", this.stepz);
			miscData.Add("type", this.GrassType.ItemIndex);
			miscData.Add("isDefault", this.defaultType);
			miscData.Add("autoRemove", this.autoRemove);

			return miscData;
		}

	}

}

