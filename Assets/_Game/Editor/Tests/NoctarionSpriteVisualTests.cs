using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class NoctarionSpriteVisualTests
{
	// Die 2D-Sprite-Sheets des Noctarion wurden am 03.09.2026 als Altlast
	// entfernt (AltlastInventar: keine Referenzen). Die zugehoerige Alpha-
	// Pruefung NoctarionMaps_AreCompleteUniqueAndAlphaAligned entfiel damit.

	[Test]
	public void NoctarionPrefab_UsesCanonicalMidpoly3DPresentation()
	{
		const string legacy = "Assets/_Game/Resources/Prefabs/Actors/2D/Noctarion_2D.prefab";
		Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(legacy), Is.Null);
		Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + legacy), Is.True);
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Prefabs/Actors/3D/Noctarion_3D.prefab");
		Assert.That(gameObject, Is.Not.Null);
		Assert.That(gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null);
		Assert.That(gameObject.GetComponentInChildren<LODGroup>(true).GetLODs(), Has.Length.EqualTo(3));
		Assert.That(gameObject.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.False);
	}
}
}
