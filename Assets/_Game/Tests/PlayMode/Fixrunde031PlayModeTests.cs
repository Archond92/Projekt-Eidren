using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Nachweis zu F31-020 (FIXSAMMLUNG_V0.3.1.md): Nach einer Leine-Rückkehr
	/// muss der Gegner den Heimatpunkt AUS EIGENER KRAFT erreichen, heilen und
	/// bei erneuter Annäherung wieder angreifen. Der bestehende Leash-Test
	/// teleportiert den Gegner per Agent.Warp an den Heimatpunkt und verdeckt
	/// damit genau den Fehler, den dieser Test belegt: Die Stoppdistanz des
	/// Agenten (AttackRange − 0,1) liegt über der Ankunftstoleranz, der Agent
	/// bremst vor dem Ziel ab und die Rückkehr schließt nie ab.
	/// </summary>
	public sealed class Fixrunde031PlayModeTests
	{
		[UnityTest]
		public IEnumerator GegnerKehrtSelbststaendigHeimHeiltUndGreiftErneutAn()
		{
			// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
			ground.name = "F31020_Ground";
			ground.transform.position = new Vector3(0f, -0.5f, 0f);
			ground.transform.localScale = new Vector3(40f, 1f, 40f);
			Physics.SyncTransforms();
			GameObject navigationRoot = new GameObject("F31020_Navigation");
			NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
			surface.collectObjects = CollectObjects.All;
			surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
			surface.BuildNavMesh();
			GameObject player = new GameObject("F31020_Player");
			player.transform.position = new Vector3(0f, 0f, 6f);
			CapsuleCollider playerCollider = player.AddComponent<CapsuleCollider>();
			playerCollider.radius = 0.48f;
			playerCollider.height = 2f;
			playerCollider.center = Vector3.up;
			Damageable playerHealth = player.AddComponent<Damageable>();
			playerHealth.Initialize(500f);
			GameObject wildlingObject = new GameObject("F31020_Wildling");
			wildlingObject.AddComponent<CapsuleCollider>();
			GameObject visual = new GameObject("Visual");
			visual.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
			GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
			telegraph.name = "Telegraph";
			telegraph.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
			UnityEngine.Object.Destroy(telegraph.GetComponent<Collider>());
			telegraph.SetActive(value: false);
			GameObject hitboxObject = new GameObject("AttackHitbox");
			hitboxObject.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
			BoxCollider attackCollider = hitboxObject.AddComponent<BoxCollider>();
			EnemyMeleeHitbox enemyHitbox = wildlingObject.AddComponent<EnemyMeleeHitbox>();
			enemyHitbox.Configure(attackCollider);
			EnemyDefinition definition = Definition();
			WildlingController wildling = wildlingObject.AddComponent<WildlingController>();
			wildling.ConfigurePrefab(definition, enemyHitbox, telegraph, visual.transform);
			wildling.Initialize(player.transform, playerHealth);
			try
			{
				// Aggro über Schaden, dann den Spieler über die Leine hinaus ziehen.
				wildling.ApplyDamage(new DamageInfo(40f, 20f, wildling.transform.position, player, isBackAttack: false, "test.damage", "test"));
				yield return WaitForState(wildling, EnemyState.Chase, 300);
				player.transform.position = new Vector3(0f, 0f, 12f);
				yield return WaitForState(wildling, EnemyState.Return, 300);
				// Kern des Nachweises: KEIN Warp — der Gegner muss selbst ankommen.
				yield return WaitForState(wildling, EnemyState.Idle, 600);
				Assert.That(wildling.CurrentHealth, Is.EqualTo(wildling.MaxHealth),
					"Nach der Rückkehr muss der Gegner vollständig geheilt sein.");
				// Erneute Annäherung unter den Erkennungsradius: Gegner greift wieder an.
				player.transform.position = new Vector3(0f, 0f, 3f);
				yield return WaitForState(wildling, EnemyState.Chase, 600);
			}
			finally
			{
				if (surface != null)
				{
					surface.RemoveData();
				}
				UnityEngine.Object.Destroy(definition);
				UnityEngine.Object.Destroy(navigationRoot);
				UnityEngine.Object.Destroy(ground);
				UnityEngine.Object.Destroy(wildlingObject);
				UnityEngine.Object.Destroy(player);
			}
			yield return null;
		}

		private static EnemyDefinition Definition()
		{
			EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
			Set(definition, "id", "enemy.wildling.f31020");
			Set(definition, "displayName", "Wildling F31-020");
			Set(definition, "maximumHealth", 180f);
			Set(definition, "maximumStagger", 90f);
			Set(definition, "staggerDuration", 0.08f);
			Set(definition, "attackDamage", 5f);
			Set(definition, "experienceReward", 1);
			Set(definition, "telegraphDuration", 0.05f);
			Set(definition, "attackWindowDuration", 0.18f);
			Set(definition, "recoveryDuration", 0.08f);
			Set(definition, "deathDisableDelay", 10f);
			// Werte wie im Spiel entscheidend: Stoppdistanz = AttackRange − 0,1
			// = 1,6 liegt ÜBER der Ankunftstoleranz 0,4/0,55.
			Set(definition, "navigation", new EnemyNavigationData
			{
				MoveSpeed = 3.6f,
				Acceleration = 14f,
				AngularSpeed = 360f,
				AttackRange = 1.7f,
				DetectionRange = 8f,
				LeashRange = 8f,
				LeashFollowDistance = 1f,
				PatrolRadius = 0f,
				PatrolWait = 0.01f,
				AlertDuration = 0.02f,
				ReturnTolerance = 0.55f,
				NavMeshSampleDistance = 3f,
				AgentRadius = 0.52f,
				AgentHeight = 2.1f
			});
			return definition;
		}

		private static void Set(EnemyDefinition target, string fieldName, object value)
		{
			typeof(EnemyDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
		}

		private static IEnumerator WaitForState(WildlingController wildling, EnemyState expected, int maximumFrames)
		{
			for (int frame = 0; frame < maximumFrames; frame++)
			{
				if (wildling.EnemyState == expected)
				{
					yield break;
				}
				yield return null;
			}
			Assert.Fail($"Wildling erreichte {expected} nicht; aktueller Zustand: {wildling.EnemyState}.");
		}
	}
}
