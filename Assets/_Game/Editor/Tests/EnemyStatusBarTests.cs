using Eidren.AI;
using Eidren.UI;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Jeder Feldgegner trägt über seiner
	/// Figur Name, Lebens- und Staggerbalken. Die Leiste (StatusBars_A20)
	/// stammt aus einem Auftrag, dessen Builder nicht mehr existiert — der
	/// EnemyStatusBarsRebuilder ist ihr neuer Autor.
	/// </summary>
	public sealed class EnemyStatusBarTests
	{
		private static readonly string[] PrefabPaths = new string[]
		{
			"Assets/_Game/Prefabs/Enemies/Wildling.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RootCharger.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/GraniteShell.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RiftGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/EmberEater.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/AshRunner.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/SealGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/ForgeGuardian.prefab",
			// #34: Auch der Kernwaechter traegt die Leiste — ohne sie wirkten
			// Treffer gegen 3000 HP folgenlos ("kann ihn nicht angreifen").
			"Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab"
		};

		private static GameObject Prefab(string path)
		{
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(gameObject, Is.Not.Null, path);
			return gameObject;
		}

		[Test]
		public void JederFeldgegner_TraegtEinNamensLabel()
		{
			foreach (string path in PrefabPaths)
			{
				WildlingStatusBars bars = Prefab(path).GetComponentInChildren<WildlingStatusBars>(true);
				Assert.That(bars, Is.Not.Null, path);
				Assert.That(bars.NameLabel, Is.Not.Null,
					path + ": Namenslabel fehlt oder ist nicht verdrahtet.");
			}
		}

		[Test]
		public void BeideBalkenfuellungen_TragenEinSprite()
		{
			foreach (string path in PrefabPaths)
			{
				WildlingStatusBars bars = Prefab(path).GetComponentInChildren<WildlingStatusBars>(true);
				Assert.That(bars, Is.Not.Null, path);
				// Ein Image mit Type.Filled ohne Sprite zeichnet IMMER voll —
				// der Staggerbalken wirkte dadurch dauerhaft gefuellt.
				Assert.That(bars.HealthFill.sprite, Is.Not.Null, path + ": HealthFill ohne Sprite.");
				Assert.That(bars.StaggerFill.sprite, Is.Not.Null, path + ": StaggerFill ohne Sprite.");
			}
		}

		/// <summary>
		/// #34: Der Kernwaechter steht 65 % der Zeit unter Schutz 0,35 — ohne
		/// sichtbares "SCHUTZ"-Label liest sich das als Unverwundbarkeit.
		/// </summary>
		[Test]
		public void Kernwaechter_TraegtEinSchutzlabel()
		{
			WildlingStatusBars bars = Prefab("Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab")
				.GetComponentInChildren<WildlingStatusBars>(true);
			Assert.That(bars, Is.Not.Null, "Kernwaechter ohne Statusleiste.");
			Transform label = bars.WorldCanvas.transform.Find("ProtectionLabel");
			Assert.That(label, Is.Not.Null, "Kernwaechter ohne Schutzlabel.");
			Assert.That(label.GetComponent<Text>(), Is.Not.Null, "Schutzlabel ohne Text-Komponente.");
		}

		[Test]
		public void Statusleiste_SitztUeberDerFigur()
		{
			foreach (string path in PrefabPaths)
			{
				GameObject prefab = Prefab(path);
				WildlingStatusBars bars = prefab.GetComponentInChildren<WildlingStatusBars>(true);
				Assert.That(bars, Is.Not.Null, path);
				CapsuleCollider body = prefab.GetComponent<CapsuleCollider>();
				Assert.That(body, Is.Not.Null, path);
				float kopfhoehe = body.center.y + body.height * 0.5f;
				float canvasHoehe = bars.WorldCanvas.transform.localPosition.y;
				Assert.That(canvasHoehe, Is.GreaterThanOrEqualTo((object)(kopfhoehe + 0.2f)),
					path + $": Leiste sitzt auf {canvasHoehe:0.00}, Kopf endet bei {kopfhoehe:0.00} — sie steckt in der Figur.");
			}
		}

	}
}
