using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public sealed class HarvestYieldResolver
	{
		private readonly HarvestYieldTable _table;

		public HarvestYieldResolver(HarvestYieldTable table)
		{
			_table = table ?? throw new ArgumentNullException("table");
		}

		public HarvestOutcome Resolve(ResourceNodeDefinition node, PlayerInventory inventory)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}
			string family = ((node.OutputItem != null) ? node.OutputItem.MaterialFamily : string.Empty);
			int resourceTier = ((node.OutputItem != null) ? node.OutputItem.Tier : 0);
			ItemDefinition itemDefinition = FindBestCarriedTool(family, inventory);
			int? toolTier = ((itemDefinition != null) ? new int?(itemDefinition.Tier) : ((int?)null));
			if (!HarvestProgressionRules.CanHarvest(resourceTier, toolTier))
			{
				ItemDefinition missingTool = (node.RequiresTool ? FindTool(node.RequiredToolItemId) : FindToolForFamily(family));
				return HarvestOutcome.Blocked(missingTool);
			}
			int amount = HarvestProgressionRules.Yield(resourceTier, toolTier);
			ToolStrain strain = HarvestProgressionRules.Strain(resourceTier, toolTier);
			return (itemDefinition != null) ? HarvestOutcome.WithTool(amount, itemDefinition, strain) : HarvestOutcome.ByHand(amount, strain);
		}

		public ItemDefinition FindTool(string toolItemId)
		{
			HarvestToolYield entry;
			return _table.TryGetToolById(toolItemId, out entry) ? entry.Tool : null;
		}

		private ItemDefinition FindToolForFamily(string family)
		{
			HarvestToolYield entry;
			return _table.TryGetToolForFamily(family, out entry) ? entry.Tool : null;
		}

		private ItemDefinition FindBestCarriedTool(string family, PlayerInventory inventory)
		{
			ItemDefinition itemDefinition = null;
			foreach (HarvestToolYield toolYield in _table.ToolYields)
			{
				ItemDefinition tool = toolYield.Tool;
				if (!(tool == null) && toolYield.Serves(family) && Carries(inventory, tool.Id) && (itemDefinition == null || tool.Tier > itemDefinition.Tier || (tool.Tier == itemDefinition.Tier && string.CompareOrdinal(tool.Id, itemDefinition.Id) < 0)))
				{
					itemDefinition = tool;
				}
			}
			return itemDefinition;
		}

		private static bool Carries(PlayerInventory inventory, string toolItemId)
		{
			return inventory != null && !string.IsNullOrWhiteSpace(toolItemId) && inventory.Contains(toolItemId);
		}
	}

	public readonly struct HarvestOutcome
	{
		public int Amount { get; }

		public ItemDefinition Tool { get; }

		public ItemDefinition MissingTool { get; }

		public ToolStrain Strain { get; }

		public bool IsAllowed => MissingTool == null && Amount > 0;

		private HarvestOutcome(int amount, ItemDefinition tool, ItemDefinition missingTool, ToolStrain strain)
		{
			Amount = amount;
			Tool = tool;
			MissingTool = missingTool;
			Strain = strain;
		}

		public static HarvestOutcome ByHand(int amount, ToolStrain strain)
		{
			return new HarvestOutcome(amount, null, null, strain);
		}

		public static HarvestOutcome WithTool(int amount, ItemDefinition tool, ToolStrain strain)
		{
			return new HarvestOutcome(amount, tool, null, strain);
		}

		public static HarvestOutcome Blocked(ItemDefinition missingTool)
		{
			return new HarvestOutcome(0, null, missingTool, ToolStrain.Insufficient);
		}
	}
}
