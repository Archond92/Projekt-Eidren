using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
using Eidren.Player;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class WildEidraIntegrationTests
{
	[UnityTest]
	public IEnumerator TheQuarryCarriesNeutralTerrockFromTheSeed()
	{
		yield return FreshZone("Zone_Quarry");
		EidraWildController[] eidra = FindEidra();
		Assert.That<EidraWildController[]>(eidra, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2), "M8.3: Feste Anzahl je Gebiet, aus enemyAllocations.", Array.Empty<object>());
		EidraWildController[] array = eidra;
		foreach (EidraWildController obj in array)
		{
			Assert.That<EidraData>(obj.Eidra, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<string>(obj.Eidra.Id, (IResolveConstraint)(object)Is.EqualTo((object)"terrock"), "Der Steinbruch ist Terrocks Heimatgebiet.", Array.Empty<object>());
			Assert.That<bool>(obj.IsNeutral, (IResolveConstraint)(object)Is.True, "Eidra greifen nicht von sich aus an (M8.3).", Array.Empty<object>());
			Assert.That<bool>(obj.HasDeparted, (IResolveConstraint)(object)Is.False);
			Assert.That<string>(obj.InstanceId, (IResolveConstraint)(object)Is.Not.Empty, "§12: Die Zoneninstanz ist ein Save-Key.", Array.Empty<object>());
			Assert.That<EidraCaptureTarget>(obj.GetComponent<EidraCaptureTarget>(), (IResolveConstraint)(object)Is.Not.Null);
			AssertCanonicalSpriteVisual(obj, "terrock");
		}
		Assert.That<int>(eidra.Select((EidraWildController wild) => wild.InstanceId).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[UnityTest]
	public IEnumerator GreenwoodCarriesNoEidraAtAll()
	{
		yield return FreshZone("Zone_Greenwood");
		Assert.That<EidraWildController[]>(FindEidra(), (IResolveConstraint)(object)Is.Empty);
	}

	[UnityTest]
	public IEnumerator TheMarshUsesCanonicalNoctarion3DVisuals()
	{
		yield return FreshZone("Zone_Marsh");
		EidraWildController[] array = (from wild in FindEidra()
			where wild.Eidra?.Id == "noctarion"
			select wild).ToArray();
		Assert.That<EidraWildController[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2));
		EidraWildController[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			AssertCanonicalSpriteVisual(array2[num], "noctarion");
		}
	}

	[UnityTest]
	public IEnumerator WithoutEquipment_TheCaptureNamesWhatIsMissing()
	{
		yield return FreshZone("Zone_Quarry");
		Assert.That<bool>(FindTarget().CanInteract(Context(), out var blockedReason), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(blockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"FANGGERÄT FEHLT"), "Der Grund steht VOR dem Halten in der Anzeige.", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator AtFullHealth_TheChargeIsTooLowAndNamesBothLevers()
	{
		yield return FreshZone("Zone_Quarry");
		EquipCaptureGear();
		Assert.That<bool>(FindTarget().CanInteract(Context(), out var blockedReason), (IResolveConstraint)(object)Is.False, "Ein unversehrter Terrock verlangt mehr als eine T1-Batterie liefert.", Array.Empty<object>());
		Assert.That<string>(blockedReason, (IResolveConstraint)(object)Does.Contain("LADUNG"));
		Assert.That<string>(blockedReason, (IResolveConstraint)(object)Does.Contain("BEDARF"));
	}

	[UnityTest]
	public IEnumerator TheVisibleCaptureDemandFallsWithHealth()
	{
		yield return FreshZone("Zone_Quarry");
		EidraWildController wild = FindEidra()[0];
		EidraCaptureTarget target = wild.GetComponent<EidraCaptureTarget>();
		float before = CaptureResolution.RequirementAt(wild.Eidra.Capture, wild.HealthFraction);
		Assert.That<string>(target.DisplayText, (IResolveConstraint)(object)Does.Contain($"BEDARF {before:0}"));
		Weaken(wild, 20f);
		yield return null;
		float after = CaptureResolution.RequirementAt(wild.Eidra.Capture, wild.HealthFraction);
		Assert.That<float>(after, (IResolveConstraint)(object)Is.LessThan((object)before));
		Assert.That<string>(target.DisplayText, (IResolveConstraint)(object)Does.Contain($"BEDARF {after:0}"), "Der sinkende Fangbedarf steht sichtbar in der Interaktionsanzeige.", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator ACancelledChannelCostsNothing()
	{
		yield return FreshZone("Zone_Quarry");
		EquipCaptureGear();
		EidraWildController wild = FindEidra()[0];
		Weaken(wild, 80f);
		yield return null;
		EidraCaptureTarget target = wild.GetComponent<EidraCaptureTarget>();
		Assert.That<bool>(target.CanInteract(Context(), out var _), (IResolveConstraint)(object)Is.True, "Geschwaecht deckt die Ladung den Bedarf.", Array.Empty<object>());
		InteractionContext context = Context();
		PlayerMotor motor = Player().Motor;
		float healthBefore = Player().Damageable.CurrentHealth;
		Vector3 eidraPositionBefore = wild.transform.position;
		target.BeginInteraction(in context);
		Assert.That<bool>(motor.IsLocomotionLocked, (IResolveConstraint)(object)Is.True, "Während des Fangkanals steht der Spieler still.", Array.Empty<object>());
		Assert.That<bool>(wild.IsCaptureChannelActive, (IResolveConstraint)(object)Is.True);
		yield return new WaitForSeconds(0.75f);
		Assert.That<float>(Player().Damageable.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)healthBefore).Within((object)0.001f), "Das aktuell gefangene Eidra darf waehrend des Kanals keinen Schaden verursachen.", Array.Empty<object>());
		Assert.That<float>(Vector3.Distance(eidraPositionBefore, wild.transform.position), (IResolveConstraint)(object)Is.LessThan((object)0.05f), "Das Ziel bleibt waehrend des Fangkanals stehen.", Array.Empty<object>());
		target.UpdateInteraction(in context, 0.8f);
		target.CancelInteraction(in context, InteractionCancelReason.PlayerDamaged);
		Assert.That<bool>(motor.IsLocomotionLocked, (IResolveConstraint)(object)Is.False, "Ein Treffer gibt die Fortbewegung sofort wieder frei.", Array.Empty<object>());
		Assert.That<bool>(HasBattery(), (IResolveConstraint)(object)Is.True, "M8.3: Ein Abbruch kostet nichts.", Array.Empty<object>());
		Assert.That<bool>(wild.IsCaptureChannelActive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(wild.HasDeparted, (IResolveConstraint)(object)Is.False);
		Assert.That<EidraInstanceState[]>(Session().Eidra.GetAll(), (IResolveConstraint)(object)Is.Empty);
	}

	[UnityTest]
	public IEnumerator AFinishedChannelConsumesTheBatteryAndTakesTheEidra()
	{
		yield return FreshZone("Zone_Quarry");
		EquipCaptureGear();
		EidraWildController wild = FindEidra()[0];
		string instanceId = wild.InstanceId;
		Assert.That<bool>(EidrenServiceRoot.FindOrCreate().ContentDatabase.TryGetEidra("terrock", out var catalogTerrock), (IResolveConstraint)(object)Is.True, "Der zentrale Eidra-Katalog muss Save-IDs in jeder Zonenszene auflösen.", Array.Empty<object>());
		Assert.That<EidraData>(catalogTerrock, (IResolveConstraint)(object)Is.SameAs((object)wild.Eidra));
		Weaken(wild, 80f);
		yield return null;
		EidraCaptureTarget component = wild.GetComponent<EidraCaptureTarget>();
		component.BeginInteraction(Context());
		component.CompleteInteraction(Context());
		yield return null;
		Assert.That<bool>(wild.HasDeparted, (IResolveConstraint)(object)Is.True, "Das gefangene Eidra verlaesst die Zone.", Array.Empty<object>());
		Assert.That<bool>(HasBattery(), (IResolveConstraint)(object)Is.False, "Die Batterie wird bei Erfolg verbraucht.", Array.Empty<object>());
		EidraInstanceState[] all = Session().Eidra.GetAll();
		Assert.That<EidraInstanceState[]>(all, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<string>(all[0].EidraId, (IResolveConstraint)(object)Is.EqualTo((object)"terrock"));
		Assert.That<string[]>(Session().Eidra.GetActiveInstanceIds(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1), "Der erste Fang besetzt den einen offenen Platz.", Array.Empty<object>());
		PlayerPrefabBindings player = Player();
		Assert.That<string>(player.EidraTeam.ActiveData?.Id, (IResolveConstraint)(object)Is.EqualTo((object)"terrock"), "Der erste Fang wird noch in derselben Szene zum aktiven Begleiter.", Array.Empty<object>());
		Assert.That<GameObject>(GameObject.Find("Active_Eidra_" + player.EidraTeam.ActiveData.DisplayName), (IResolveConstraint)(object)Is.Not.Null, "M3: Das aktive Eidra steht sichtbar neben dem Spieler.", Array.Empty<object>());
		Assert.That<bool>(player.Motor.IsLocomotionLocked, (IResolveConstraint)(object)Is.False, "Der Commit beendet die Bewegungssperre.", Array.Empty<object>());
		Assert.That<bool>(Session().ZoneStates.TryGet("zone_quarry", out var state), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(state.IsHarvested(instanceId), (IResolveConstraint)(object)Is.True);
	}

	[UnityTest]
	public IEnumerator AnEidraNeverDropsBelowItsFleeThreshold()
	{
		yield return FreshZone("Zone_Quarry");
		EidraWildController wild = FindEidra()[0];
		float floor = CaptureResolution.FleeHealthFloor(wild.Eidra.Capture, wild.MaxHealth);
		Weaken(wild, 10000f);
		yield return null;
		Assert.That<float>(wild.CurrentHealth, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)(floor - 0.001f)));
		Assert.That<bool>(wild.IsAlive, (IResolveConstraint)(object)Is.True, "M8.3: Eidra sterben nicht -- sie fliehen.", Array.Empty<object>());
		Assert.That<bool>(wild.HasDeparted, (IResolveConstraint)(object)Is.True, "Unter der Fluchtschwelle bricht es ab und verschwindet.", Array.Empty<object>());
		Assert.That<EidraInstanceState[]>(Session().Eidra.GetAll(), (IResolveConstraint)(object)Is.Empty);
	}

	[UnityTearDown]
	public IEnumerator UnloadZone()
	{
		Scene cleanup = SceneManager.CreateScene($"WildEidraCleanup_{Guid.NewGuid():N}");
		SceneManager.SetActiveScene(cleanup);
		for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
		{
			Scene scene = SceneManager.GetSceneAt(index);
			if (!(scene == cleanup) && scene.isLoaded)
			{
				AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
				while (unload != null && !unload.isDone)
				{
					yield return null;
				}
			}
		}
		yield return null;
	}

	private static GameSession Session()
	{
		return EidrenServiceRoot.FindOrCreate().GameSession;
	}

	private static PlayerPrefabBindings Player()
	{
		ZonePlayerSpawner zonePlayerSpawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		Assert.That<PlayerPrefabBindings>(zonePlayerSpawner?.SpawnedPlayer, (IResolveConstraint)(object)Is.Not.Null);
		return zonePlayerSpawner.SpawnedPlayer;
	}

	private static EidraWildController[] FindEidra()
	{
		return (from wild in UnityEngine.Object.FindObjectsByType<EidraWildController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			where !wild.HasDeparted
			select wild).OrderBy((EidraWildController wild) => wild.InstanceId, StringComparer.Ordinal).ToArray();
	}

	private static EidraCaptureTarget FindTarget()
	{
		EidraWildController[] array = FindEidra();
		Assert.That<EidraWildController[]>(array, (IResolveConstraint)(object)Is.Not.Empty);
		EidraCaptureTarget component = array[0].GetComponent<EidraCaptureTarget>();
		Assert.That<EidraCaptureTarget>(component, (IResolveConstraint)(object)Is.Not.Null);
		return component;
	}

	private static InteractionContext Context()
	{
		PlayerPrefabBindings player = Player();
		return new InteractionContext(player.gameObject, player.transform, player.transform.forward, Session().PlayerInventory);
	}

	private static void EquipCaptureGear()
	{
		PlayerEquipment playerEquipment = Session().PlayerEquipment;
		Assert.That<bool>(playerEquipment.TryEquip(EquipmentSlot.CatchDevice, new ItemStack("catch_device", 1), out var deviceError), (IResolveConstraint)(object)Is.True, deviceError, Array.Empty<object>());
		Assert.That<bool>(playerEquipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 1), out var batteryError), (IResolveConstraint)(object)Is.True, batteryError, Array.Empty<object>());
	}

	private static bool HasBattery()
	{
		ItemStack stack;
		return Session().PlayerEquipment.TryGetSlot(EquipmentSlot.Battery, out stack);
	}

	private static void Weaken(EidraWildController wild, float healthDamage)
	{
		wild.ApplyDamage(new DamageInfo(healthDamage, 0f, wild.transform.position + Vector3.up, wild.gameObject, isBackAttack: false, "test.weaken", "test"));
	}

	private static void AssertCanonicalSpriteVisual(EidraWildController wild, string actorId)
	{
		// Seit dem Kreaturen-Einbau laedt der Controller die 3D-Wrapper
		// (EidraVisual3DBuilder). Der kanonische Aufbau: KEINE Sprite-
		// Darstellung mehr (ein Nebeneinander hinge an der Kindreihenfolge),
		// dafuer die Kreaturenschicht plus der Animator, den der Controller
		// bindet. Die Kennungspruefung laeuft ueber den Wrapper-Namen, den
		// EnsureVisualVariant vom geladenen Prefab uebernimmt.
		Assert.That<SpriteActorPresentation>(
			wild.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true),
			(IResolveConstraint)(object)Is.Null,
			"Wild " + actorId + " traegt noch die Sprite-Darstellung.", Array.Empty<object>());
		CreatureMeshPresentation mesh = wild.GetComponentInChildren<CreatureMeshPresentation>(includeInactive: true);
		Assert.That<CreatureMeshPresentation>(mesh, (IResolveConstraint)(object)Is.Not.Null, "Wild " + actorId + " must reuse its canonical 3D wrapper prefab.", Array.Empty<object>());
		string erwarteterWrapper = char.ToUpperInvariant(actorId[0]) + actorId.Substring(1) + "_3D";
		Assert.That<Transform>(FindChild(wild.transform, erwarteterWrapper), (IResolveConstraint)(object)Is.Not.Null, "Wild " + actorId + ": Wrapper '" + erwarteterWrapper + "' fehlt.", Array.Empty<object>());
		Assert.That<SpriteActorAnimator>(wild.GetComponentInChildren<SpriteActorAnimator>(includeInactive: true), (IResolveConstraint)(object)Is.Not.Null);
	}

	private static Transform FindChild(Transform root, string name)
	{
		if (root.name == name)
		{
			return root;
		}
		for (int i = 0; i < root.childCount; i++)
		{
			Transform hit = FindChild(root.GetChild(i), name);
			if (hit != null)
			{
				return hit;
			}
		}
		return null;
	}

	private static IEnumerator FreshZone(string sceneName)
	{
		EidrenServiceRoot.FindOrCreate().GameSession.StartNewGame();
		AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 3; frame++)
		{
			yield return null;
		}
	}
}
}
