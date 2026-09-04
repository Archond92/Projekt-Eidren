using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
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
	/// F31-016b: Eidra-Fähigkeiten funktionieren gegen normale Gegner —
	/// nicht nur gegen die zwei Bosse. Terrocks Felsbrecher gegen einen
	/// Wildling ist der Nachweis: kein „KEIN GÜLTIGES ZIEL", und der
	/// Stagger des Ziels steigt.
	/// </summary>
	public sealed class Fixrunde031EidraTests
	{
		[UnityTest]
		public IEnumerator Felsbrecher_TrifftEinenNormalenGegner()
		{
			// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
			ground.name = "F31016b_Ground";
			ground.transform.position = new Vector3(0f, -0.5f, 0f);
			ground.transform.localScale = new Vector3(40f, 1f, 40f);
			Physics.SyncTransforms();
			GameObject navigationRoot = new GameObject("F31016b_Navigation");
			NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
			surface.collectObjects = CollectObjects.All;
			surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
			surface.BuildNavMesh();
			GameObject playerObject = new GameObject("F31016b_Player");
			GameObject cameraObject = new GameObject("F31016b_Camera");
			GameObject wildlingObject = new GameObject("F31016b_Wildling");
			try
			{
				playerObject.transform.position = Vector3.zero;
				playerObject.AddComponent<CharacterController>();
				Damageable health = playerObject.AddComponent<Damageable>();
				health.Initialize(100f);
				PlayerInputReader input = playerObject.AddComponent<PlayerInputReader>();
				PlayerMotor motor = playerObject.AddComponent<PlayerMotor>();
				Camera camera = cameraObject.AddComponent<Camera>();
				cameraObject.tag = "MainCamera";
				motor.Initialize(input, camera, health, Vector3.zero, 3f);
				// Wildling in Fähigkeitsreichweite (Felsbrecher: 8) aufbauen.
				wildlingObject.transform.position = new Vector3(0f, 0f, 5f);
				wildlingObject.AddComponent<CapsuleCollider>();
				GameObject visual = new GameObject("Visual");
				visual.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
				GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
				telegraph.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
				Object.Destroy(telegraph.GetComponent<Collider>());
				telegraph.SetActive(value: false);
				GameObject hitboxObject = new GameObject("AttackHitbox");
				hitboxObject.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
				BoxCollider attackCollider = hitboxObject.AddComponent<BoxCollider>();
				EnemyMeleeHitbox enemyHitbox = wildlingObject.AddComponent<EnemyMeleeHitbox>();
				enemyHitbox.Configure(attackCollider);
				EnemyDefinition definition = Definition();
				WildlingController wildling = wildlingObject.AddComponent<WildlingController>();
				wildling.ConfigurePrefab(definition, enemyHitbox, telegraph, visual.transform);
				wildling.Initialize(playerObject.transform, health);
				yield return null;
				EidraData terrock = Resources.Load<EidraData>("Data/Eidren/Terrock");
				if (terrock == null)
				{
					terrock = FindTerrock();
				}
				Assert.That(terrock, Is.Not.Null, "Terrock-Daten nicht gefunden.");
				Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
				EidraTeamController team = playerObject.AddComponent<EidraTeamController>();
				team.Initialize(input, motor, health, null, terrock, null, material, material);
				string failure = null;
				team.AbilityFailed += (index, reason) =>
				{
					if (index == 0)
					{
						failure = reason;
					}
				};
				float staggerVorher = wildling.CurrentStagger;
				input.PressSkill(0);
				yield return null;
				Assert.That(failure, Is.Not.EqualTo("KEIN GÜLTIGES ZIEL"),
					"Der Felsbrecher muss einen normalen Gegner als Ziel finden.");
				// Abklingen der Cast-Zeit in Spielzeit abwarten, dann Wirkung prüfen.
				float zeit = 0f;
				while (wildling.CurrentStagger <= staggerVorher && zeit < 6f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Assert.That(wildling.CurrentStagger, Is.GreaterThan(staggerVorher),
					"Der Felsbrecher muss den Stagger des Gegners erhöhen.");
			}
			finally
			{
				if (surface != null)
				{
					surface.RemoveData();
				}
				Object.Destroy(navigationRoot);
				Object.Destroy(ground);
				Object.Destroy(wildlingObject);
				Object.Destroy(playerObject);
				Object.Destroy(cameraObject);
			}
			yield return null;
		}

		private static EidraData FindTerrock()
		{
#if UNITY_EDITOR
			return UnityEditor.AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset");
#else
			return null;
#endif
		}

		private static EnemyDefinition Definition()
		{
			EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
			Set(definition, "id", "enemy.wildling.f31016b");
			Set(definition, "displayName", "Wildling F31-016b");
			Set(definition, "maximumHealth", 180f);
			Set(definition, "maximumStagger", 90f);
			Set(definition, "staggerDuration", 0.08f);
			Set(definition, "attackDamage", 5f);
			Set(definition, "experienceReward", 1);
			Set(definition, "telegraphDuration", 0.05f);
			Set(definition, "attackWindowDuration", 0.18f);
			Set(definition, "recoveryDuration", 0.08f);
			Set(definition, "deathDisableDelay", 10f);
			Set(definition, "navigation", new EnemyNavigationData
			{
				MoveSpeed = 0.01f,
				Acceleration = 14f,
				AngularSpeed = 360f,
				AttackRange = 1.7f,
				DetectionRange = 2f,
				LeashRange = 30f,
				LeashFollowDistance = 5f,
				PatrolRadius = 0f,
				PatrolWait = 5f,
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
	}
}
