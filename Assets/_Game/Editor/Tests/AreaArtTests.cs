using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class AreaArtTests
{
	private static readonly string[] AreaKeys = new string[5] { "Greenwood", "Marsh", "Quarry", "EmberRuins", "HomeBase" };

	private static readonly string[] SceneNames = new string[5] { "Zone_Greenwood", "Zone_Marsh", "Zone_Quarry", "Zone_EmberRuins", "HomeBase" };

	[Test]
	public void EveryAreaArt_ValidatesAndHasOneZone()
	{
		string[] areaKeys = AreaKeys;
		foreach (string key in areaKeys)
		{
			ZoneAreaArtDefinition area = AssetDatabase.LoadAssetAtPath<ZoneAreaArtDefinition>("Assets/_Game/Data/AreaArt/AreaArt_" + key + ".asset");
			Assert.That<ZoneAreaArtDefinition>(area, (IResolveConstraint)(object)Is.Not.Null, key, Array.Empty<object>());
			Assert.That<string[]>(area.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty, key + ": " + string.Join("; ", area.GetValidationErrors()), Array.Empty<object>());
			Assert.That<int>(AssetDatabase.FindAssets("t:ZoneDefinition", new string[1] { "Assets/_Game/Data/Zones" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ZoneDefinition>)
				.Count((ZoneDefinition zone) => zone != null && zone.AreaArt == area), (IResolveConstraint)(object)Is.EqualTo((object)1), key, Array.Empty<object>());
		}
	}

	[Test]
	public void AreaArtData_ContainsNoHeightOrWidthField()
	{
		Type[] array = new Type[4]
		{
			typeof(ZoneAreaArtDefinition),
			typeof(AreaArtGroundLayer),
			typeof(AreaArtResourceVariant),
			typeof(AreaArtDecoration)
		};
		foreach (Type type in array)
		{
			Assert.That<string[]>((from field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				select field.Name.ToLowerInvariant() into name
				where name.Contains("height") || name.Contains("width")
				select name).ToArray(), (IResolveConstraint)(object)Is.Empty, type.Name, Array.Empty<object>());
		}
	}

	[Test]
	public void EveryZone_HasOneEnabledGroundAndAreaArtRoot()
	{
		string[] sceneNames = SceneNames;
		foreach (string sceneName in sceneNames)
		{
			GameObject root = Root(Open(sceneName));
			Transform transform = root.transform.Find("EnvironmentRoot");
			Assert.That<Transform>(transform, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Transform transform2 = transform.Find("WalkableGround");
			Assert.That<Transform>(transform2, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<bool>(transform2.GetComponent<Renderer>().enabled, (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
			Assert.That<Transform>(transform.Find("AreaArt"), (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			string[] array = new string[7] { "Ground_StyleLayer", "GroundTransition_West", "GroundTransition_North", "ReferencePath", "LargeObstacle_West", "MassiveObstacle", "RuinsWall_West" };
			foreach (string name in array)
			{
				Assert.That<Transform>(FindRecursive(root.transform, name), (IResolveConstraint)(object)Is.Null, sceneName + ": " + name, Array.Empty<object>());
			}
		}
	}

	[Test]
	public void EveryOutdoorSpawn_IsOnExactlyOneEdge()
	{
		foreach (string sceneName in SceneNames.Take(4))
		{
			Transform spawns = Root(Open(sceneName)).transform.Find("PlayerSpawnPoints");
			foreach (Transform spawn in spawns)
			{
				bool num = Mathf.Abs(Mathf.Abs(spawn.position.x) - 33f) <= 0.02f;
				bool onZ = Mathf.Abs(Mathf.Abs(spawn.position.z) - 33f) <= 0.02f;
				Assert.That<bool>(num ^ onZ, (IResolveConstraint)(object)Is.True, $"{sceneName}/{spawn.name}: {spawn.position}", Array.Empty<object>());
				Vector3 toCenter = new Vector3(0f - spawn.position.x, 0f, 0f - spawn.position.z).normalized;
				Assert.That<float>(Vector3.Dot(spawn.forward, toCenter), (IResolveConstraint)(object)Is.GreaterThan((object)0.98f), sceneName + "/" + spawn.name + " faces away from center.", Array.Empty<object>());
			}
			Vector3 defaultPosition = spawns.Find("Spawn_Default").position;
			Vector3 fromSouth = spawns.Find("Spawn_FromSouth").position;
			Assert.That<float>(Vector3.Distance(defaultPosition, fromSouth), (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)6f), sceneName, Array.Empty<object>());
		}
	}

	[Test]
	public void HomeSpawn_IsOutsideReservedBuildArea()
	{
		Vector3 position = Root(Open("HomeBase")).transform.Find("PlayerSpawnPoints/Spawn_Default").position;
		Assert.That<float>(new Vector2(position.x, position.z).magnitude, (IResolveConstraint)(object)Is.GreaterThan((object)12f));
	}

	[Test]
	public void RemovedPathAssets_DoNotExist()
	{
		Assert.That<bool>(File.Exists("Assets/_Game/Art/StyleProof" + "/Textures/greenwood_path_v01.png"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(File.Exists("Assets/_Game/Art/StyleProof" + "/Materials/M_SP_Path.mat"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(File.Exists("Assets/_Game/Art/StyleProof" + "/Materials/M_SP_PathEdge.mat"), (IResolveConstraint)(object)Is.False);
	}

	private static Scene Open(string name)
	{
		return EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity", OpenSceneMode.Single);
	}

	private static GameObject Root(Scene scene)
	{
		return scene.GetRootGameObjects().First((GameObject item) => item.name == "ZoneRoot");
	}

	private static Transform FindRecursive(Transform root, string name)
	{
		if (root.name == name)
		{
			return root;
		}
		foreach (Transform item in root)
		{
			Transform result = FindRecursive(item, name);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}
}
}
