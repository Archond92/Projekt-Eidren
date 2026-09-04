using Eidren.Composition;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class WorldMapArtDirectionTests
{
	private const string MapPath = "Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset";

	private const string ThemePath = "Assets/_Game/Data/WorldMap/Themes/WM_Theme_EidrenV01.asset";

	private const string PrefabRoot = "Assets/_Game/UI/WorldMap/Prefabs";

	[Test]
	public void NodeView_BindsDataAndRendersRequiredStates()
	{
		WorldMapDefinition map = LoadMap();
		WorldMapNodeDefinition node = map.Nodes.Single((WorldMapNodeDefinition value) => value.Id == "zone_ember_ruins");
		string before = EditorJsonUtility.ToJson(node);
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapNodeView.prefab");
		try
		{
			WorldMapNodeView component = root.GetComponent<WorldMapNodeView>();
			component.Bind(node, map.FallbackNodeIcon, map.Theme, delegate
			{
			});
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: false, visited: false, eventActive: false, boss: true));
			Assert.That<string>(component.DisplayedName, (IResolveConstraint)(object)Is.EqualTo((object)"Glutruinen"));
			Assert.That<int>(component.DisplayedDanger, (IResolveConstraint)(object)Is.EqualTo((object)5));
			Assert.That<bool>(component.NewMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.VisitedMarkerVisible, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(component.SelectedMarkerVisible, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(component.BossMarkerVisible, (IResolveConstraint)(object)Is.True);
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: true, visited: true, eventActive: true, boss: true));
			Assert.That<bool>(component.SelectedMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.VisitedMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.NewMarkerVisible, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(component.EventMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.BossMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<string>(EditorJsonUtility.ToJson(node), (IResolveConstraint)(object)Is.EqualTo((object)before));
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[Test]
	public void NodeView_AvailableToLocked_UsesMarkerAndInteraction()
	{
		WorldMapDefinition map = LoadMap();
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapNodeView.prefab");
		try
		{
			WorldMapNodeView component = root.GetComponent<WorldMapNodeView>();
			component.Bind(map.Nodes[0], map.FallbackNodeIcon, map.Theme, delegate
			{
			});
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: false, visited: false, eventActive: false, boss: false));
			Assert.That<bool>(component.Button.interactable, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.LockedMarkerVisible, (IResolveConstraint)(object)Is.False);
			component.ApplyState(new WorldMapNodeVisualState(available: false, selected: false, visited: false, eventActive: false, boss: false));
			Assert.That<bool>(component.Button.interactable, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(component.LockedMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<string>(component.DisplayedName, (IResolveConstraint)(object)Is.Not.Empty);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[Test]
	public void NodeView_BossAndEventMarkers_AreIndependent()
	{
		WorldMapDefinition map = LoadMap();
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapNodeView.prefab");
		try
		{
			WorldMapNodeView component = root.GetComponent<WorldMapNodeView>();
			component.Bind(map.Nodes[0], map.FallbackNodeIcon, map.Theme, delegate
			{
			});
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: false, visited: true, eventActive: true, boss: false));
			Assert.That<bool>(component.EventMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.BossMarkerVisible, (IResolveConstraint)(object)Is.False);
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: false, visited: true, eventActive: false, boss: true));
			Assert.That<bool>(component.EventMarkerVisible, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(component.BossMarkerVisible, (IResolveConstraint)(object)Is.True);
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: false, visited: true, eventActive: true, boss: true));
			Assert.That<bool>(component.EventMarkerVisible, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(component.BossMarkerVisible, (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[Test]
	public void InfoPanel_CanOpenAndCloseRepeatedly()
	{
		WorldMapDefinition map = LoadMap();
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapInfoPanel.prefab");
		try
		{
			WorldMapInfoPanel component = root.GetComponent<WorldMapInfoPanel>();
			component.ApplyTheme(map.Theme);
			component.Bind(map.Nodes[1], visited: false);
			component.Open(immediate: true);
			Assert.That<bool>(component.IsOpen, (IResolveConstraint)(object)Is.True);
			Assert.That<string>(component.DisplayedNodeId, (IResolveConstraint)(object)Is.EqualTo((object)"zone_greenwood"));
			component.Close(immediate: true);
			Assert.That<bool>(component.IsOpen, (IResolveConstraint)(object)Is.False);
			component.Open(immediate: true);
			Assert.That<bool>(component.IsOpen, (IResolveConstraint)(object)Is.True);
			Assert.That<int>(component.OpenInvocationCount, (IResolveConstraint)(object)Is.EqualTo((object)2));
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[Test]
	public void ThemeChange_ChangesViewWithoutChangingNodeData()
	{
		WorldMapDefinition map = LoadMap();
		WorldMapThemeData alternate = null;
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapNodeView.prefab");
		try
		{
			// Den Knoten erst NACH LoadPrefabContents aus der Karte holen: Nur im
			// vollen EditMode-Lauf (1357 Tests) war eine vorher gezogene Referenz
			// hier ein entladenes Unity-Objekt, jede Teilmenge war gruen
			// (Bisektion 03.09.2026, OFFENE_PUNKTE OP-17). Die serialisierte
			// Referenz der Karte loest beim Zugriff frisch auf.
			map = LoadMap();
			WorldMapNodeDefinition node = map.Nodes[1];
			if (node == null)
			{
				// Entladenes Asset hinter einer gecachten Referenz: von der Platte nachladen.
				node = AssetDatabase.LoadAssetAtPath<WorldMapNodeDefinition>("Assets/_Game/Data/WorldMap/Nodes/Node_Greenwood.asset");
			}
			Assert.That<bool>(node != null, (IResolveConstraint)(object)Is.True, "Node_Greenwood fehlt.", Array.Empty<object>());
			WorldMapThemeData theme = map.Theme;
			if (theme == null)
			{
				theme = AssetDatabase.LoadAssetAtPath<WorldMapThemeData>(ThemePath);
			}
			Assert.That<bool>(theme != null, (IResolveConstraint)(object)Is.True, "Theme fehlt.", Array.Empty<object>());
			// Die Kopie ebenfalls erst hier anlegen: eine vor LoadPrefabContents
			// instanziierte Kopie war im vollen Lauf danach bereits entladen.
			alternate = UnityEngine.Object.Instantiate(theme);
			SerializedObject serializedObject = new SerializedObject(alternate);
			serializedObject.FindProperty("selection").colorValue = Color.magenta;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			string before = EditorJsonUtility.ToJson(node);
			WorldMapNodeView component = root.GetComponent<WorldMapNodeView>();
			component.Bind(node, map.FallbackNodeIcon, theme, delegate
			{
			});
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: true, visited: true, eventActive: false, boss: false));
			Color original = component.FrameColor;
			component.Bind(node, map.FallbackNodeIcon, alternate, delegate
			{
			});
			component.ApplyState(new WorldMapNodeVisualState(available: true, selected: true, visited: true, eventActive: false, boss: false));
			Assert.That<Color>(component.FrameColor, (IResolveConstraint)(object)Is.EqualTo((object)Color.magenta));
			Assert.That<Color>(component.FrameColor, (IResolveConstraint)(object)Is.Not.EqualTo((object)original));
			Assert.That<string>(EditorJsonUtility.ToJson(node), (IResolveConstraint)(object)Is.EqualTo((object)before));
		}
		finally
		{
			if (alternate != null)
			{
				UnityEngine.Object.DestroyImmediate(alternate);
			}
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[Test]
	public void AllRequiredPrefabs_ExistAndCanvasReferencesValidate()
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
		string[] array = new string[12]
		{
			"WorldMapCanvas", "WorldMapNodeView", "WorldMapConnectionView", "WorldMapInfoPanel", "WorldMapResourceEntry", "WorldMapDangerIndicator", "WorldMapTopStatusBar", "WorldMapStatusEntry", "WorldMapQuickActions", "WorldMapSideMenu",
			"WorldMapTravelButton", "WorldMapTransitionOverlay"
		};
		foreach (string name in array)
		{
			Assert.That<bool>(File.Exists("Assets/_Game/UI/WorldMap/Prefabs/" + name + ".prefab"), (IResolveConstraint)(object)Is.True, name, Array.Empty<object>());
		}
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/UI/WorldMap/Prefabs/WorldMapCanvas.prefab");
		try
		{
			WorldMapCanvasView component = root.GetComponent<WorldMapCanvasView>();
			Assert.That<WorldMapCanvasView>(component, (IResolveConstraint)(object)Is.Not.Null);
			Assert.DoesNotThrow(new TestDelegate(component.ValidateReferences));
			Assert.That<bool>(root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Any((MonoBehaviour monoBehaviour) => monoBehaviour == null), (IResolveConstraint)(object)Is.False);
			Assert.That<Transform>(root.transform.Find("SafeArea"), (IResolveConstraint)(object)Is.Not.Null);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[TestCase(1.7777778f, WorldMapInfoPanelPlacement.Bottom)]
	[TestCase(2f, WorldMapInfoPanelPlacement.Side)]
	[TestCase(2.1666667f, WorldMapInfoPanelPlacement.Side)]
	[TestCase(1.3333334f, WorldMapInfoPanelPlacement.Bottom)]
	public void ResponsiveLayout_CoversRequiredLandscapeRatios(float aspect, WorldMapInfoPanelPlacement expected)
	{
		WorldMapLayoutSpec layout = WorldMapResponsiveLayout.Calculate(aspect);
		Assert.That<WorldMapInfoPanelPlacement>(layout.Placement, (IResolveConstraint)(object)Is.EqualTo((object)expected));
		Assert.That<float>(layout.MapAnchors.width, (IResolveConstraint)(object)Is.GreaterThan((object)0.55f));
		Assert.That<float>(layout.MapAnchors.height, (IResolveConstraint)(object)Is.GreaterThan((object)0.5f));
		Assert.That<float>(layout.InfoAnchors.width, (IResolveConstraint)(object)Is.GreaterThan((object)0.15f));
		Assert.That<float>(layout.InfoAnchors.height, (IResolveConstraint)(object)Is.GreaterThan((object)0.2f));
	}

	[Test]
	public void ThemeDefinesAllFunctionalAndTypographyRoles()
	{
		WorldMapThemeData theme = AssetDatabase.LoadAssetAtPath<WorldMapThemeData>("Assets/_Game/Data/WorldMap/Themes/WM_Theme_EidrenV01.asset");
		Assert.That<WorldMapThemeData>(theme, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(theme.BackgroundDeep.a, (IResolveConstraint)(object)Is.GreaterThan((object)0.9f));
		Assert.That<Color>(theme.TextPrimary, (IResolveConstraint)(object)Is.Not.EqualTo((object)theme.PanelSurface));
		Assert.That<Color>(theme.Selection, (IResolveConstraint)(object)Is.Not.EqualTo((object)theme.Available));
		foreach (WorldMapTypographyRole role in Enum.GetValues(typeof(WorldMapTypographyRole)))
		{
			Assert.That<int>(theme.GetTypography(role).Size, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)12), role.ToString(), Array.Empty<object>());
		}
	}

	private static WorldMapDefinition LoadMap()
	{
		WorldMapDefinition worldMapDefinition = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>("Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset");
		Assert.That<WorldMapDefinition>(worldMapDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<WorldMapThemeData>(worldMapDefinition.Theme, (IResolveConstraint)(object)Is.Not.Null);
		return worldMapDefinition;
	}
}
}
