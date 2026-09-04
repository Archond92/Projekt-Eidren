using System.Collections;
using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
	public sealed class CombatTargetIndicatorPlayModeTests
	{
		[UnityTest]
		public IEnumerator RingFollowsGroundSwitchesTargetFadesAndSuppressesInteractionRing()
		{
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject owner = new GameObject("HUD100_RingOwner");
			GameObject indicatorObject = new GameObject("CombatTargetRing");
			GameObject firstObject = Target("HUD100_FirstTarget", new Vector3(0f, 1f, 3f));
			GameObject secondObject = Target("HUD100_SecondTarget", new Vector3(2f, 1f, 3f));
			GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Material material = null;
			try
			{
				floor.name = "HUD100_SlopedGround";
				floor.transform.position = new Vector3(0f, -.25f, 3f);
				floor.transform.localScale = new Vector3(10f, .5f, 10f);
				floor.transform.rotation = Quaternion.Euler(0f, 0f, 8f);
				indicatorObject.transform.SetParent(owner.transform, false);
				Shader shader = Shader.Find("Eidren/CombatTargetRing") ?? Shader.Find("Sprites/Default");
				material = new Material(shader);
				LineRenderer primary = Ring("Primary", indicatorObject.transform, material, 64);
				LineRenderer accent = Ring("Accent", indicatorObject.transform, material, 16);
				CombatTargetIndicator indicator = indicatorObject.AddComponent<CombatTargetIndicator>();
				indicator.ConfigurePrefabReferences(primary, accent);
				ResourceTargetIndicator interaction = owner.AddComponent<ResourceTargetIndicator>();
				PersistentCombatTargetQuery query = new PersistentCombatTargetQuery(owner.transform);
				indicator.Bind(query, interaction);
				Physics.SyncTransforms();
				query.Tick(Time.time, true);
				yield return WaitFor(() => indicator.Alpha > .95f, .5f);

				Assert.That(indicator.TargetIdentity, Is.EqualTo(firstObject.transform.GetInstanceID()));
				Assert.That(indicator.Category, Is.EqualTo(CombatTargetCategory.Normal));
				Assert.That(primary.enabled, Is.True);
				Assert.That(accent.enabled, Is.False);
				Assert.That(interaction.CombatSuppressed, Is.True);
				Assert.That(primary.sharedMaterial, Is.SameAs(accent.sharedMaterial));
				int indicatorId = indicator.GetInstanceID();
				int materialId = primary.sharedMaterial.GetInstanceID();

				Vector3 before = indicator.transform.position;
				firstObject.transform.position += Vector3.right;
				Physics.SyncTransforms();
				yield return null;
				Assert.That(indicator.transform.position.x, Is.GreaterThan(before.x));
				Assert.That(indicator.transform.position.x, Is.LessThan(firstObject.transform.position.x));
				yield return WaitFor(() => Vector3.Distance(indicator.transform.position, GroundPoint(firstObject.transform.position)) < .2f, .6f);
				Assert.That(Vector3.Angle(indicator.transform.up, Vector3.up), Is.GreaterThan(1f), "Ring muss der geneigten Bodenflaeche folgen.");

				firstObject.GetComponent<Damageable>().ApplyDamage(new DamageInfo(1000f, 0f, firstObject.transform.position, owner, false, "test", "test"));
				query.Tick(Time.time, true);
				yield return null;
				Assert.That(indicator.TargetIdentity, Is.EqualTo(secondObject.transform.GetInstanceID()));
				Assert.That(indicator.GetInstanceID(), Is.EqualTo(indicatorId));
				Assert.That(primary.sharedMaterial.GetInstanceID(), Is.EqualTo(materialId));

				secondObject.GetComponent<Damageable>().ApplyDamage(new DamageInfo(1000f, 0f, secondObject.transform.position, owner, false, "test", "test"));
				query.Tick(Time.time, true);
				yield return WaitFor(() => !indicator.IsVisible, .5f);
				Assert.That(primary.enabled, Is.False);
				Assert.That(interaction.CombatSuppressed, Is.False);
			}
			finally
			{
				Object.Destroy(owner); Object.Destroy(firstObject); Object.Destroy(secondObject);
				Object.Destroy(floor); Object.Destroy(material);
			}
			yield return null;
		}

		[UnityTest]
		public IEnumerator ActiveGaronReceivesBossVariantInProductionScene()
		{
			yield return Testumgebung.LeereWeltBereitstellen();
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			services.GameSession.StartNewGame();
			services.PlayerProgression.RecordEnemyDefeated(5100);
			yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
			for (int i = 0; i < 24; i++) yield return null;
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			BossController boss = Object.FindFirstObjectByType<BossAreaController>().ActiveBoss;
			Assert.That(player, Is.Not.Null);
			Assert.That(boss, Is.Not.Null);
			player.Motor.Teleport(boss.transform.position + Vector3.forward * 5f + Vector3.up * .05f);
			Physics.SyncTransforms();
			boss.BeginBattle();
			for (int i = 0; i < 12; i++) yield return null;
			player.Combat.Targeting.Tick(Time.time, true);
			CombatTargetSnapshot snapshot = player.Combat.Targeting.Current;
			Assert.That(snapshot.Identity, Is.EqualTo(boss.transform.GetInstanceID()),
				"Keine freie Arena-Sichtlinie lieferte Garon als aktives Combat Target.");
			CombatTargetIndicator indicator = player.GetComponentInChildren<CombatTargetIndicator>(true);
			yield return WaitFor(() => indicator.Alpha > .95f, .6f);
			Assert.That(indicator.TargetIdentity, Is.EqualTo(boss.transform.GetInstanceID()));
			Assert.That(indicator.Category, Is.EqualTo(CombatTargetCategory.Boss));
			Assert.That(indicator.PrimaryRing.enabled, Is.True);
			Assert.That(indicator.AccentRing.enabled, Is.True);
			yield return Testumgebung.LeereWeltBereitstellen();
			if (EidrenServiceRoot.Instance != null) Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}

		private static GameObject Target(string name, Vector3 position)
		{
			GameObject target = new GameObject(name);
			target.transform.position = position;
			CapsuleCollider collider = target.AddComponent<CapsuleCollider>();
			collider.height = 2f; collider.radius = .5f;
			Damageable health = target.AddComponent<Damageable>();
			health.Initialize(100f);
			return target;
		}

		private static LineRenderer Ring(string name, Transform parent, Material material, int points)
		{
			GameObject child = new GameObject(name);
			child.transform.SetParent(parent, false);
			LineRenderer ring = child.AddComponent<LineRenderer>();
			ring.sharedMaterial = material; ring.positionCount = points; ring.loop = true;
			ring.shadowCastingMode = ShadowCastingMode.Off; ring.receiveShadows = false;
			return ring;
		}

		private static Vector3 GroundPoint(Vector3 target) => new Vector3(target.x, .075f, target.z);

		private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
		{
			float deadline = Time.realtimeSinceStartup + timeout;
			while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
			Assert.That(condition(), Is.True, "Zeitlimit beim Warten auf den Combat Target Ring.");
		}
	}
}
