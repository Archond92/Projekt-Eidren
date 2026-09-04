using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class WorldMapDefinitionTests
{
	private const string MapPath = "Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset";

	[Test]
	public void RegionalMap_HasExactlyEightUniqueNodes()
	{
		WorldMapDefinition worldMapDefinition = LoadMap();
		worldMapDefinition.ValidateOrThrow();
		Assert.That<int>(worldMapDefinition.Nodes.Count, (IResolveConstraint)(object)Is.EqualTo((object)8));
		Assert.That<int>(worldMapDefinition.Nodes.Select((WorldMapNodeDefinition node) => node.Id).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)8));
	}

	[Test]
	public void RegionalMap_UsesRequiredHubTopology()
	{
		WorldMapDefinition map = LoadMap();
		Assert.That<bool>(map.TryGetNode("home_base", out var home), (IResolveConstraint)(object)Is.True);
		Assert.That<IReadOnlyList<string>>(home.DirectNeighborIds, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[7] { "zone_greenwood", "zone_quarry", "zone_marsh", "zone_ember_ruins", "zone_twilight_grove", "zone_veil_marsh", "zone_grey_rifts" }));
		foreach (WorldMapNodeDefinition outer in map.Nodes.Where((WorldMapNodeDefinition node) => node != home))
		{
			Assert.That<IReadOnlyList<string>>(outer.DirectNeighborIds, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[1] { "home_base" }), outer.Id, Array.Empty<object>());
		}
	}

	[Test]
	public void AllNodeSceneKeys_AreEnabledBuildScenes()
	{
		WorldMapDefinition worldMapDefinition = LoadMap();
		string[] enabledNames = (from scene in EditorBuildSettings.scenes
			where scene.enabled
			select Path.GetFileNameWithoutExtension(scene.path)).ToArray();
		foreach (WorldMapNodeDefinition node in worldMapDefinition.Nodes)
		{
			Assert.That<string[]>(enabledNames, (IResolveConstraint)(object)Does.Contain(node.SceneKey), "Scene key for node '" + node.Id + "' is not an enabled scene.", Array.Empty<object>());
		}
	}

	[Test]
	public void MissingNodeIcon_UsesControlledFallback()
	{
		WorldMapDefinition map = LoadMap();
		WorldMapNodeDefinition worldMapNodeDefinition = ScriptableObject.CreateInstance<WorldMapNodeDefinition>();
		Assert.That<Sprite>(worldMapNodeDefinition.Icon, (IResolveConstraint)(object)Is.Null);
		Assert.That<Sprite>(worldMapNodeDefinition.GetIconOrFallback(map.FallbackNodeIcon), (IResolveConstraint)(object)Is.SameAs((object)map.FallbackNodeIcon));
		UnityEngine.Object.DestroyImmediate(worldMapNodeDefinition);
	}

	[Test]
	public void VisitedRuntimeState_DoesNotModifyDefinitionAsset()
	{
		WorldMapNodeDefinition greenwood = LoadMap().Nodes.Single((WorldMapNodeDefinition node) => node.Id == "zone_greenwood");
		string before = EditorJsonUtility.ToJson(greenwood);
		GameObject gameObject = new GameObject("WorldMapSession_Test");
		GameSession gameSession = gameObject.AddComponent<GameSession>();
		gameSession.Initialize();
		Assert.That<bool>(gameSession.BeginWorldTravel(greenwood.Id, greenwood.SceneKey), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gameSession.CompleteWorldTravel(greenwood.SceneKey), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gameSession.IsWorldMapNodeVisited(greenwood.Id), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(EditorJsonUtility.ToJson(greenwood), (IResolveConstraint)(object)Is.EqualTo((object)before));
		UnityEngine.Object.DestroyImmediate(gameObject);
	}

	[Test]
	public void RegionalMap_UsesOnlyProjectOwnedWorldMapArt()
	{
		WorldMapDefinition worldMapDefinition = LoadMap();
		Assert.That<string>(AssetDatabase.GetAssetPath(worldMapDefinition.MapVisual), (IResolveConstraint)(object)Does.StartWith("Assets/_Game/Art/WorldMap/Backgrounds/"));
		foreach (WorldMapNodeDefinition node in worldMapDefinition.Nodes)
		{
			Assert.That<Sprite>(node.Icon, (IResolveConstraint)(object)Is.Not.Null, node.Id, Array.Empty<object>());
			Assert.That<string>(AssetDatabase.GetAssetPath(node.Icon), (IResolveConstraint)(object)Does.StartWith("Assets/_Game/Art/WorldMap/Nodes/"), node.Id, Array.Empty<object>());
		}
	}

	private static WorldMapDefinition LoadMap()
	{
		WorldMapDefinition worldMapDefinition = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>("Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset");
		Assert.That<WorldMapDefinition>(worldMapDefinition, (IResolveConstraint)(object)Is.Not.Null);
		return worldMapDefinition;
	}
}
}
