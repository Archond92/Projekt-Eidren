// G-003: Diagnose- und Regressionstest fuer den Bodenschatten der Figuren.
//
// Anlass: In keinem Capture bis einschliesslich G-002 war unter der Spielfigur
// ein Bodenschatten sichtbar, obwohl das Prefab die Komponente trug. Zwei
// Ursachen wurden in G-003 in den Quellen behoben (doppelt multiplizierte
// Deckkraft, doppelt angewandte Groesse).
//
// Wanderer3D-Umstellung (Task 6/7): Der Spieler ist seither vollstaendig 3D
// (MeshActorPresentation statt SpriteActorPresentation) und traegt keinen
// DynamicActorGroundShadow mehr - echte URP-Schatten des Meshes ersetzen den
// Blob-Schatten. Die frueheren Sprite-/Vertrag-Asserts (Groesse, Deckkraft,
// Shader) betrafen ausschliesslich den Spieler und sind damit hinfaellig; sie
// wurden durch eine Regressionswache ersetzt, die sicherstellt, dass die
// Komponente am Spieler nicht wieder auftaucht. Gegner nutzen weiterhin den
// klassischen Blob-Schatten (siehe z. B. GaronPresentationPlayModeTests) und
// sind von dieser Umstellung nicht betroffen. G-004 baut auf genau dieser
// Kette auf.

using Eidren.Presentation;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class GroundShadowPlayModeTests
{
	[UnityTest]
	public IEnumerator PlayerGroundShadow_SpielerHatKeinenBlobSchattenMehr()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		// Das Player-Prefab liegt nicht unter Resources; der Test laeuft im
		// Editor-PlayMode und laedt direkt aus der AssetDatabase.
		GameObject prefab = null;
#if UNITY_EDITOR
		prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
#endif
		Assert.That(prefab, Is.Not.Null, "Player-Prefab nicht gefunden.");

		GameObject instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
		try
		{
			// Ein Frame fuer Awake, ein weiterer fuer LateUpdate.
			yield return null;
			yield return null;

			DynamicActorGroundShadow shadow = instance.GetComponentInChildren<DynamicActorGroundShadow>(includeInactive: true);
			Assert.That(shadow, Is.Null, "Blob-Schatten am Spieler ist seit der 3D-Umstellung obsolet (echte URP-Schatten).");
		}
		finally
		{
			Object.Destroy(instance);
		}
	}
}
}
