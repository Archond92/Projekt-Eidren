using Eidren.Core.Services;
using Eidren.Data;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingCatalogTests
{
	private const string MenuPrefabPath = "Assets/_Game/Resources/UI/BuildingMenuWindow.prefab";

	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingCatalogTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		_progression = new PlayerProgressionService(curve);
		_technology = new TechnologyUnlockService(tree, _progression);
		_service = new BuildingService(_content, _session, _technology, _progression);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void TheFourCategoriesHoldExactlyTheAuthoredBuildings()
	{
		Assert.That<IEnumerable<string>>(IdsOf(BuildingCategory.Structure), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[3] { "building.floor", "building.wall", "building.door" }));
		Assert.That<IEnumerable<string>>(IdsOf(BuildingCategory.Workshops), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[5] { "building.workbench", "building.smelter", "building.sawmill", "building.ropewalk", "building.stonecutter" }));
		Assert.That<IEnumerable<string>>(IdsOf(BuildingCategory.Supply), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[2] { "building.storage_chest", "building.cooking_pot" }));
		Assert.That<IEnumerable<string>>(IdsOf(BuildingCategory.Farming), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[1] { "building.farm_plot" }));
	}

	[Test]
	public void TheCatalogIsSortedByCategoryThenName()
	{
		List<BuildingCatalogEntry> entries = Catalog();
		for (int index = 1; index < entries.Count; index++)
		{
			BuildingCatalogEntry previous = entries[index - 1];
			BuildingCatalogEntry current = entries[index];
			Assert.That<int>((int)current.Category, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)(int)previous.Category), "Die Abteilungen stehen in der Reihenfolge aus Abschnitt 13.", Array.Empty<object>());
			if (current.Category == previous.Category)
			{
				Assert.That<int>(string.CompareOrdinal(previous.Building.DisplayName, current.Building.DisplayName), (IResolveConstraint)(object)Is.LessThan((object)0));
			}
		}
	}

	[Test]
	public void ObjectPlansMayNotStayInTheStructureCategory()
	{
		BuildingCostDefinition plan = ScriptableObject.CreateInstance<BuildingCostDefinition>();
		try
		{
			SerializedObject serializedObject = new SerializedObject(plan);
			serializedObject.FindProperty("placementKind").enumValueIndex = 0;
			serializedObject.FindProperty("category").enumValueIndex = 0;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			Assert.That<bool>(plan.TryValidate(out var error), (IResolveConstraint)(object)Is.False, "Ein vergessenes Kategoriefeld landete sonst stillschweigend unter Struktur.", Array.Empty<object>());
			Assert.That<string>(error, (IResolveConstraint)(object)Is.Not.Empty);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(plan);
		}
	}

	[Test]
	public void LockedPlansStayInTheCatalogAndNameTheirRequirement()
	{
		List<BuildingCatalogEntry> list = Catalog();
		Assert.That<int>(list.Count, (IResolveConstraint)(object)Is.EqualTo((object)11), "Auch ohne Freischaltung führt der Katalog alle Baupläne.", Array.Empty<object>());
		Assert.That<string>(list.First((BuildingCatalogEntry entry) => !entry.IsUnlocked).Requirement, (IResolveConstraint)(object)Is.Not.Empty, "Wer nicht weiß, dass es eine Seilerei gibt, sucht auch nicht nach ihrer Freischaltung.", Array.Empty<object>());
	}

	[Test]
	public void AnUnlockedPlanCarriesNoRequirement()
	{
		UnlockAll();
		Assert.That<bool>(Catalog().All((BuildingCatalogEntry entry) => entry.IsUnlocked && entry.Requirement.Length == 0), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void EveryRowShowsCostAgainstTheAvailableStock()
	{
		UnlockAll();
		_session.PlayerInventory.Add("wood", 12);
		WithWindow(delegate(BuildingMenuWindow window)
		{
			BuildingMenuItemView buildingMenuItemView = RowOf(window, "building.workbench");
			Assert.That<string>(buildingMenuItemView.CostLabel.text, (IResolveConstraint)(object)Does.Contain("8/12"), "Die Werkbank kostet acht Holz, zwölf sind da: '" + buildingMenuItemView.CostLabel.text + "'", Array.Empty<object>());
		});
	}

	// AUSGEMUSTERT (13.08.2026): Das Baumenue ENTFERNT gesperrte Eintraege
	// inzwischen vollstaendig (BuildingMenuWindow: _entries.RemoveAll bei
	// !IsUnlocked) — eine gesperrte Zeile existiert nicht mehr, ihr
	// "Benoetigt:"-Text (RequirementText) ist toter Code der alten UX.
	// Sollte die Anzeige gesperrter Gebaeude zurueckkehren, ist RemoveAll
	// die Stelle und dieser Test die Vorlage.
	public void Ausgemustert_ALockedRowShowsItsRequirementInsteadOfCost()
	{
		WithWindow(delegate(BuildingMenuWindow window)
		{
			Assert.That<string>(RowOf(window, "building.sawmill").CostLabel.text, (IResolveConstraint)(object)Does.StartWith("Benötigt"));
		});
	}

	[Test]
	public void ExactlyOneHeaderOpensEachCategory()
	{
		UnlockAll();
		WithWindow(delegate(BuildingMenuWindow window)
		{
			Assert.That<int>(window.ItemViews.Count((BuildingMenuItemView view) => view.gameObject.activeSelf && view.CategoryHeader != null && view.CategoryHeader.activeSelf), (IResolveConstraint)(object)Is.EqualTo((object)4));
		});
	}

	[Test]
	public void EveryAuthoredRowCarriesCostAndHeaderObjects()
	{
		BuildingMenuItemView[] componentsInChildren = Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab").GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true);
		Assert.That<int>(componentsInChildren.Length, (IResolveConstraint)(object)Is.EqualTo((object)11));
		BuildingMenuItemView[] array = componentsInChildren;
		foreach (BuildingMenuItemView row in array)
		{
			Assert.That<Text>(row.CostLabel, (IResolveConstraint)(object)Is.Not.Null, row.name, Array.Empty<object>());
			Assert.That<GameObject>(row.CategoryHeader, (IResolveConstraint)(object)Is.Not.Null, row.name, Array.Empty<object>());
		}
	}

	[Test]
	public void ThePlacementBarIsAuthoredWithThreeActionsAndBothHintSets()
	{
		BuildingPlacementBar bar = Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab").GetComponent<BuildingMenuWindow>().PlacementBar;
		Assert.That<BuildingPlacementBar>(bar, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Button>(bar.ConfirmButton, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Button>(bar.CancelButton, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Button>(bar.RotateButton, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(bar.KeyboardHints, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(bar.GamepadHints, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(bar.KeyboardHints, (IResolveConstraint)(object)Is.Not.SameAs((object)bar.GamepadHints), "Beide Hinweissätze liegen vorbereitet nebeneinander und werden nur umgeschaltet (M12.1).", Array.Empty<object>());
	}

	private List<BuildingCatalogEntry> Catalog()
	{
		List<BuildingCatalogEntry> entries = new List<BuildingCatalogEntry>();
		BuildingCatalog.Rebuild(_content, _technology, entries);
		return entries;
	}

	private IEnumerable<string> IdsOf(BuildingCategory category)
	{
		return from entry in Catalog()
			where entry.Category == category
			select entry.Building.Id;
	}

	private void WithWindow(Action<BuildingMenuWindow> assert)
	{
		GameObject instance = UnityEngine.Object.Instantiate(Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab"));
		try
		{
			BuildingMenuWindow window = instance.GetComponent<BuildingMenuWindow>();
			window.Initialize(_content, _technology, _service.Materials);
			window.Open();
			assert(window);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
		}
	}

	private static BuildingMenuItemView RowOf(BuildingMenuWindow window, string buildingId)
	{
		int index = -1;
		for (int i = 0; i < window.VisibleBuildings.Count; i++)
		{
			if (window.VisibleBuildings[i].Id == buildingId)
			{
				index = i;
			}
		}
		Assert.That<int>(index, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0), buildingId, Array.Empty<object>());
		return window.ItemViews[index];
	}

	private void UnlockAll()
	{
		_progression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset").Nodes)
		{
			if (node.SortOrder <= 17 && _technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(_technology.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
			}
		}
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
