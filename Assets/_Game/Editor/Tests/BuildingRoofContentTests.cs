using Eidren.Data;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingRoofContentTests
{
	private const string ResourceFolder = "Art/Buildings";

	private const string DataFolder = "Assets/_Game/Data/Buildings";

	[Test]
	public void TheRoofTileIsAuthoredAndLoadable()
	{
		GameObject gameObject = Resources.Load<GameObject>("Art/Buildings/BLD_Roof_Tile");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, "Dachprefab fehlt: Art/Buildings/BLD_Roof_Tile", Array.Empty<object>());
		Assert.That<MeshRenderer>(gameObject.GetComponentInChildren<MeshRenderer>(includeInactive: true), (IResolveConstraint)(object)Is.Not.Null, "Die Dachkachel hat kein Sichtteil.", Array.Empty<object>());
	}

	[Test]
	public void TheRoofTileChangesNeitherPhysicsNorNavigation()
	{
		GameObject gameObject = Resources.Load<GameObject>("Art/Buildings/BLD_Roof_Tile");
		Assert.That<Collider>(gameObject.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "Ein Dach darf kein Raycast-Ziel erzeugen.", Array.Empty<object>());
		Assert.That<NavMeshObstacle>(gameObject.GetComponentInChildren<NavMeshObstacle>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "Ein Dach blockiert keine Navigation (Abschnitt 14).", Array.Empty<object>());
	}

	[Test]
	public void TheRoofTileCoversExactlyOneCellAndLiesFlat()
	{
		Transform transform = Resources.Load<GameObject>("Art/Buildings/BLD_Roof_Tile").GetComponentInChildren<MeshRenderer>(includeInactive: true).transform;
		Assert.That<float>(transform.localScale.x, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.0001f));
		Assert.That<float>(transform.localScale.y, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.0001f));
		Assert.That<float>(Mathf.Abs(Vector3.Dot(transform.rotation * Vector3.forward, Vector3.up)), (IResolveConstraint)(object)Is.GreaterThan((object)0.99f), "Die Dachkachel steht statt zu liegen (M10.4).", Array.Empty<object>());
	}

	[Test]
	public void TheRoofMaterialCanFade()
	{
		Material material = Resources.Load<Material>("Art/Buildings/BLD_Roof");
		Assert.That<Material>(material, (IResolveConstraint)(object)Is.Not.Null, "Dachmaterial fehlt: Art/Buildings/BLD_Roof", Array.Empty<object>());
		Assert.That<float>(material.GetFloat("_Surface"), (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.0001f), "Das Dachmaterial muss durchscheinen können.", Array.Empty<object>());
		Assert.That<Texture>(material.GetTexture("_BaseMap"), (IResolveConstraint)(object)Is.Not.Null, "Das Dach hat keine Deckung — eine glatte Fläche wirkte aus der isometrischen Kamera wie ein Deckel.", Array.Empty<object>());
	}

	[Test]
	public void EveryEdgePrefabIsAnOcclusionFadeTarget()
	{
		GameObject[] array = EdgePrefabs();
		Assert.That<GameObject[]>(array, (IResolveConstraint)(object)Is.Not.Empty, "Ohne Kantenbauteile gäbe es nichts zu prüfen.", Array.Empty<object>());
		GameObject[] array2 = array;
		foreach (GameObject prefab in array2)
		{
			Assert.That<OcclusionFadeTarget>(prefab.GetComponent<OcclusionFadeTarget>(), (IResolveConstraint)(object)Is.Not.Null, "'" + prefab.name + "' bliebe vor dem Spieler undurchsichtig.", Array.Empty<object>());
		}
	}

	[Test]
	public void TheFadeMarkLeavesPhysicsAlone()
	{
		GameObject[] array = EdgePrefabs();
		foreach (GameObject prefab in array)
		{
			Assert.That<Collider>(prefab.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Not.Null, "'" + prefab.name + "' hat seinen Collider verloren.", Array.Empty<object>());
		}
	}

	private static GameObject[] EdgePrefabs()
	{
		BuildingFootprint footprint;
		GameObject prefab;
		return (from guid in AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" })
			select AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(guid)) into plan
			where plan != null && plan.PlacementKind == BuildingPlacementKind.Edge
			// Bereichsvariable umbenannt: der Dekompilierer hat die out-Variablen
			// auf Methodenebene gezogen, wodurch 'prefab' doppelt belegt war.
			select (!plan.TryGetLevel(1, out footprint, out prefab)) ? null : prefab into edgePrefab
			where edgePrefab != null
			select edgePrefab).ToArray();
	}
}
}
