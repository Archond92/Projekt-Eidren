using Eidren.Combat;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using Eidren.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatHudStatusPresenterTests
	{
		private const string TerrockAssetPath = "Assets/_Game/Data/Eidren/Terrock.asset";

		[Test]
		public void Resonanzbalken_FolgtAktivemEidraOhneNeuesHud()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			GameObject hud = new GameObject("StatusPresenterTest_Hud");
			GameObject player = new GameObject("StatusPresenterTest_Player");
			GameObject cameraObject = new GameObject("StatusPresenterTest_Camera");
			try
			{
				EidraData terrock = AssetDatabase.LoadAssetAtPath<EidraData>(TerrockAssetPath);
				Assert.That(terrock, Is.Not.Null, "Terrock-Daten fehlen: " + TerrockAssetPath);

				CombatHudStatusPresenter presenter = hud.AddComponent<CombatHudStatusPresenter>();
				Image resonance = NewFill(hud, "Resonance");
				presenter.ConfigureReferences(NewPanel(hud, "PlayerPanel"), NewFill(hud, "Health"), NewFill(hud, "Stamina"), resonance, NewText(hud, "HealthValue"), NewPanel(hud, "Objective"), NewText(hud, "ObjectiveText"), NewPanel(hud, "Boss"), NewFill(hud, "BossHealth"), NewFill(hud, "BossStagger"), NewText(hud, "BossState"));

				player.AddComponent<CharacterController>();
				Damageable health = player.AddComponent<Damageable>();
				health.Initialize(100f);
				PlayerInputReader input = player.AddComponent<PlayerInputReader>();
				PlayerMotor motor = player.AddComponent<PlayerMotor>();
				motor.Initialize(input, cameraObject.AddComponent<Camera>(), health, Vector3.zero, 3f);
				EidraTeamController eidra = player.AddComponent<EidraTeamController>();

				presenter.Bind(health, motor, eidra, null, null, null);
				Assert.That(resonance.fillAmount, Is.EqualTo(0f), "Ohne aktives Eidra muss der Resonanzbalken leer sein");

				eidra.SetTeam(terrock, null);
				Assert.That(resonance.fillAmount, Is.EqualTo(1f), "Aktivieren eines Eidra muss den Balken ohne Neuaufbau des HUDs füllen");

				eidra.SetTeam(null, null);
				Assert.That(resonance.fillAmount, Is.EqualTo(0f), "Entfernen des aktiven Eidra muss den Balken sofort leeren");
			}
			finally
			{
				foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
				{
					if (root != null && root.name.StartsWith("Active_Eidra"))
					{
						Object.DestroyImmediate(root);
					}
				}
				Object.DestroyImmediate(cameraObject);
				Object.DestroyImmediate(player);
				Object.DestroyImmediate(hud);
			}
		}

		private static GameObject NewPanel(GameObject parent, string childName)
		{
			GameObject panel = new GameObject(childName);
			panel.transform.SetParent(parent.transform);
			return panel;
		}

		private static Image NewFill(GameObject parent, string childName)
		{
			return NewPanel(parent, childName).AddComponent<Image>();
		}

		private static Text NewText(GameObject parent, string childName)
		{
			return NewPanel(parent, childName).AddComponent<Text>();
		}
	}
}
