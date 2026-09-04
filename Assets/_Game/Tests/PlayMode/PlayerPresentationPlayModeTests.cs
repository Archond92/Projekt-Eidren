using Eidren.Presentation;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class PlayerPresentationPlayModeTests
{
	[UnityEngine.TestTools.UnityTest]
	public System.Collections.IEnumerator Player3D_RuestungswechselSchaltetGenauEinSlotmesh()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		GameObject prefab = Resources.Load<GameObject>("Player") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
		GameObject player = UnityEngine.Object.Instantiate(prefab);
		yield return null;
		MeshActorPresentation mesh = player.GetComponentInChildren<MeshActorPresentation>(true);
		Assert.That(mesh, Is.Not.Null);
		mesh.SetArmorTiers(2, 1, 0, 3);
		yield return null;
		Assert.That(FindDeep(player.transform, "Helm_Kupfer").gameObject.activeSelf, Is.True);
		Assert.That(FindDeep(player.transform, "Helm_Stoff").gameObject.activeSelf, Is.False);
		Assert.That(FindDeep(player.transform, "Harnisch_Stoff").gameObject.activeSelf, Is.True);
		Assert.That(FindDeep(player.transform, "Haende_Stoff").gameObject.activeSelf, Is.False);
		Assert.That(FindDeep(player.transform, "Haende_Kupfer").gameObject.activeSelf, Is.False);
		Assert.That(FindDeep(player.transform, "Haende_Eisen").gameObject.activeSelf, Is.False);
		Assert.That(FindDeep(player.transform, "Beine_Eisen").gameObject.activeSelf, Is.True);
		UnityEngine.Object.Destroy(player);
	}

	[UnityEngine.TestTools.UnityTest]
	public System.Collections.IEnumerator Player3D_AngriffSpieltAngriffsClip()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
		GameObject player = UnityEngine.Object.Instantiate(prefab);
		yield return null;
		MeshActorPresentation mesh = player.GetComponentInChildren<MeshActorPresentation>(true);
		Animation animationPlayer = player.GetComponentInChildren<Animation>(true);
		Assert.That(mesh, Is.Not.Null);
		Assert.That(animationPlayer, Is.Not.Null);
		mesh.SetWeaponStance(MeshActorPresentation.StanceHammer, showWeaponMesh: true);
		mesh.SetAuthoredTimedState("hammer1", 0.8f);
		yield return null;
		Assert.That(animationPlayer.IsPlaying("Angriff_Hammer"), Is.True, "Angriff_Hammer laeuft nicht");
		Assert.That(FindDeep(player.transform, "Waffe_Hammer").gameObject.activeSelf, Is.True);
		UnityEngine.Object.Destroy(player);
	}

	[UnityEngine.TestTools.UnityTest]
	public System.Collections.IEnumerator Player3D_AbbauZeigtWerkzeugMeshUndSpieltAbbauClip()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		// W-001: Waehrend des Abbaus ersetzt das Werkzeug-Mesh die Waffe und der
		// Abbau-Clip aus der Wanderer.glb laeuft als Schleife; danach kehrt die
		// Waffe zurueck.
		GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
		GameObject player = UnityEngine.Object.Instantiate(prefab);
		yield return null;
		MeshActorPresentation mesh = player.GetComponentInChildren<MeshActorPresentation>(true);
		Animation animationPlayer = player.GetComponentInChildren<Animation>(true);
		Assert.That(mesh, Is.Not.Null);
		Assert.That(animationPlayer, Is.Not.Null);
		mesh.SetWeaponStance(MeshActorPresentation.StanceSpear, showWeaponMesh: true);

		mesh.SetHarvestTool(MeshActorPresentation.StanceAxe);
		mesh.SetAuthoredState("harvest", 0f, loop: true);
		yield return null;
		Assert.That(animationPlayer.IsPlaying("Abbau_Axt"), Is.True, "Abbau_Axt laeuft nicht");
		Assert.That(FindDeep(player.transform, "Waffe_Axt").gameObject.activeSelf, Is.True, "Werkzeug-Mesh fehlt waehrend des Abbaus");
		Assert.That(FindDeep(player.transform, "Waffe_Speer").gameObject.activeSelf, Is.False, "Waffe muss waehrend des Abbaus ausgeblendet sein");

		mesh.ClearHarvestTool();
		yield return null;
		Assert.That(FindDeep(player.transform, "Waffe_Axt").gameObject.activeSelf, Is.False, "Werkzeug-Mesh muss nach dem Abbau wieder weg sein");
		Assert.That(FindDeep(player.transform, "Waffe_Speer").gameObject.activeSelf, Is.True, "Waffe muss nach dem Abbau zurueckkehren");
		Assert.That(animationPlayer.IsPlaying("Ruhe_Speer"), Is.True, "Nach dem Abbau muss die Ruhe der Waffen-Stance laufen");
		UnityEngine.Object.Destroy(player);
	}

	private static Transform FindDeep(Transform root, string childName)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child.name == childName)
			{
				return child;
			}
		}
		return null;
	}
}
}
