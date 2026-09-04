using Eidren.Combat;
using Eidren.Composition;
using Eidren.Eidra;
using Eidren.Gameplay.Presentation;
using Eidren.Player;
using Eidren.Presentation;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class PlayerPrefabTests
{
	private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";

	[Test]
	public void PlayerPrefab_Contains3DPlayerStructureAndReferences()
	{
		GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(root, Is.Not.Null, "Missing prefab: " + PlayerPrefabPath);
		PlayerPrefabBindings component = root.GetComponent<PlayerPrefabBindings>();
		Assert.That(component, Is.Not.Null);
		Assert.That(component.CharacterController, Is.SameAs((object)root.GetComponent<CharacterController>()));
		Assert.That(component.Damageable, Is.SameAs((object)root.GetComponent<Damageable>()));
		Assert.That(component.Motor, Is.SameAs((object)root.GetComponent<PlayerMotor>()));
		Assert.That(component.EidraTeam, Is.SameAs((object)root.GetComponent<EidraTeamController>()));
		Assert.That(component.Combat, Is.SameAs((object)root.GetComponent<PlayerCombatController>()));
		Assert.That(component.Interaction, Is.SameAs((object)root.GetComponent<Eidren.Interaction.InteractionController>()));
		Assert.That(component.WeaponHitbox, Is.SameAs((object)root.GetComponent<MeleeWeaponHitbox>()));
		Assert.That(component.Consumables, Is.SameAs((object)root.GetComponent<ConsumableController>()));
		Assert.That(component.VisualAnimator, Is.SameAs((object)root.GetComponent<PlayerVisualAnimator>()));

		Transform art = root.transform.Find("Player_Visual");
		Assert.That(art, Is.Not.Null);
		MeshActorPresentation mesh = art.GetComponentInChildren<MeshActorPresentation>(true);
		Assert.That(mesh, Is.Not.Null, "Player_Visual traegt keine MeshActorPresentation");
		Assert.That(component.PlayerVisual, Is.SameAs((object)mesh));
		Assert.That(root.GetComponentInChildren<SpriteActorPresentation>(true), Is.Null,
			"Spieler traegt noch die 2D-Darstellung");
		Assert.That(root.GetComponentInChildren<DynamicActorGroundShadow>(true), Is.Null,
			"Blob-Schatten am Spieler ist obsolet (echte URP-Schatten)");

		SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		Assert.That(renderers, Is.Not.Empty, "Wanderer-Rig fehlt");
		foreach (SkinnedMeshRenderer renderer in renderers)
		{
			Assert.That(renderer.sharedMaterial, Is.Not.Null);
			Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Eidren/Actors/WandererVertexLit"),
				"Falscher Wanderer-Vertexfarbshader an " + renderer.name + ": " + renderer.sharedMaterial.shader.name);
		}

		Animation animationPlayer = art.GetComponentInChildren<Animation>(true);
		Assert.That(animationPlayer, Is.Not.Null, "Animation-Komponente fehlt");
		Assert.That(animationPlayer["Ruhe_Ohne"], Is.Not.Null, "Clip Ruhe_Ohne nicht angebunden");
		Assert.That(animationPlayer["Ruhe_Dolche"], Is.Not.Null, "Clip Ruhe_Dolche nicht angebunden");
		Assert.That(animationPlayer["Angriff_Hammer"], Is.Not.Null, "Clip Angriff_Hammer nicht angebunden");
		Assert.That(animationPlayer["Abbau_Spitzhacke"], Is.Not.Null, "Clip Abbau_Spitzhacke nicht angebunden");
		Assert.That(animationPlayer["Oeffnen"], Is.Not.Null, "Clip Oeffnen nicht angebunden (W-009)");

		Transform weapon = root.transform.Find("WeaponDriver");
		Assert.That(weapon, Is.Not.Null);
		Assert.That(component.WeaponVisual, Is.SameAs((object)weapon));
		Assert.That(weapon.GetComponent<WandererEquipmentVisual>(), Is.Not.Null);
		Assert.That(weapon.GetComponent<PlayerWeaponVisual>(), Is.Null, "2D-Waffen-Billboard ist obsolet");
		Assert.That(weapon.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty,
			"WeaponDriver traegt noch Sprite-Waffen");

		Assert.That(root.GetComponent<PlayerHarvestVisual>(), Is.Not.Null);
		Assert.That(root.GetComponentsInChildren<CombatTargetIndicator>(true), Has.Length.EqualTo(1),
			"Genau eine wiederverwendete Combat-Ring-Instanz muss im Player-Prefab autoriert sein.");
		Assert.That(component.MaxHealth, Is.EqualTo(100f));
		Assert.DoesNotThrow(new TestDelegate(component.ValidateReferences));
	}

	[Test]
	public void PlayerPrefab_SlotMeshesStartInaktivNurBasisAktiv()
	{
		GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(root, Is.Not.Null);
		// W-003: auch die Werkzeuge muessen anfangs inaktiv sein — sie blieben
		// nach dem GLB-Import aktiv und hingen dauerhaft sichtbar an der Hand.
		string[] inactive = new string[18]
		{
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer",
			"Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense"
		};
		Assert.That(FindDeep(root.transform, "Basis"), Is.Not.Null);
		Assert.That(FindDeep(root.transform, "Basis").gameObject.activeSelf, Is.True, "Basis muss aktiv sein");
		foreach (string nodeName in inactive)
		{
			Transform node = FindDeep(root.transform, nodeName);
			Assert.That(node, Is.Not.Null, "Knoten fehlt: " + nodeName);
			Assert.That(node.gameObject.activeSelf, Is.False, "Muss anfangs inaktiv sein: " + nodeName);
		}
	}

	[Test]
	public void PlayerPrefab_PreservesCharacterControllerValues()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(gameObject, Is.Not.Null, "Missing prefab: " + PlayerPrefabPath);
		CharacterController component = gameObject.GetComponent<CharacterController>();
		Assert.That(component, Is.Not.Null);
		Assert.That(component.height, Is.EqualTo(2f));
		Assert.That(component.radius, Is.EqualTo(0.48f));
		Assert.That(component.center, Is.EqualTo(new Vector3(0f, 1f, 0f)));
		Assert.That(gameObject.GetComponents<Collider>(), Has.Length.EqualTo(1));
		Assert.That(gameObject.GetComponents<CharacterController>(), Has.Length.EqualTo(1));
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
