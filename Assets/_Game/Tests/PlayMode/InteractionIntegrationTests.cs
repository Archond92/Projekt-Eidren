using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class InteractionIntegrationTests : InputTestFixture
{
	private readonly struct TestReferences
	{
		public readonly PlayerPrefabBindings Player;

		public readonly PlayerInputReader Input;

		public readonly ResourceNode Resource;

		public readonly InteractionController Controller;

		public TestReferences(PlayerPrefabBindings player, PlayerInputReader input, ResourceNode resource, InteractionController controller)
		{
			Player = player;
			Input = input;
			Resource = resource;
			Controller = controller;
		}
	}

	[UnityTest]
	public IEnumerator CopperMining_SynchronizesTwoVisibleImpactBursts()
	{
		yield return LoadCopperZone();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ResourceNode copper = null;
		CopperMiningVisualFeedback feedback = null;
		ResourceNode[] array = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (ResourceNode candidate in array)
		{
			if (candidate.Definition != null && candidate.Definition.Id == "resource.copper_vein")
			{
				CopperMiningVisualFeedback candidateFeedback = candidate.GetComponentInChildren<CopperMiningVisualFeedback>(includeInactive: true);
				if (!(candidateFeedback == null))
				{
					copper = candidate;
					feedback = candidateFeedback;
					break;
				}
			}
		}
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ResourceNode>(copper, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CopperMiningVisualFeedback>(feedback, (IResolveConstraint)(object)Is.Not.Null);
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		InteractionContext context = new InteractionContext(player.gameObject, player.transform, copper.transform.position - player.transform.position, inventory);
		copper.BeginInteraction(in context);
		copper.UpdateInteraction(in context, 0.37f);
		yield return null;
		Assert.That<bool>(feedback.IsMining, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(feedback.ImpactCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		copper.UpdateInteraction(in context, 0.85f);
		yield return null;
		Assert.That<int>(feedback.ImpactCount, (IResolveConstraint)(object)Is.EqualTo((object)2));
		copper.CancelInteraction(in context, InteractionCancelReason.InputReleased);
		yield return null;
		Assert.That<bool>(feedback.IsMining, (IResolveConstraint)(object)Is.False);
	}

	[UnityTest]
	public IEnumerator InputReader_PreservesKeyboardGamepadAndTouchPaths()
	{
		yield return LoadCopperZone();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		InteractionButton touch = UnityEngine.Object.FindFirstObjectByType<InteractionButton>(FindObjectsInactive.Include);
		TestReferences references = PrepareOreInteraction();
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(input.HasInteractionBinding("<Keyboard>/r"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.HasInteractionBinding("<Gamepad>/buttonWest"), (IResolveConstraint)(object)Is.True);
		Assert.That<InteractionButton>(touch, (IResolveConstraint)(object)Is.Not.Null);
		input.SetGameplayEnabled(enabled: true);
		ResourceTargetIndicator component = references.Controller.GetComponent<ResourceTargetIndicator>();
		Assert.That<ResourceTargetIndicator>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(component.IsVisible || component.CombatSuppressed, (IResolveConstraint)(object)Is.True,
			"Der Interaktionsring muss sichtbar sein oder einem aktiven Combat-Ring eindeutig untergeordnet werden.", Array.Empty<object>());
		Assert.That<IInteractable>(component.Target, (IResolveConstraint)(object)Is.SameAs((object)references.Resource));
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Press(keyboard.rKey);
		yield return null;
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True);
		Release(keyboard.rKey);
		yield return null;
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True, "Resource mining continues after the control is released.", Array.Empty<object>());
		// W-001: Das Sprite-Billboard entfaellt bei der 3D-Figur — das Werkzeug
		// kommt als Mesh aus der Wanderer.glb; der aufgeloeste Werkzeug-Typ
		// bleibt fuer den Animator verfuegbar.
		Assert.That<bool>(references.Player.VisualAnimator.HarvestVisual.ToolVisible, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(references.Player.VisualAnimator.HarvestVisual.LastResolvedToolItemId, (IResolveConstraint)(object)Is.EqualTo((object)"pickaxe"));
		InputSystem.RemoveDevice(keyboard);
		references.Controller.RefreshTargetsNow();
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		Press(gamepad.buttonWest);
		yield return null;
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True);
		Release(gamepad.buttonWest);
		yield return null;
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.False);
		InputSystem.RemoveDevice(gamepad);
		references.Controller.RefreshTargetsNow();
		touch.SetAvailable(available: true);
		touch.OnPointerDown(null);
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True);
		touch.OnPointerUp(null);
		Assert.That<bool>(input.InteractHeld, (IResolveConstraint)(object)Is.False);
	}

	[UnityTest]
	public IEnumerator CopperVein_Hold225SecondsCollectsExactlyOnce()
	{
		yield return LoadCopperZone();
		TestReferences references = PrepareOreInteraction();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		int copperBefore = inventory.GetTotalAmount("copper_ore");
		int minedEvents = 0;
		references.Resource.Mined += delegate
		{
			minedEvents++;
		};
		Assert.That<string>(references.Resource.Definition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"resource.copper_vein"));
		Assert.That<string>(references.Resource.Definition.OutputItem.Id, (IResolveConstraint)(object)Is.EqualTo((object)"copper_ore"));
		references.Input.SetVirtualInteract(held: true);
		float elapsed;
		for (elapsed = 0f; elapsed < 6f; elapsed += Time.deltaTime)
		{
			if (references.Resource.IsMined)
			{
				break;
			}
			yield return null;
		}
		// Zustand und Abbruchgrund mitgeben: Ein einziger Abbruch (z. B. Schaden
		// durch ein zugewandertes Eidra) sperrt den Rest des Zeitfensters, weil
		// _requiresRelease einen Neustart bei gehaltener Taste verhindert. Ohne
		// diese Angaben ist so ein Fehlschlag im Nachhinein nicht deutbar.
		Assert.That<bool>(references.Resource.IsMined, (IResolveConstraint)(object)Is.True, $"Nach {elapsed:0.00} s Halten wurde nicht abgebaut (Zustand {references.Controller.State}, Ziel {references.Controller.CurrentTarget?.InteractionId ?? "-"}, letzter Abbruchgrund {references.Controller.LastCancelReason}).", Array.Empty<object>());
		Assert.That<float>(elapsed, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)2.2f));
		Assert.That<float>(elapsed, (IResolveConstraint)(object)Is.LessThan((object)2.7f));
		Assert.That<int>(minedEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(inventory.GetTotalAmount("copper_ore") - copperBefore, (IResolveConstraint)(object)Is.EqualTo((object)references.Resource.CollectedAmount));
		Assert.That<int>(references.Resource.CollectedAmount, (IResolveConstraint)(object)Is.EqualTo((object)2));
		for (int i = 0; i < 10; i++)
		{
			yield return null;
		}
		Assert.That<int>(minedEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
		references.Input.SetVirtualInteract(held: false);
	}

	[UnityTest]
	public IEnumerator CopperVein_IsGatedByThePickaxeAndThenYieldsTwo()
	{
		yield return LoadCopperZone();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ResourceNode resource = FindCopperVein();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ResourceNode>(resource, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(resource.Definition.RequiresTool, (IResolveConstraint)(object)Is.True);
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		string[] array = new string[3] { "pickaxe", "copper_pickaxe", "iron_pickaxe" };
		foreach (string pickaxeId in array)
		{
			int carried = inventory.GetTotalAmount(pickaxeId);
			if (carried > 0)
			{
				inventory.Remove(pickaxeId, carried);
			}
		}
		InteractionContext context = new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory);
		Assert.That<bool>(resource.CanInteract(in context, out var blockedReason), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(blockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"WERKZEUG UNZUREICHEND"));
		Assert.That<int>(resource.ResolveYield(in context), (IResolveConstraint)(object)Is.Zero);
		InteractionSnapshot snapshot = InteractionSnapshot.Empty;
		player.Interaction.SnapshotChanged += delegate(InteractionSnapshot value)
		{
			snapshot = value;
		};
		player.Motor.Teleport(resource.transform.position + Vector3.back * 1.5f + Vector3.up * 0.05f);
		Physics.SyncTransforms();
		player.Interaction.RefreshTargetsNow();
		yield return null;
		Assert.That<InteractionState>(player.Interaction.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Blocked));
		Assert.That<string>(snapshot.BlockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"WERKZEUG UNZUREICHEND"));
		inventory.Add("pickaxe", 1);
		Assert.That<bool>(resource.CanInteract(in context, out var _), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(resource.ResolveYield(in context), (IResolveConstraint)(object)Is.EqualTo((object)2));
		int before = inventory.GetTotalAmount("copper_ore");
		resource.CompleteInteraction(in context);
		Assert.That<int>(inventory.GetTotalAmount("copper_ore") - before, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(resource.CollectedAmount, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(inventory.GetTotalAmount("pickaxe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[UnityTest]
	public IEnumerator AttackDodgeDamageSceneAndInput_CancelActiveHold()
	{
		yield return LoadCopperZone();
		TestReferences references = PrepareOreInteraction();
		Assert.That<bool>(EidrenServiceRoot.Instance.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(EidrenServiceRoot.Instance.ContentDatabase.GetItem("hammer"), 1), out var equipError), (IResolveConstraint)(object)Is.True, equipError, Array.Empty<object>());
		yield return null;
		yield return BeginPartialHold(references);
		references.Input.PressAttack();
		yield return null;
		AssertCancelled(references, InteractionCancelReason.AttackStarted);
		references.Input.SetVirtualInteract(held: false);
		yield return WaitUntil(() => !references.Player.Combat.IsAttacking);
		references.Controller.RefreshTargetsNow();
		yield return BeginPartialHold(references);
		references.Input.PressDodge();
		yield return null;
		AssertCancelled(references, InteractionCancelReason.DodgeStarted);
		references.Input.SetVirtualInteract(held: false);
		yield return WaitUntil(() => !references.Player.Motor.IsDodging);
		references.Controller.RefreshTargetsNow();
		yield return BeginPartialHold(references);
		references.Player.Damageable.ApplyDamage(new DamageInfo(1f, 0f, references.Player.transform.position, references.Resource.gameObject, isBackAttack: false, "test.interaction.damage", "test"));
		// Damageable meldet synchron — der Abbruch steht, bevor ein Bild vergeht.
		AssertCancelled(references, InteractionCancelReason.PlayerDamaged);
		// Seit 16.08.2026: Der Treffer kostet den Fortschritt, aber nicht die
		// Absicht. Die gehaltene Taste setzt den Abbau von selbst wieder an;
		// frueher stand der Spieler bis zum erneuten Druecken still.
		yield return WaitUntil(() => references.Controller.IsInteracting);
		Assert.That<bool>(references.Resource.IsMined, (IResolveConstraint)(object)Is.False, "Der Abbau setzt neu an, er springt nicht ans Ende.", Array.Empty<object>());
		references.Input.SetVirtualInteract(held: false);
		references.Controller.RefreshTargetsNow();
		yield return BeginPartialHold(references);
		references.Controller.CancelForSceneTransition();
		yield return null;
		AssertCancelled(references, InteractionCancelReason.SceneTransition);
		references.Input.SetVirtualInteract(held: false);
		references.Controller.RefreshTargetsNow();
		yield return BeginPartialHold(references);
		references.Input.SetGameplayEnabled(enabled: false);
		yield return null;
		AssertCancelled(references, InteractionCancelReason.InputDisabled);
	}

	[UnityTest]
	public IEnumerator DamageDuringHold_ResumesWithoutReleasingTheButton()
	{
		yield return LoadCopperZone();
		TestReferences references = PrepareOreInteraction();
		yield return BeginPartialHold(references);
		references.Player.Damageable.ApplyDamage(new DamageInfo(1f, 0f, references.Player.transform.position, references.Resource.gameObject, isBackAttack: false, "test.interaction.resume", "test"));
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.False, "Der Treffer muss den laufenden Abbau abbrechen.", Array.Empty<object>());
		// Ab hier wird die Taste NICHT losgelassen. Vor dem 16.08.2026 sperrte
		// _requiresRelease den Neustart, der Abbau kam nie wieder in Gang.
		float elapsed;
		for (elapsed = 0f; elapsed < 6f; elapsed += Time.deltaTime)
		{
			if (references.Resource.IsMined)
			{
				break;
			}
			yield return null;
		}
		Assert.That<bool>(references.Resource.IsMined, (IResolveConstraint)(object)Is.True, $"Nach {elapsed:0.00} s gehaltener Taste wurde nach dem Treffer nicht weiter abgebaut (Zustand {references.Controller.State}, letzter Abbruchgrund {references.Controller.LastCancelReason}).", Array.Empty<object>());
		references.Input.SetVirtualInteract(held: false);
	}

	[UnityTest]
	public IEnumerator PlayerDeath_CancelsActiveHold()
	{
		yield return LoadCopperZone();
		TestReferences references = PrepareOreInteraction();
		yield return BeginPartialHold(references);
		references.Player.Damageable.ApplyDamage(new DamageInfo(references.Player.Damageable.MaxHealth * 10f, 0f, references.Player.transform.position, references.Resource.gameObject, isBackAttack: false, "test.interaction.lethal", "test"));
		yield return null;
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.False);
		Assert.That<InteractionCancelReason>(references.Controller.LastCancelReason, (IResolveConstraint)(object)Is.EqualTo((object)InteractionCancelReason.PlayerDied));
		Assert.That<bool>(references.Resource.IsMined, (IResolveConstraint)(object)Is.False);
	}

	private static IEnumerator BeginPartialHold(TestReferences references)
	{
		references.Input.SetVirtualInteract(held: true);
		yield return null;
		yield return null;
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(references.Controller.Progress, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
	}

	private static void AssertCancelled(TestReferences references, InteractionCancelReason reason)
	{
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.False);
		Assert.That<InteractionCancelReason>(references.Controller.LastCancelReason, (IResolveConstraint)(object)Is.EqualTo((object)reason));
		Assert.That<bool>(references.Resource.IsMined, (IResolveConstraint)(object)Is.False);
		Assert.That<float>(references.Resource.Progress, (IResolveConstraint)(object)Is.Zero);
	}

	[UnityTest]
	public IEnumerator CopperMining_ZeigtWerkzeugMeshMitAbbauClipStattSpriteWerkzeug()
	{
		// W-001: Der Abbau spielt den Abbau-Clip der Wanderer.glb und blendet
		// das Werkzeug-Mesh ein; das alte 2D-Sprite-Werkzeug bleibt unterdrueckt.
		yield return LoadCopperZone();
		TestReferences references = PrepareOreInteraction();
		references.Input.SetVirtualInteract(held: true);
		float elapsed = 0f;
		while (!references.Controller.IsInteracting && elapsed < 2f)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}
		Assert.That<bool>(references.Controller.IsInteracting, (IResolveConstraint)(object)Is.True, "Abbau ist nicht angelaufen", Array.Empty<object>());
		yield return null;
		yield return null;
		Animation animationPlayer = references.Player.GetComponentInChildren<Animation>(true);
		Assert.That<Animation>(animationPlayer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(animationPlayer.IsPlaying("Abbau_Spitzhacke"), (IResolveConstraint)(object)Is.True, "Abbau_Spitzhacke laeuft nicht", Array.Empty<object>());
		Assert.That<bool>(FindDeepNode(references.Player.transform, "Waffe_Spitzhacke").gameObject.activeSelf, (IResolveConstraint)(object)Is.True, "Low-Poly-Spitzhacke fehlt waehrend des Abbaus", Array.Empty<object>());
		Assert.That<bool>(references.Player.VisualAnimator.HarvestVisual.IsHarvesting, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(references.Player.VisualAnimator.HarvestVisual.ToolVisible, (IResolveConstraint)(object)Is.False, "Sprite-Werkzeug muss bei der 3D-Figur unterdrueckt bleiben", Array.Empty<object>());
		references.Input.SetVirtualInteract(held: false);
		references.Player.Motor.Teleport(references.Player.transform.position + Vector3.back * 10f);
		Physics.SyncTransforms();
		references.Controller.RefreshTargetsNow();
		yield return null;
		yield return null;
		Assert.That<bool>(FindDeepNode(references.Player.transform, "Waffe_Spitzhacke").gameObject.activeSelf, (IResolveConstraint)(object)Is.False, "Low-Poly-Spitzhacke muss nach dem Abbau wieder weg sein", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator ChestOpening_SpieltOeffnenClipUndZeigtKeineSpriteHaende()
	{
		// W-009: Die Kistenoeffnung erzaehlt die kniende 3D-Figur mit dem
		// Oeffnen-Clip; die alten 2D-Handsprites existieren nicht mehr.
		yield return LoadCopperZone();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		WorldChestContainer chest = null;
		WorldChestContainer[] chests = UnityEngine.Object.FindObjectsByType<WorldChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (WorldChestContainer candidate in chests)
		{
			if (candidate.Status == WorldChestStatus.Closed)
			{
				chest = candidate;
				break;
			}
		}
		Assert.That<WorldChestContainer>(chest, (IResolveConstraint)(object)Is.Not.Null, "Keine geschlossene Gebietskiste in der Zone", Array.Empty<object>());
		Assert.That<UnityEngine.SpriteRenderer[]>(chest.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true), (IResolveConstraint)(object)Is.Empty, "Kiste traegt noch Sprite-Haende", Array.Empty<object>());

		player.Motor.Teleport(chest.transform.position + Vector3.back * 1.4f + Vector3.up * 0.05f);
		Physics.SyncTransforms();
		player.Interaction.RefreshTargetsNow();
		Assert.That<IInteractable>(player.Interaction.CurrentTarget, (IResolveConstraint)(object)Is.SameAs((object)chest));

		input.SetVirtualInteract(held: true);
		float elapsed = 0f;
		while (!player.Interaction.IsInteracting && elapsed < 2f)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}
		Assert.That<bool>(player.Interaction.IsInteracting, (IResolveConstraint)(object)Is.True, "Kistenoeffnung ist nicht angelaufen", Array.Empty<object>());
		input.SetVirtualInteract(held: false);
		yield return null;
		yield return null;
		Animation animationPlayer = player.GetComponentInChildren<Animation>(true);
		Assert.That<Animation>(animationPlayer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(animationPlayer.IsPlaying("Oeffnen"), (IResolveConstraint)(object)Is.True, "Oeffnen-Clip laeuft nicht waehrend der Kistenoeffnung", Array.Empty<object>());

		player.Motor.Teleport(player.transform.position + Vector3.back * 10f);
		Physics.SyncTransforms();
		player.Interaction.RefreshTargetsNow();
		// Crossfade abwarten: der Clip blendet ueber ~0,12 s aus.
		float fade = 0f;
		while (animationPlayer.IsPlaying("Oeffnen") && fade < 1f)
		{
			fade += Time.deltaTime;
			yield return null;
		}
		Assert.That<bool>(animationPlayer.IsPlaying("Oeffnen"), (IResolveConstraint)(object)Is.False, "Oeffnen-Clip muss nach dem Abbruch enden", Array.Empty<object>());
	}

	private static Transform FindDeepNode(Transform root, string childName)
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

	private static TestReferences PrepareOreInteraction()
	{
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		ResourceNode resource = FindCopperVein();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ResourceNode>(resource, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<InteractionController>(player.Interaction, (IResolveConstraint)(object)Is.Not.Null);
		if (resource.Definition != null && resource.Definition.RequiresTool)
		{
			PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
			string toolId = resource.Definition.RequiredToolItemId;
			if (!inventory.Contains(toolId))
			{
				inventory.Add(toolId, 1);
			}
		}
		player.Motor.Teleport(resource.transform.position + Vector3.back * 1.5f + Vector3.up * 0.05f);
		Physics.SyncTransforms();
		player.Interaction.RefreshTargetsNow();
		Assert.That<IInteractable>(player.Interaction.CurrentTarget, (IResolveConstraint)(object)Is.SameAs((object)resource));
		return new TestReferences(player, input, resource, player.Interaction);
	}

	// Die Glutruinen werden pro Lauf neu ausgewuerfelt: ZoneStateService zieht
	// seine Saat aus einer ungesetzten Zufallsquelle, und ZoneLayoutGenerator
	// haelt die Standorte der Welttruhen nicht frei. Ein Kupfervorkommen kann
	// deshalb dicht neben einer bewachten Kiste landen, deren Interaktionsradius
	// den Abbauplatz dieses Tests mit erfasst. Wir suchen darum ein freistehendes
	// Vorkommen, statt auf die Saat zu hoffen (Fehlschlag vom 15.08.2026).
	// Beide Kupfertests stellen den Spieler 1,5 m hinter das Vorkommen.
	private static readonly Vector3 TestStandOffset = Vector3.back * 1.5f;

	// Kleiner Zuschlag: Teleport und Character-Controller setzen den Spieler nicht
	// aufs Tausendstel genau ab.
	private const float CompetitorMargin = 0.5f;

	private static ResourceNode FindCopperVein()
	{
		ResourceNode[] array = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		ResourceNode fallback = null;
		foreach (ResourceNode candidate in array)
		{
			if (candidate.Definition != null && candidate.Definition.Id == "resource.copper_vein")
			{
				if (fallback == null)
				{
					fallback = candidate;
				}
				if (IsClearOfCompetitors(candidate))
				{
					return candidate;
				}
			}
		}
		return fallback;
	}

	// Ein anderer Interaktionspunkt stoert genau dann, wenn er vom Teststandort aus
	// in seiner EIGENEN Reichweite liegt - dasselbe Kriterium, das der
	// InteractionTargetSelector anlegt. Dann zieht er das Ziel an sich; ist das
	// Vorkommen zusaetzlich gesperrt (kein Spitzhacke-Test), kippt der Zustand von
	// Blocked auf Available, weil ein bedienbarer Kandidat einen gesperrten
	// grundsaetzlich schlaegt. Ein pauschaler Radius taugt hier nicht: die Knoten
	// stehen ohnehin nur ~3,25 m auseinander, damit wuerde jedes Vorkommen
	// verworfen.
	private static bool IsClearOfCompetitors(ResourceNode vein)
	{
		Vector3 stand = vein.transform.position + TestStandOffset;
		MonoBehaviour[] array = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (MonoBehaviour behaviour in array)
		{
			if (behaviour == null || (object)behaviour == (object)vein)
			{
				continue;
			}
			if (behaviour is IInteractable other && InteractionUtility.IsActive(other) && InteractionUtility.FlatDistanceToBody(stand, other) <= other.InteractionRange + CompetitorMargin)
			{
				return false;
			}
		}
		return true;
	}

	[OneTimeTearDown]
	public void KlassenHinterlassenschaftRaeumen()
	{
		// Diese Klasse toetet den Spieler absichtlich
		// (PlayerDeath_CancelsActiveHold). Der DontDestroyOnLoad-Dienstbaum
		// wuerde den toten Zustand sonst in die naechste Testklasse tragen
		// und dort jeden Spawn sperren (Kaskaden-Mechanik aus der
		// Vergiftungsjagd vom 12.08.2026). LoadCopperZone schuetzt nur uns
		// selbst (Root-Destroy VOR dem Laden) — hier raeumen wir auch hinter
		// uns auf.
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.DestroyImmediate(EidrenServiceRoot.Instance.gameObject);
		}
	}

	private static IEnumerator LoadCopperZone()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		AsyncOperation load = SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int i = 0; i < 3; i++)
		{
			yield return null;
		}
		// Seit dem Leinen-Fix wittern patrouillierende Gegner den Spieler —
		// Treffer brechen Halte-Interaktionen und verfaelschen Zeitmessungen.
		// Diese Klasse prueft Interaktion, nicht Kampf.
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		if (zone?.EnemyPopulator != null)
		{
			foreach (Eidren.AI.WildlingController gegner in zone.EnemyPopulator.Spawned)
			{
				if (gegner != null)
				{
					gegner.gameObject.SetActive(value: false);
				}
			}
		}
		for (int i = 0; i < 180; i++)
		{
			if (FindCopperVein() != null)
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail("The Ember Ruins copper vein did not spawn.");
	}

	private static IEnumerator WaitUntil(Func<bool> predicate)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			if (predicate())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail("Condition was not reached in 180 frames.");
	}
}
}
