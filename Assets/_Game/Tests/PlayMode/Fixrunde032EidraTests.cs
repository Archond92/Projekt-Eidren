using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F32-003/F32-004: Keine Fähigkeit ist an einen Gegnertyp gebunden.
	/// Noctarions Rückenmal verlangte einen Boss, Ignivars Schmelzbrand ein
	/// Ziel mit Schutzwert — beide gegen einen gewöhnlichen Wildling geprüft.
	/// </summary>
	public sealed class Fixrunde032EidraTests
	{
		private GameObject _ground;

		private GameObject _navigationRoot;

		private NavMeshSurface _surface;

		private GameObject _playerObject;

		private GameObject _cameraObject;

		private GameObject _wildlingObject;

		private WildlingController _wildling;

		private EidraTeamController _team;

		private PlayerInputReader _input;

		private string _failure;

		private int _beobachteterSkill = 1;

		/// <summary>
		/// Der Aufbau steht bewusst weit weg vom Welt-Ursprung: Dort liegen
		/// im Voll-Lauf Reste anderer Testklassen, und der Schattenschritt
		/// braucht seinen Zielpunkt frei von fremden Kollidern.
		/// </summary>
		private static readonly Vector3 Mitte = new Vector3(400f, 0f, 400f);

		/// <summary>
		/// F32-004, oberste Prämisse: JEDE Fähigkeit muss gegen JEDEN Gegner
		/// im Kampf nutzbar sein. Der Wächter geht alle sechs Fähigkeiten der
		/// drei Eidra gegen einen gewöhnlichen Wildling durch — genau die
		/// Gegnerart, an der Rückenmal und Schmelzbrand gescheitert sind.
		/// Schlägt eine davon fehl, nennt die Meldung Fähigkeit und Grund.
		/// </summary>
		[UnityTest]
		public IEnumerator KeineFaehigkeit_ScheitertAmGegnertyp()
		{
			(string eidra, int skill)[] faehigkeiten = new (string, int)[6]
			{
				("terrock", 0), ("terrock", 1),
				("noctarion", 0), ("noctarion", 1),
				("ignivar", 0), ("ignivar", 1)
			};
			List<string> fehlschlaege = new List<string>();
			foreach ((string eidra, int skill) in faehigkeiten)
			{
				_beobachteterSkill = skill;
				yield return Aufbau(eidra);
				_input.PressSkill(skill);
				// Ansagezeit der längsten Fähigkeit (0,42 s) in Spielzeit abwarten.
				float zeit = 0f;
				while (zeit < 1.2f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				if (_failure != null)
				{
					fehlschlaege.Add($"{eidra} Fähigkeit {skill + 1}: {_failure}");
				}
				Abbau();
				yield return null;
			}
			_beobachteterSkill = 1;
			Assert.That(fehlschlaege, Is.Empty,
				"Diese Fähigkeiten sind gegen einen gewöhnlichen Wildling nicht nutzbar:\n"
					+ string.Join("\n", fehlschlaege));
		}

		/// <summary>
		/// F32-004, oberste Prämisse: Läuft ein Bosskampf, nahm der Dienst
		/// den Boss ohne jede Reichweitenprüfung als Ziel. Steht der Boss
		/// außer Reichweite und ein gewöhnlicher Gegner direkt daneben, war
		/// die Fähigkeit damit unbenutzbar („AUSSER REICHWEITE") — obwohl ein
		/// gültiges Ziel in Schlagweite stand.
		/// </summary>
		[UnityTest]
		public IEnumerator BossAusserReichweite_SperrtDieFaehigkeitNicht()
		{
			_beobachteterSkill = 0;
			yield return Aufbau("terrock");
			GameObject bossObject = new GameObject("F32_Boss");
			BossData bossData = ScriptableObject.CreateInstance<BossData>();
			try
			{
				// Boss weit außerhalb der Felsbrecher-Reichweite (8), der
				// Wildling bei 5 gut darin.
				bossObject.transform.position = Mitte + new Vector3(0f, 0f, 30f);
				bossData.MaxHealth = 100f;
				bossData.MaxStagger = 100f;
				bossData.Navigation = new EnemyNavigationData
				{
					MoveSpeed = 0.01f,
					Acceleration = 14f,
					AngularSpeed = 360f,
					AttackRange = 1.7f,
					DetectionRange = 2f,
					LeashRange = 60f,
					LeashFollowDistance = 5f,
					PatrolRadius = 0f,
					PatrolWait = 5f,
					AlertDuration = 0.02f,
					ReturnTolerance = 0.55f,
					NavMeshSampleDistance = 3f,
					AgentRadius = 0.52f,
					AgentHeight = 2.1f
				};
				BossController boss = bossObject.AddComponent<BossController>();
				boss.Initialize(bossData, _playerObject.transform, _playerObject.GetComponent<Damageable>(), null);
				boss.BeginBattle();
				_team.AttachBoss(boss);
				yield return null;

				_input.PressSkill(0);
				yield return null;

				Assert.That(_failure, Is.Null,
					"Ein Boss außer Reichweite darf die Fähigkeit nicht sperren, meldete aber: " + _failure);
				PruefeZielIstUnserWildling();
			}
			finally
			{
				Object.Destroy(bossObject);
				Object.DestroyImmediate(bossData);
				Abbau();
			}
			yield return null;
		}

		[UnityTest]
		public IEnumerator Rueckenmal_MarkiertEinenNormalenGegner()
		{
			_beobachteterSkill = 1;
			yield return Aufbau("noctarion");
			Assert.That(_wildling.BackMarkRemaining, Is.EqualTo(0f), "Vorbedingung: noch nicht markiert.");

			_input.PressSkill(1);
			yield return null;

			Assert.That(_failure, Is.Null,
				"Das Rückenmal darf gegen einen normalen Gegner nicht scheitern, meldete aber: " + _failure);
			PruefeZielIstUnserWildling();

			float zeit = 0f;
			while (_wildling.BackMarkRemaining <= 0f && zeit < 4f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(_wildling.BackMarkRemaining, Is.GreaterThan(0f),
				"Der Wildling muss die Rückenmarkierung tragen.");
			Abbau();
		}

		[UnityTest]
		public IEnumerator Schmelzbrand_WirktGegenEinZielOhneSchutz()
		{
			_beobachteterSkill = 1;
			yield return Aufbau("ignivar");
			Assert.That(_wildling.Protection, Is.EqualTo(0f), "Vorbedingung: der Wildling trägt keinen Schutz.");
			float vorher = Schadensprobe();

			_input.PressSkill(1);
			yield return null;

			Assert.That(_failure, Is.Null,
				"Der Schmelzbrand darf gegen ein ungeschütztes Ziel nicht scheitern, meldete aber: " + _failure);
			PruefeZielIstUnserWildling();

			// Ansagezeit (0,35 s) in Spielzeit abwarten.
			float zeit = 0f;
			while (zeit < 1.2f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			float nachher = Schadensprobe();
			Assert.That(nachher, Is.GreaterThan(vorher * 1.05f),
				$"Der Schmelzbrand muss ein ungeschütztes Ziel weicher machen (vorher {vorher}, nachher {nachher}).");
			Abbau();
		}

		/// <summary>
		/// F32-005: Ignivars Passiv entzündet das Ziel. Hier zählt der
		/// laufende Betrieb — die reine Rechnung steht in den EditMode-Tests,
		/// hier muss der Takt im echten Update ankommen und der Schaden am
		/// Gegner landen.
		/// </summary>
		[UnityTest]
		public IEnumerator Brand_ZehrtNachDemTrefferWeiter()
		{
			_beobachteterSkill = 1;
			yield return Aufbau("ignivar");
			float start = _wildling.CurrentHealth;

			_wildling.ApplyBurn(8f, 4, _playerObject);
			Assert.That(_wildling.IsBurning, Is.True, "Nach dem Treffer muss der Gegner brennen.");

			// Vier Sekundentakte in SPIELZEIT abwarten (Batch-deltaTime ist winzig).
			float zeit = 0f;
			while (_wildling.IsBurning && zeit < 10f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(_wildling.IsBurning, Is.False, "Nach vier Takten ist der Brand aus.");
			Assert.That(_wildling.CurrentHealth, Is.EqualTo(start - 8f).Within(0.01f),
				"Der Brand muss in Summe genau den mitgegebenen Schaden abziehen.");
			Abbau();
		}

		/// <summary>
		/// Der Fähigkeitsdienst sucht sich sein Ziel selbst aus der
		/// Kampfziel-Liste. Im Voll-Lauf liegen dort noch Gegner aus früheren
		/// Tests — trifft die Fähigkeit einen davon, bleibt unser Wildling
		/// unberührt, ohne dass eine Fehlermeldung fällt. Diese Prüfung macht
		/// genau das sichtbar, statt es im Ergebnis zu verstecken.
		/// </summary>
		private void PruefeZielIstUnserWildling()
		{
			object ziel = typeof(EidraTeamController)
				.GetField("_combatTarget", BindingFlags.Instance | BindingFlags.NonPublic)
				?.GetValue(_team);
			Assert.That(ziel, Is.SameAs(_wildling),
				"Die Fähigkeit hat ein fremdes Ziel gewählt: " + ((ziel as Component)?.name ?? "null"));
		}

		/// <summary>Ein fester Treffer; zurück kommt der tatsächlich abgezogene Schaden.</summary>
		private float Schadensprobe()
		{
			float vor = _wildling.CurrentHealth;
			_wildling.ApplyDamage(new DamageInfo(10f, 0f, _wildlingObject.transform.position, _playerObject, isBackAttack: false, "test.probe", "test"));
			return vor - _wildling.CurrentHealth;
		}

		[UnityTest]
		public IEnumerator HUD100_AlleSechsFaehigkeitenNutzenGemeinsamesTargeting()
		{
			try
			{
			foreach (string eidra in new[] { "terrock", "noctarion", "ignivar" })
			{
				for (int skill = 0; skill < 2; skill++)
				{
					_beobachteterSkill = skill;
					yield return Aufbau(eidra, true);
					Assert.That(_team.Targeting.TryFindEnemy(12f, out EnemyControllerBase selected), Is.True);
					Assert.That(selected, Is.SameAs(_wildling));
					int cooldowns = 0;
					_team.AbilityCooldownStarted += (index, duration) => { if (index == skill) cooldowns++; };
					_input.PressSkill(skill);
					// Nur waehrend der Aktion ist dies deren Ziel; spaetere Begleitschlaege
					// duerfen einen neuen Aktionskontext haben, keinen zweiten Lock-on.
					if (!(eidra == "terrock" && skill == 1)) PruefeZielIstUnserWildling();
					float elapsed = 0;
					while (elapsed < 1.2f) { elapsed += Time.deltaTime; yield return null; }
					Assert.That(_failure, Is.Null, eidra + " Skill " + skill);
					Assert.That(cooldowns, Is.EqualTo(1), eidra + " Skill " + skill + " muss genau einmal ausgeloest werden.");
					Abbau(); yield return null;
				}
			}
			}
			finally { Abbau(); }
		}

		private IEnumerator Aufbau(string eidraId, bool sharedTargeting = false)
		{
			_failure = null;
			// Fremde Gegner aus früheren Tests abschalten — beim Abschalten
			// tragen sie sich aus der Kampfziel-Liste aus. Sonst sucht sich
			// die Fähigkeit im Voll-Lauf eines von ihnen als Ziel.
			Testumgebung.Bereitstellen(fremdeGegnerAbschalten: true);
			_ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
			_ground.name = "F32_Ground";
			_ground.transform.position = Mitte + new Vector3(0f, -0.5f, 0f);
			_ground.transform.localScale = new Vector3(40f, 1f, 40f);
			Physics.SyncTransforms();
			_navigationRoot = new GameObject("F32_Navigation");
			_surface = _navigationRoot.AddComponent<NavMeshSurface>();
			_surface.collectObjects = CollectObjects.All;
			_surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
			_surface.BuildNavMesh();

			_playerObject = new GameObject("F32_Player");
			_cameraObject = new GameObject("F32_Camera");
			_wildlingObject = new GameObject("F32_Wildling");
			_playerObject.transform.position = Mitte;
			_playerObject.AddComponent<CharacterController>();
			Damageable health = _playerObject.AddComponent<Damageable>();
			health.Initialize(100f);
			_input = _playerObject.AddComponent<PlayerInputReader>();
			PlayerMotor motor = _playerObject.AddComponent<PlayerMotor>();
			Camera camera = _cameraObject.AddComponent<Camera>();
			_cameraObject.tag = "MainCamera";
			// Grosszuegige Bewegungsgrenze — sie ist Testgeruest, keine
			// Spielregel, und darf den Schattenschritt nicht kuenstlich sperren.
			motor.Initialize(_input, camera, health, Mitte, 50f);

			// Ziel in Reichweite beider Fähigkeiten (Rückenmal 12, Schmelzbrand 10).
			_wildlingObject.transform.position = Mitte + new Vector3(0f, 0f, 5f);
			_wildlingObject.AddComponent<CapsuleCollider>();
			GameObject visual = new GameObject("Visual");
			visual.transform.SetParent(_wildlingObject.transform, worldPositionStays: false);
			GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
			telegraph.transform.SetParent(_wildlingObject.transform, worldPositionStays: false);
			Object.Destroy(telegraph.GetComponent<Collider>());
			telegraph.SetActive(value: false);
			GameObject hitboxObject = new GameObject("AttackHitbox");
			hitboxObject.transform.SetParent(_wildlingObject.transform, worldPositionStays: false);
			BoxCollider attackCollider = hitboxObject.AddComponent<BoxCollider>();
			EnemyMeleeHitbox enemyHitbox = _wildlingObject.AddComponent<EnemyMeleeHitbox>();
			enemyHitbox.Configure(attackCollider);
			_wildling = _wildlingObject.AddComponent<WildlingController>();
			_wildling.ConfigurePrefab(Definition(), enemyHitbox, telegraph, visual.transform);
			_wildling.Initialize(_playerObject.transform, health);
			yield return null;

			EidraData eidra = LadeEidra(eidraId);
			Assert.That(eidra, Is.Not.Null, "Eidra-Daten nicht gefunden: " + eidraId);
			Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			_team = _playerObject.AddComponent<EidraTeamController>();
			_team.Initialize(_input, motor, health, null, eidra, null, material, material,
				sharedTargeting ? new PersistentCombatTargetQuery(_playerObject.transform, motor, health: health) : null);
			_team.AbilityFailed += (index, reason) =>
			{
				if (index == _beobachteterSkill)
				{
					_failure = reason;
				}
			};
			yield return null;
		}

		private void Abbau()
		{
			if (_surface != null)
			{
				_surface.RemoveData();
			}
			Object.Destroy(_navigationRoot);
			Object.Destroy(_ground);
			Object.Destroy(_wildlingObject);
			Object.Destroy(_playerObject);
			Object.Destroy(_cameraObject);
		}

		private static EidraData LadeEidra(string id)
		{
			EidraData geladen = Resources.Load<EidraData>("Data/Eidren/" + Grossbuchstabe(id));
			if (geladen != null)
			{
				return geladen;
			}
#if UNITY_EDITOR
			return UnityEditor.AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/" + Grossbuchstabe(id) + ".asset");
#else
			return null;
#endif
		}

		private static string Grossbuchstabe(string id)
		{
			return char.ToUpperInvariant(id[0]) + id.Substring(1);
		}

		private static EnemyDefinition Definition()
		{
			EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
			Set(definition, "id", "enemy.wildling.f32");
			Set(definition, "displayName", "Wildling F32");
			Set(definition, "maximumHealth", 400f);
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
