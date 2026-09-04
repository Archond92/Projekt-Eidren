using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class V02ContainerVisualTests
{
	[Test]
	public void ForgeContainers_AreEightUniqueStateful3DFamilies()
	{
		string[] obj = new string[8] { "SupplyChest", "OptionalChest", "EliteChest", "CompletionChest", "SmallRewardChest", "MediumRewardChest", "LargeRewardChest", "RecoveryContainer" };
		HashSet<string> signatures = new HashSet<string>();
		string[] array = obj;
		foreach (string name in array)
		{
			string path = "Assets/_Game/Prefabs/Containers/Forge/" + name + "_2D.prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<SpriteRenderer>(prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true), (IResolveConstraint)(object)Is.Null, path, Array.Empty<object>());
			Assert.That<int>(prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true).Length, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)5), path, Array.Empty<object>());
			Assert.That<WorldChestVisual>(prefab.GetComponent<WorldChestVisual>(), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<BoxCollider>(prefab.GetComponent<BoxCollider>(), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<Transform>(prefab.transform.Find("LidPivot"), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<Transform>(prefab.transform.Find("ClosedDetails"), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<Transform>(prefab.transform.Find("OpenedDetails"), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<Transform>(prefab.transform.Find("EmptiedDetails"), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Bounds bounds = default(Bounds);
			bool initialized = false;
			Renderer[] componentsInChildren = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (!initialized)
				{
					bounds = renderer.bounds;
					initialized = true;
				}
				else
				{
					bounds.Encapsulate(renderer.bounds);
				}
			}
			string signature = $"{bounds.size.x:0.000}/{bounds.size.y:0.000}/" + $"{prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true).Length}";
			Assert.That<bool>(signatures.Add(signature), (IResolveConstraint)(object)Is.True, name + " lacks a unique silhouette signature", Array.Empty<object>());
		}
	}

	[Test]
	public void TierTwoGameplayPrefabs_UseTheirOwnActorIdentity()
	{
		(string, string)[] array = new(string, string)[5]
		{
			("Riftling", "riftling"),
			("RootCharger", "root_charger"),
			("MoorThrower", "moor_thrower"),
			("GraniteShell", "granite_shell"),
			("RiftGuardian", "rift_guardian")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string stem = tuple.Item1;
			string id = tuple.Item2;
			string path = "Assets/_Game/Prefabs/Enemies/TierTwo/" + stem + ".prefab";
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			SpriteActorPresentation componentInChildren = gameObject.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true);
			// Auf 3D umgestellte Gegner tragen keine SpriteActorPresentation mehr.
			// Die Sprite-Kennung (ActorId) ist ein reiner Marker der 2D-Pipeline:
			// ausser Tests und dem V02ActorVisualBuilder liest sie kein Code.
			// Ein Gegner darf aber nicht BEIDES tragen — ActorPresentationLocator
			// nimmt die erste Darstellung im Baum, ein Nebeneinander waere von
			// der Kindreihenfolge abhaengig statt entschieden.
			CreatureMeshPresentation mesh = gameObject.GetComponentInChildren<CreatureMeshPresentation>(includeInactive: true);
			if (mesh != null)
			{
				Assert.That<SpriteActorPresentation>(componentInChildren, (IResolveConstraint)(object)Is.Null, path + " traegt 3D- UND Sprite-Darstellung", Array.Empty<object>());
			}
			else
			{
				Assert.That<SpriteActorPresentation>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
				Assert.That<string>(componentInChildren.ActorId, (IResolveConstraint)(object)Is.EqualTo((object)id), path, Array.Empty<object>());
			}
			Assert.That<StaticSpriteActorPresentation>(gameObject.GetComponentInChildren<StaticSpriteActorPresentation>(includeInactive: true), (IResolveConstraint)(object)Is.Null, path, Array.Empty<object>());
		}
	}
}
}
