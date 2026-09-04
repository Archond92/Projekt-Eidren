using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Die ausgelieferte Schmiede-Szene
	/// stammte von einer Builder-Fassung OHNE Wand-Kollider — man konnte
	/// durch die Waende laufen, und die Sichtlinien-Ausblendung konnte sie
	/// nie treffen (der Strahl trifft nur Kollider). Diese Wache haelt die
	/// Kollider in der Szene fest.
	/// </summary>
	public sealed class ForgeWallColliderTests
	{
		/// <summary>
		/// Tester-Runde 19.08.2026: Das Verlies braucht einen Ausgang am
		/// Windfang — vorher war die Schmiede eine Einbahnstraße. Die Wache
		/// hält das Builder-Objekt in der Szene fest (Rebuild-Falle).
		/// </summary>
		[Test]
		public void Verlies_TraegtDenAusgangAmWindfang()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
			Eidren.Composition.EidraForgeExit exit = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				exit = root.GetComponentInChildren<Eidren.Composition.EidraForgeExit>(true);
				if (exit != null)
				{
					break;
				}
			}
			Assert.That(exit, Is.Not.Null,
				"EidraForge hat keinen EidraForgeExit — das Verlies ist eine Einbahnstraße. " +
				"Eidren/Scenes/Build Eidra Forge laufen lassen.");
			Assert.That(exit.gameObject.activeSelf, Is.True);
			Assert.That(exit.transform.position.z, Is.LessThan(-30f), "Der Ausgang gehört an den Windfang (Süden).");
			Assert.That(exit.GetComponentInChildren<Collider>(true), Is.Not.Null,
				"Der Ausgang braucht einen Trigger-Collider für die Interaktion.");
		}

		/// <summary>
		/// F32-008 (19.08.2026, mit Bild gemeldet): Die Figur stand IM
		/// achteckigen Körper der Esse. Der Prop-Builder setzte grundsätzlich
		/// keine Kollider — anders als der Geometrie-Builder, der die Wände
		/// baut. Deshalb waren die Wandprüfungen oben grün, während man durch
		/// die Requisiten lief.
		///
		/// Der Wächter hält beide Seiten fest: Aufragende Requisiten sind
		/// fest, flache Bodenzeichnungen bleiben begehbar.
		/// </summary>
		[Test]
		public void Schmiederequisiten_SindFestOderBewusstBegehbar()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
			string[] festePraefixe = new string[4] { "Esse_Koerper", "Esse_Deckplatte", "Windrohr_", "Gelaender" };
			string[] begehbarePraefixe = new string[3] { "Glutriss_", "Glutader_", "Esse_OeffnungGlut" };
			System.Collections.Generic.List<string> ohneKollider = new System.Collections.Generic.List<string>();
			System.Collections.Generic.List<string> faelschlichFest = new System.Collections.Generic.List<string>();
			int gefunden = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (Transform kind in root.GetComponentsInChildren<Transform>(true))
				{
					if (Passt(kind.name, festePraefixe))
					{
						gefunden++;
						if (kind.GetComponent<Collider>() == null)
						{
							ohneKollider.Add(kind.name);
						}
					}
					else if (Passt(kind.name, begehbarePraefixe) && kind.GetComponent<Collider>() != null)
					{
						faelschlichFest.Add(kind.name);
					}
				}
			}
			Assert.That(gefunden, Is.GreaterThan(0), "Die Schmiede hat ihre Requisiten verloren.");
			Assert.That(ohneKollider, Is.Empty,
				"Diese aufragenden Requisiten haben KEINEN Kollider — man laeuft hindurch:\n" + string.Join("\n", ohneKollider));
			Assert.That(faelschlichFest, Is.Empty,
				"Diese Bodenzeichnungen haben einen Kollider und werden zu Stolperkanten:\n" + string.Join("\n", faelschlichFest));
		}

		private static bool Passt(string name, string[] praefixe)
		{
			foreach (string praefix in praefixe)
			{
				if (name.StartsWith(praefix, System.StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		[Test]
		public void Verlieswaende_TragenKollider()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
			int waende = 0;
			int mitKollider = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (Transform kind in root.GetComponentsInChildren<Transform>(true))
				{
					if (kind.name.StartsWith("Wand_", System.StringComparison.Ordinal))
					{
						waende++;
						if (kind.GetComponent<BoxCollider>() != null)
						{
							mitKollider++;
						}
					}
				}
			}
			Assert.That(waende, Is.GreaterThan(50), "Die Schmiede hat ihre Wandstuecke verloren.");
			Assert.That(mitKollider, Is.EqualTo(waende),
				$"{waende - mitKollider} von {waende} Wandstuecken haben KEINEN Kollider — man laeuft hindurch und sie koennen nie faden.");
		}

		[Test]
		public void Wandkollider_DeckenDieSichtbareWandAb()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
			int geprueft = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (Transform kind in root.GetComponentsInChildren<Transform>(true))
				{
					if (!kind.name.StartsWith("Wand_", System.StringComparison.Ordinal))
					{
						continue;
					}
					Renderer renderer = kind.GetComponent<Renderer>();
					BoxCollider collider = kind.GetComponent<BoxCollider>();
					if (renderer == null || collider == null)
					{
						continue;
					}
					Bounds sichtbar = renderer.bounds;
					Bounds fest = collider.bounds;
					// Der Kollider muss die sichtbare Wand tragen — ein leerer
					// oder versetzter Kasten laesst die Figur hindurch.
					Assert.That(fest.size.x * fest.size.y * fest.size.z, Is.GreaterThan(0.01f),
						kind.name + ": Kollider ist praktisch leer.");
					Assert.That(Vector3.Distance(fest.center, sichtbar.center), Is.LessThan(1.5f),
						kind.name + $": Kollider sitzt bei {fest.center}, die sichtbare Wand bei {sichtbar.center}.");
					Assert.That(fest.size.y, Is.GreaterThanOrEqualTo(sichtbar.size.y * 0.6f),
						kind.name + ": Kollider ist deutlich niedriger als die sichtbare Wand.");
					geprueft++;
				}
			}
			Assert.That(geprueft, Is.GreaterThan(50));
		}
	}
}
