using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class ContentDatabase : MonoBehaviour
	{
		private const string DefaultBuildingCatalogResourcePath = "Data/BuildingCatalog_V01";

		[SerializeField]
		private BuildingCatalogDefinition buildingCatalog;

		[SerializeField]
		private BuildingCostDefinition[] buildings = Array.Empty<BuildingCostDefinition>();

		private readonly Dictionary<string, BuildingCostDefinition> _buildingsById = new Dictionary<string, BuildingCostDefinition>(StringComparer.Ordinal);

		private const string DefaultCaptureRulesResourcePath = "Data/CaptureRules_V01";

		[SerializeField]
		private CaptureRulesDefinition captureRules;

		private const string DefaultItemCatalogResourcePath = "Data/ItemCatalog_V01";

		private const string DefaultWeaponCatalogResourcePath = "Data/WeaponCatalog_V02";

		private const string DefaultEidraCatalogResourcePath = "Data/EidraCatalog_V01";

		private const string DefaultResourceCatalogResourcePath = "Data/ResourceNodeCatalog_V01";

		private const string DefaultCraftingCatalogResourcePath = "Data/CraftingRecipeCatalog_V01";

		[SerializeField]
		private WeaponData[] weapons = Array.Empty<WeaponData>();

		[SerializeField]
		private WeaponCatalogDefinition weaponCatalog;

		[SerializeField]
		private EidraCatalogDefinition eidraCatalog;

		[SerializeField]
		private EidraData[] eidren = Array.Empty<EidraData>();

		[SerializeField]
		private AbilityData[] abilities = Array.Empty<AbilityData>();

		[SerializeField]
		private BossData[] bosses = Array.Empty<BossData>();

		[SerializeField]
		private WorldMapDefinition[] worldMaps = Array.Empty<WorldMapDefinition>();

		[SerializeField]
		private ItemCatalogDefinition itemCatalog;

		[SerializeField]
		private ItemDefinition[] items = Array.Empty<ItemDefinition>();

		[SerializeField]
		private ResourceNodeCatalogDefinition resourceNodeCatalog;

		[SerializeField]
		private ResourceNodeDefinition[] resourceNodes = Array.Empty<ResourceNodeDefinition>();

		[SerializeField]
		private CraftingRecipeCatalogDefinition craftingRecipeCatalog;

		[SerializeField]
		private CraftingRecipeDefinition[] craftingRecipes = Array.Empty<CraftingRecipeDefinition>();

		private readonly Dictionary<string, WeaponData> _weaponsById = new Dictionary<string, WeaponData>(StringComparer.Ordinal);

		private readonly Dictionary<string, EidraData> _eidrenById = new Dictionary<string, EidraData>(StringComparer.Ordinal);

		private readonly Dictionary<string, AbilityData> _abilitiesById = new Dictionary<string, AbilityData>(StringComparer.Ordinal);

		private readonly Dictionary<string, BossData> _bossesById = new Dictionary<string, BossData>(StringComparer.Ordinal);

		private readonly Dictionary<string, WorldMapDefinition> _worldMapsById = new Dictionary<string, WorldMapDefinition>(StringComparer.Ordinal);

		private readonly Dictionary<string, ItemDefinition> _itemsById = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

		private readonly Dictionary<string, ResourceNodeDefinition> _resourceNodesById = new Dictionary<string, ResourceNodeDefinition>(StringComparer.Ordinal);

		private readonly Dictionary<string, CraftingRecipeDefinition> _craftingRecipesById = new Dictionary<string, CraftingRecipeDefinition>(StringComparer.Ordinal);

		public void ConfigureBuildings(BuildingCostDefinition[] buildingContent)
		{
			buildings = buildingContent ?? Array.Empty<BuildingCostDefinition>();
			Rebuild(buildings, _buildingsById, (BuildingCostDefinition value) => value.Id, "BuildingCostDefinition");
		}

		public bool TryGetBuilding(string id, out BuildingCostDefinition value)
		{
			EnsureBuildingsLoaded();
			if (string.IsNullOrWhiteSpace(id))
			{
				value = null;
				return false;
			}
			return _buildingsById.TryGetValue(id, out value);
		}

		public BuildingCostDefinition[] GetBuildings()
		{
			EnsureBuildingsLoaded();
			BuildingCostDefinition[] array = new BuildingCostDefinition[_buildingsById.Count];
			_buildingsById.Values.CopyTo(array, 0);
			Array.Sort(array, (BuildingCostDefinition first, BuildingCostDefinition second) => string.CompareOrdinal(first.Id, second.Id));
			return array;
		}

		private void EnsureBuildingsLoaded()
		{
			if (_buildingsById.Count <= 0)
			{
				if (buildingCatalog == null)
				{
					buildingCatalog = Resources.Load<BuildingCatalogDefinition>("Data/BuildingCatalog_V01");
				}
				if (buildingCatalog != null)
				{
					buildings = buildingCatalog.BuildingArray;
				}
				Rebuild(buildings, _buildingsById, (BuildingCostDefinition value) => value.Id, "BuildingCostDefinition");
			}
		}

		public void ConfigureCaptureRules(CaptureRulesDefinition rules)
		{
			captureRules = rules;
		}

		public CaptureRulesDefinition GetCaptureRules()
		{
			if (captureRules == null)
			{
				captureRules = Resources.Load<CaptureRulesDefinition>("Data/CaptureRules_V01");
			}
			return captureRules;
		}

		public void Configure(WeaponData[] weaponContent, EidraData[] eidraContent, AbilityData[] abilityContent, BossData[] bossContent)
		{
			weapons = weaponContent ?? Array.Empty<WeaponData>();
			eidren = eidraContent ?? Array.Empty<EidraData>();
			abilities = abilityContent ?? Array.Empty<AbilityData>();
			bosses = bossContent ?? Array.Empty<BossData>();
			RebuildIndex();
		}

		public void ConfigureWorldMaps(WorldMapDefinition[] mapContent)
		{
			worldMaps = mapContent ?? Array.Empty<WorldMapDefinition>();
			RebuildIndex();
		}

		public void ConfigureItems(ItemDefinition[] itemContent)
		{
			items = itemContent ?? Array.Empty<ItemDefinition>();
			RebuildIndex();
		}

		public void ConfigureResourceNodes(ResourceNodeDefinition[] resourceContent)
		{
			resourceNodes = resourceContent ?? Array.Empty<ResourceNodeDefinition>();
			RebuildIndex();
		}

		public void ConfigureCraftingRecipes(CraftingRecipeDefinition[] recipeContent)
		{
			craftingRecipes = recipeContent ?? Array.Empty<CraftingRecipeDefinition>();
			RebuildIndex();
		}

		public bool TryGetWeapon(string id, out WeaponData value)
		{
			EnsureWeaponsLoaded();
			return _weaponsById.TryGetValue(id, out value);
		}

		public bool TryGetEidra(string id, out EidraData value)
		{
			EnsureEidrenLoaded();
			return _eidrenById.TryGetValue(id, out value);
		}

		public bool TryGetAbility(string id, out AbilityData value)
		{
			return _abilitiesById.TryGetValue(id, out value);
		}

		public bool TryGetBoss(string id, out BossData value)
		{
			return _bossesById.TryGetValue(id, out value);
		}

		public bool TryGetWorldMap(string id, out WorldMapDefinition value)
		{
			return _worldMapsById.TryGetValue(id, out value);
		}

		public bool TryGetItem(string id, out ItemDefinition value)
		{
			EnsureItemsLoaded();
			if (string.IsNullOrWhiteSpace(id))
			{
				value = null;
				return false;
			}
			return _itemsById.TryGetValue(id, out value);
		}

		public bool TryGetResourceNode(string id, out ResourceNodeDefinition value)
		{
			EnsureResourceNodesLoaded();
			if (string.IsNullOrWhiteSpace(id))
			{
				value = null;
				return false;
			}
			return _resourceNodesById.TryGetValue(id, out value);
		}

		public bool TryGetCraftingRecipe(string id, out CraftingRecipeDefinition value)
		{
			EnsureCraftingRecipesLoaded();
			if (string.IsNullOrWhiteSpace(id))
			{
				value = null;
				return false;
			}
			return _craftingRecipesById.TryGetValue(id, out value);
		}

		public WeaponData GetWeapon(string id)
		{
			EnsureWeaponsLoaded();
			return GetRequired(_weaponsById, id, "WeaponData");
		}

		public EidraData GetEidra(string id)
		{
			EnsureEidrenLoaded();
			return GetRequired(_eidrenById, id, "EidraData");
		}

		public AbilityData GetAbility(string id)
		{
			return GetRequired(_abilitiesById, id, "AbilityData");
		}

		public BossData GetBoss(string id)
		{
			return GetRequired(_bossesById, id, "BossData");
		}

		public WorldMapDefinition GetWorldMap(string id)
		{
			return GetRequired(_worldMapsById, id, "WorldMapDefinition");
		}

		public ItemDefinition GetItem(string id)
		{
			EnsureItemsLoaded();
			return GetRequired(_itemsById, id, "ItemDefinition");
		}

		public ResourceNodeDefinition GetResourceNode(string id)
		{
			EnsureResourceNodesLoaded();
			return GetRequired(_resourceNodesById, id, "ResourceNodeDefinition");
		}

		public CraftingRecipeDefinition GetCraftingRecipe(string id)
		{
			EnsureCraftingRecipesLoaded();
			return GetRequired(_craftingRecipesById, id, "CraftingRecipeDefinition");
		}

		public CraftingRecipeDefinition[] GetCraftingRecipes()
		{
			EnsureCraftingRecipesLoaded();
			CraftingRecipeDefinition[] array = new CraftingRecipeDefinition[_craftingRecipesById.Count];
			_craftingRecipesById.Values.CopyTo(array, 0);
			Array.Sort(array, delegate(CraftingRecipeDefinition first, CraftingRecipeDefinition second)
			{
				int num = first.SortOrder.CompareTo(second.SortOrder);
				return (num != 0) ? num : string.CompareOrdinal(first.Id, second.Id);
			});
			return array;
		}

		private void Awake()
		{
			RebuildIndex();
		}

		private void OnValidate()
		{
			RebuildIndex();
		}

		private void RebuildIndex()
		{
			Rebuild(weapons, _weaponsById, (WeaponData value) => value.Id, "WeaponData");
			Rebuild(eidren, _eidrenById, (EidraData value) => value.Id, "EidraData");
			Rebuild(abilities, _abilitiesById, (AbilityData value) => value.Id, "AbilityData");
			Rebuild(bosses, _bossesById, (BossData value) => value.Id, "BossData");
			Rebuild(worldMaps, _worldMapsById, (WorldMapDefinition value) => value.Id, "WorldMapDefinition");
			Rebuild(items, _itemsById, (ItemDefinition value) => value.Id, "ItemDefinition");
			Rebuild(resourceNodes, _resourceNodesById, (ResourceNodeDefinition value) => value.Id, "ResourceNodeDefinition");
			Rebuild(craftingRecipes, _craftingRecipesById, (CraftingRecipeDefinition value) => value.Id, "CraftingRecipeDefinition");
		}

		private void LoadDefaultItemCatalog()
		{
			if (items == null || items.Length == 0)
			{
				if (itemCatalog == null)
				{
					itemCatalog = Resources.Load<ItemCatalogDefinition>("Data/ItemCatalog_V01");
				}
				if (itemCatalog != null)
				{
					items = itemCatalog.ItemArray;
				}
			}
		}

		private void EnsureEidrenLoaded()
		{
			if (_eidrenById.Count <= 0)
			{
				if (eidraCatalog == null)
				{
					eidraCatalog = Resources.Load<EidraCatalogDefinition>("Data/EidraCatalog_V01");
				}
				if (eidraCatalog != null)
				{
					eidren = eidraCatalog.EidraArray;
				}
				Rebuild(eidren, _eidrenById, (EidraData value) => value.Id, "EidraData");
			}
		}

		private void EnsureItemsLoaded()
		{
			if (_itemsById.Count <= 0)
			{
				LoadDefaultItemCatalog();
				Rebuild(items, _itemsById, (ItemDefinition value) => value.Id, "ItemDefinition");
			}
		}

		private void EnsureResourceNodesLoaded()
		{
			if (_resourceNodesById.Count <= 0)
			{
				if (resourceNodeCatalog == null)
				{
					resourceNodeCatalog = Resources.Load<ResourceNodeCatalogDefinition>("Data/ResourceNodeCatalog_V01");
				}
				if (resourceNodeCatalog != null)
				{
					resourceNodes = resourceNodeCatalog.Definitions;
				}
				Rebuild(resourceNodes, _resourceNodesById, (ResourceNodeDefinition value) => value.Id, "ResourceNodeDefinition");
			}
		}

		private void EnsureCraftingRecipesLoaded()
		{
			if (_craftingRecipesById.Count <= 0)
			{
				if (craftingRecipeCatalog == null)
				{
					craftingRecipeCatalog = Resources.Load<CraftingRecipeCatalogDefinition>("Data/CraftingRecipeCatalog_V01");
				}
				if (craftingRecipeCatalog != null)
				{
					craftingRecipes = craftingRecipeCatalog.RecipeArray;
				}
				Rebuild(craftingRecipes, _craftingRecipesById, (CraftingRecipeDefinition value) => value.Id, "CraftingRecipeDefinition");
			}
		}

		private static void Rebuild<T>(IEnumerable<T> source, Dictionary<string, T> target, Func<T, string> idSelector, string contentType) where T : UnityEngine.Object
		{
			target.Clear();
			foreach (T item in source)
			{
				if (!(item == null))
				{
					string text = idSelector(item);
					if (string.IsNullOrWhiteSpace(text))
					{
						throw new InvalidOperationException(contentType + " '" + item.name + "' has no stable ID.");
					}
					if (!target.TryAdd(text, item))
					{
						throw new InvalidOperationException("Duplicate " + contentType + " ID '" + text + "'.");
					}
				}
			}
		}

		private static T GetRequired<T>(IReadOnlyDictionary<string, T> source, string id, string contentType)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("A content ID is required.", "id");
			}
			if (source.TryGetValue(id, out var value))
			{
				return value;
			}
			throw new KeyNotFoundException(contentType + " with ID '" + id + "' is not registered.");
		}

		private void EnsureWeaponsLoaded()
		{
			if (_weaponsById.Count <= 0)
			{
				if (weaponCatalog == null)
				{
					weaponCatalog = Resources.Load<WeaponCatalogDefinition>("Data/WeaponCatalog_V02");
				}
				if (weaponCatalog != null)
				{
					weapons = weaponCatalog.WeaponArray;
				}
				Rebuild(weapons, _weaponsById, (WeaponData value) => value.Id, "WeaponData");
			}
		}
	}
}
