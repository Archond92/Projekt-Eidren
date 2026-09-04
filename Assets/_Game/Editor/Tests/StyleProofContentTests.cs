using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class StyleProofContentTests
{
	// Nachgezogen (Stilumbau E5, Abschlussarbeit A1): der Alt-Vertrag verlangte
	// >=12 URP/Lit-Sprite-Materialien unter Art/StyleProof/Materials. Seit E4
	// nutzt kein StyleProof-Requisit mehr M_SP_*-Materialien - der Ordner ist
	// leer (A2-Rueckbau), alle 16 SP_-Requisiten tragen das gemeinsame
	// Vertexfarben-Material M_EidrenWorld_VertexLit. Der Ist-Vertrag: die 16
	// Prefabs existieren, jeder Renderer nutzt exakt dieses Material, und jedes
	// Mesh traegt Vertexfarben (sonst waere der Stil nur eine Einfaerbung ohne
	// Wirkung). Faellt jedes Prefab/Material bei Loeschung durch.
	//
	// Nachgezogen (11.08., G-004): die Erdung haengt an 14 der 16 Requisiten
	// einen "ContactShadow"-Decal mit M_World_ContactShadow — Teil des Bauwegs
	// (WorldContactShadowBuilder, G004_ABNAHME.md), kein Stilbruch. Nur die
	// beiden flachen Bodendecker (Gras, Moos) tragen keinen. Der Decal ist
	// von der Material- und der Vertexfarben-Pflicht ausgenommen; jede ANDERE
	// Abweichung faellt weiterhin durch.
	[Test]
	public void ReusableStyleProofAssets_ArePresentAndCompatible()
	{
		string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[1] { "Assets/_Game/Prefabs/Environment/StyleProof" });
		Assert.That<int>(prefabGuids.Length, (IResolveConstraint)(object)Is.EqualTo((object)16), "Die 16 wiederverwendbaren StyleProof-Requisiten (Baeume, Felsen, Ruinen, Bodenbewuchs) muessen vollstaendig vorhanden sein.", Array.Empty<object>());

		Material contactShadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/World/Materials/M_World_ContactShadow.mat");
		Assert.That<Material>(contactShadow, (IResolveConstraint)(object)Is.Not.Null, "Das Kontaktschatten-Material der G-004-Erdung fehlt.", Array.Empty<object>());

		foreach (string guid in prefabGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());

			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("MidPoly"), path);
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("Approved"), path);
			LODGroup group = prefab.GetComponent<LODGroup>();
			Assert.That(group, Is.Not.Null, path + " hat keine LODGroup.");
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3), path + " braucht LOD0/1/2.");
			long previousTriangles = long.MaxValue;
			for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
			{
				Renderer[] renderers = lods[lodIndex].renderers.Where(renderer => renderer != null).ToArray();
				Assert.That(renderers, Is.Not.Empty, path + "/LOD" + lodIndex + " hat keinen Renderer.");
				long triangles = 0;
				foreach (Renderer renderer in renderers)
				{
					Assert.That(renderer.sharedMaterials, Is.Not.Empty, path + "/" + renderer.name);
					Assert.That(renderer.sharedMaterials.All(material => material != null && material.name.StartsWith("MP_ENV_", StringComparison.Ordinal) && material.enableInstancing), Is.True,
						path + ": Renderer '" + renderer.name + "' nutzt kein zentrales instanziertes Phase-5-Material.");
					Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
					Assert.That(mesh, Is.Not.Null, path + "/" + renderer.name + " hat kein Mesh.");
					for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++) triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
				}
				Assert.That(triangles, Is.GreaterThan(0), path + "/LOD" + lodIndex);
				Assert.That(triangles, Is.LessThan(previousTriangles), path + "/LOD" + lodIndex + " reduziert nicht.");
				previousTriangles = triangles;
			}

			Transform contact = prefab.transform.Find("ContactShadow");
			if (contact != null)
			{
				Assert.That(contact.GetComponent<Renderer>().sharedMaterial, Is.EqualTo(contactShadow), path + " nutzt falsche Erdung.");
			}
		}
	}

	[Test]
	public void Greenwood_ContainsCurrentAreaArtPresentation()
	{
		GameObject gameObject = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity", OpenSceneMode.Single).GetRootGameObjects().FirstOrDefault((GameObject item) => item.name == "ZoneRoot");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Transform>(gameObject.transform.Find("StyleProofReferenceArea"), (IResolveConstraint)(object)Is.Null, "The temporary StyleProof reference root was removed by the AreaArt migration and must not return.", Array.Empty<object>());
		Transform transform = gameObject.transform.Find("EnvironmentRoot");
		Assert.That<Transform>(transform, (IResolveConstraint)(object)Is.Not.Null);
		Transform transform2 = transform.Find("AreaArt");
		Assert.That<Transform>(transform2, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Transform>(transform2.Find("AuthoredDecoration"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Transform>(transform2.Find("AreaArtGlobalVolume"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<IEnumerable<MeshCollider>>(transform2.GetComponentsInChildren<Collider>(includeInactive: true).OfType<MeshCollider>(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<Rigidbody[]>(transform2.GetComponentsInChildren<Rigidbody>(includeInactive: true), (IResolveConstraint)(object)Is.Empty);
		Assert.That<bool>(transform2.GetComponentsInChildren<Renderer>(includeInactive: true).Any((Renderer renderer) => renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(transform2.GetComponentsInChildren<Volume>(includeInactive: true).Length, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	// Nachgezogen (Stilumbau E5, Abschlussarbeit A1): der Alt-Vertrag pruefte
	// das verworfene StyleProof-Volume auf Tonemapping (ACES) + Bloom. Laut
	// G003_ABNAHME.md Abschnitt 5/12 rendert das Projekt in Gamma mit
	// LDR-Grading; URP baut die Grading-LUT dort ohne Tonemapper - eine
	// Tonemapping-Komponente ist nachweislich wirkungslos, "aus demselben
	// Grund war das ACES des Style-Proof-Volumes immer wirkungslos". Der
	// bindende Vertrag seit G-003 ist ZoneLightingBuilder.FillVolumeProfile
	// auf dem produktiven Zonen-Volume (ColorAdjustments/LiftGammaGain/
	// Vignette, siehe ZoneLightingBuilder.cs); Test bleibt namensgleich, da er
	// weiterhin "das Greenwood-Volume traegt die verlangten Overrides" prueft
	// - nur der Ort und die Overrides selbst sind der aktuelle Vertrag.
	[Test]
	public void GreenwoodVolume_ContainsRequiredOverrides()
	{
		VolumeProfile volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Settings/AreaArt/Greenwood_AreaArt_Volume.asset");
		Assert.That<VolumeProfile>(volumeProfile, (IResolveConstraint)(object)Is.Not.Null, "Das produktive Greenwood-Zonen-Volume (ZoneLightingBuilder) fehlt.", Array.Empty<object>());

		Assert.That<bool>(volumeProfile.TryGet<ColorAdjustments>(out var color), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(color.contrast.value, (IResolveConstraint)(object)Is.EqualTo((object)12f).Within((object)0.01f));
		Assert.That<float>(color.saturation.value, (IResolveConstraint)(object)Is.EqualTo((object)4f).Within((object)0.01f));

		Assert.That<bool>(volumeProfile.TryGet<LiftGammaGain>(out var lgg), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(lgg.lift.value.w, (IResolveConstraint)(object)Is.EqualTo((object)(-0.01f)).Within((object)0.001f));
		Assert.That<float>(lgg.gain.value.w, (IResolveConstraint)(object)Is.EqualTo((object)0.06f).Within((object)0.001f));

		Assert.That<bool>(volumeProfile.TryGet<Vignette>(out var vignette), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(vignette.intensity.value, (IResolveConstraint)(object)Is.EqualTo((object)0.1f).Within((object)0.001f));
		Assert.That<float>(vignette.smoothness.value, (IResolveConstraint)(object)Is.EqualTo((object)0.4f).Within((object)0.001f));

		// Dokumentiert den G-003-Befund: Tonemapping wird im Gamma+LDR-Pfad
		// bewusst NICHT gesetzt, weil es dort wirkungslos waere.
		Assert.That<bool>(volumeProfile.TryGet<Tonemapping>(out _), (IResolveConstraint)(object)Is.False, "Tonemapping ist im Gamma+LDR-Pfad wirkungslos (G003_ABNAHME.md Abschnitt 5) und sollte hier nicht gesetzt sein.", Array.Empty<object>());
	}
}
}
